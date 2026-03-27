namespace SympNet.Application.DTOs.Auth;

public class ValidateTokenDto
{
    public string Token { get; set; } = string.Empty;
    public string? Email { get; set; }
}