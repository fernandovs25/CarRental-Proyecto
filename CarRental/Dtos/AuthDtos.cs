using System.ComponentModel.DataAnnotations;

namespace CarRental.Dtos;

public sealed record RegisterDto
{
    [Required, MinLength(3), MaxLength(100)]
    public string Username { get; init; } = string.Empty;

    [Required, MinLength(12), MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; init; } = string.Empty;
}

public sealed record LoginDto
{
    [Required, MaxLength(100)]
    public string Username { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}

public sealed record TokenResponse(string Token, DateTimeOffset ExpiresAt);

public sealed record RegisterResponse(int Id, string Username, string Email, string Role);