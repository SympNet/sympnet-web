using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.Application.DTOs.Auth;
using SympNet.Application.DTOs.Patient;
using SympNet.Infrastructure.Data;
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

    /// <summary>
    /// Patient registers himself (mobile only)
    /// </summary>
    [HttpPost("register/patient")]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientDto dto)
    {
        try
        {
            var result = await _authService.RegisterPatientAsync(dto);
            return CreatedAtAction(nameof(RegisterPatient), result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Login for all roles (Admin, Doctor, Patient)
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // TEMPORARY - delete after use!
    [HttpGet("reset-admin")]
    public async Task<IActionResult> ResetAdmin()
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "sirine.rezgui@ensi-uma.tn");
        if (user == null) return NotFound(new { message = "Admin not found" });
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("sirine123.");
        await _db.SaveChangesAsync();
        return Ok(new { message = "Admin password reset successfully!" });
    }

    /// <summary>
    /// Request a password reset token (sent via email)
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto);
        return Ok(new { message = "If this email exists, a reset token has been sent." });
    }

    /// <summary>
    /// Reset password using the token received by email
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { message = "Password reset successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("fix-admin")]
    public async Task<IActionResult> FixAdmin()
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "sirine.rezgui@ensi-uma.tn");
        if (user == null) return NotFound(new { message = "Admin not found" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@2026!");
        await _db.SaveChangesAsync();

        return Ok(new { message = "Admin password fixed!" });
    }
}