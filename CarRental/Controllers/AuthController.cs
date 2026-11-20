using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using CarRental.Data;
using CarRental.Dtos;
using CarRental.Models;
using CarRental.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace CarRental.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext context,
    IPasswordHasher<AppUser> passwordHasher,
    JwtTokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterDto request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();
        if (await context.Users.AnyAsync(user => user.Username == username || user.Email == email, cancellationToken))
        {
            return Conflict(new { message = "El usuario o correo ya está registrado." });
        }

        var user = new AppUser
        {
            Username = username,
            Email = email,
            Role = "User"
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        context.Users.Add(user);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.GetBaseException() is MySqlException { Number: 1062 })
        {
            return Conflict(new { message = "El usuario o correo ya está registrado." });
        }

        var response = new RegisterResponse(user.Id, user.Username, user.Email, user.Role);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
    }

    [Authorize]
    [HttpGet("users/{id:int}")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        if (id != currentUserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return user is null
            ? NotFound()
            : Ok(new RegisterResponse(user.Id, user.Username, user.Email, user.Role));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginDto request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var user = await context.Users.SingleOrDefaultAsync(
            candidate => candidate.Username == username, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "Usuario o contraseña incorrectos." });
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Usuario o contraseña incorrectos." });
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await context.SaveChangesAsync(cancellationToken);
        }

        var (token, expiresAt) = tokenService.GenerateToken(user);
        return Ok(new TokenResponse(token, expiresAt));
    }
}