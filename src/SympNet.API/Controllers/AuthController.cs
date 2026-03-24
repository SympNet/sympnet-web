using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SympNet.Application.DTOs.Auth;
using SympNet.Application.DTOs.Doctor;
using SympNet.Application.DTOs.Patient;
using SympNet.Infrastructure.Services;

namespace SympNet.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    // FIX: removed AppDbContext — controllers must NOT talk to DB directly
    public AuthController(AuthService authService)
    {
        _authService = authService;
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
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin creates a doctor account (Admin role required)
    /// </summary>
    [HttpPost("register/doctor")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorDto dto)
    {
        try
        {
            var result = await _authService.CreateDoctorByAdminAsync(dto);
            return CreatedAtAction(nameof(CreateDoctor), result);
        }
        catch (AppException ex)
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
        catch (AppException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Request a password reset token (sent via email)
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        // Always return the same message regardless of whether email exists
        await _authService.ForgotPasswordAsync(dto);
        return Ok(new { message = "If this email exists, a reset link has been sent." });
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
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}