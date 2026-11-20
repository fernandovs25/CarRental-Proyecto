using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CarRental.Controllers;
using CarRental.Data;
using CarRental.Dtos;
using CarRental.Models;
using CarRental.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CarRental.Tests.Security;

public sealed class AuthControllerTests : IDisposable
{
    private const string JwtSigningKey = "test-signing-key-with-at-least-32-characters-long";
    private readonly AppDbContext _context;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();
    private readonly JwtTokenService _tokenService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtSigningKey,
                ["Jwt:Issuer"] = "CarRental.Tests",
                ["Jwt:Audience"] = "CarRental.Tests"
            })
            .Build();
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "CarRental.Tests",
            Audience = "CarRental.Tests",
            ExpireMinutes = 60
        });
        _tokenService = new JwtTokenService(configuration, jwtOptions);
        _controller = new AuthController(_context, _passwordHasher, _tokenService);
    }

    [Fact]
    public async Task Register_StoresPasswordHashAndIgnoresRequestedAdminRole()
    {
        var request = JsonSerializer.Deserialize<RegisterDto>(
            """{"username":"admin_car","password":"Password123!","email":"admin@example.test","role":"Admin"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var result = await _controller.Register(request, TestContext.Current.CancellationToken);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(AuthController.GetUser), created.ActionName);
        var user = await _context.Users.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("User", user.Role);
        Assert.NotEqual("Password123!", user.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, "Password123!"));
    }

    [Fact]
    public async Task Register_RejectsDuplicateUsernameOrEmail()
    {
        await _controller.Register(new RegisterDto
        {
            Username = "admin_car",
            Password = "Password123!",
            Email = "admin@example.test"
        }, TestContext.Current.CancellationToken);

        var result = await _controller.Register(new RegisterDto
        {
            Username = "admin_car",
            Password = "AnotherPass123!",
            Email = "other@example.test"
        }, TestContext.Current.CancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsTokenWithUserRole()
    {
        await _controller.Register(new RegisterDto
        {
            Username = "driver_one",
            Password = "Password123!",
            Email = "driver@example.test"
        }, TestContext.Current.CancellationToken);

        var result = Assert.IsType<OkObjectResult>(await _controller.Login(
            new LoginDto { Username = "driver_one", Password = "Password123!" },
            TestContext.Current.CancellationToken));
        var response = Assert.IsType<TokenResponse>(result.Value);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
        var principal = new JwtSecurityTokenHandler().ValidateToken(response.Token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "CarRental.Tests",
                ValidateAudience = true,
                ValidAudience = "CarRental.Tests",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

        Assert.True(response.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "User");
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Jti);
        Assert.Equal("driver_one", principal.Identity?.Name);
    }

    [Fact]
    public async Task Login_RejectsWrongPassword()
    {
        await _controller.Register(new RegisterDto
        {
            Username = "driver_one",
            Password = "Password123!",
            Email = "driver@example.test"
        }, TestContext.Current.CancellationToken);

        var result = await _controller.Login(
            new LoginDto { Username = "driver_one", Password = "WrongPassword123!" },
            TestContext.Current.CancellationToken);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void VehicleWriteEndpointsRequireAuthenticationAndAdminRole()
    {
        var create = typeof(VehiculosController).GetMethod(nameof(VehiculosController.Create))!;
        var delete = typeof(VehiculosController).GetMethod(nameof(VehiculosController.Delete))!;

        Assert.NotNull(create.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.Equal("Admin", Assert.IsType<AuthorizeAttribute>(
            Assert.Single(delete.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true))).Roles);
    }

    [Fact]
    public async Task GetUser_AllowsOwnerAndHidesPasswordHash()
    {
        var result = Assert.IsType<CreatedAtActionResult>(await _controller.Register(new RegisterDto
        {
            Username = "driver_one",
            Password = "Password123!",
            Email = "driver@example.test"
        }, TestContext.Current.CancellationToken));
        var response = Assert.IsType<RegisterResponse>(result.Value);
        SetAuthenticatedUser(response.Id);

        var profile = Assert.IsType<OkObjectResult>(await _controller.GetUser(response.Id, TestContext.Current.CancellationToken));

        Assert.Equal(response, profile.Value);
        Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(profile.Value));
    }

    [Fact]
    public async Task GetUser_ForbidsAnotherRegularUser()
    {
        await _controller.Register(new RegisterDto
        {
            Username = "driver_one",
            Password = "Password123!",
            Email = "driver@example.test"
        }, TestContext.Current.CancellationToken);
        var other = Assert.IsType<CreatedAtActionResult>(await _controller.Register(new RegisterDto
        {
            Username = "driver_two",
            Password = "Password123!",
            Email = "other@example.test"
        }, TestContext.Current.CancellationToken));
        var otherUser = Assert.IsType<RegisterResponse>(other.Value);
        SetAuthenticatedUser(999);

        var result = await _controller.GetUser(otherUser.Id, TestContext.Current.CancellationToken);

        Assert.IsType<ForbidResult>(result);
    }

    private void SetAuthenticatedUser(int id)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, "test_user"),
            new Claim(ClaimTypes.Role, "User")
        ], "test");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}