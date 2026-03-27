using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.Application.DTOs.Auth;
using SympNet.Application.DTOs.Doctor;
using SympNet.Application.DTOs.Patient;
using SympNet.Infrastructure.Data;
using SympNet.Infrastructure.Exceptions;
using SympNet.Infrastructure.Services;

namespace SympNet.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly AppDbContext _db;

    public AuthController(AuthService authService, AppDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    [HttpPost("register/patient")]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientDto dto)
    {
        try
        {
            var result = await _authService.RegisterPatientAsync(dto);
            return CreatedAtAction(nameof(RegisterPatient), result);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (AppException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("create-doctor")]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorDto dto)
    {
        try
        {
            var result = await _authService.CreateDoctorByAdminAsync(dto);
            return Ok(result);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto);
        return Ok(new { message = "Si l'email existe, un lien de réinitialisation a été envoyé." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { message = "Mot de passe réinitialisé avec succès." });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("validate-reset-token")]
    public async Task<IActionResult> ValidateResetToken([FromBody] ValidateTokenDto dto)
    {
        try
        {
            var candidates = await _db.Users
                .Where(u => u.PasswordResetTokenExpiry > DateTime.UtcNow
                         && u.PasswordResetToken != null)
                .ToListAsync();

            var valid = candidates.Any(u =>
                BCrypt.Net.BCrypt.Verify(dto.Token, u.PasswordResetToken!));

            if (!valid) return BadRequest(new { message = "Token invalide ou expiré." });

            return Ok(new { message = "Token valide." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}