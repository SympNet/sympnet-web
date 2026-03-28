using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.Application.DTOs.Doctor;
using SympNet.Infrastructure.Data;
using System.Security.Claims;

namespace SympNet.API.Controllers;

[ApiController]
[Route("api/doctor")]
[Authorize(Roles = "Doctor")]
public class DoctorController : ControllerBase
{
    private readonly AppDbContext _db;

    public DoctorController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get current doctor profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == int.Parse(userId));

        if (doctor == null)
            return NotFound(new { message = "Doctor not found." });

        return Ok(new DoctorProfileDto
        {
            Id = doctor.Id,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            Email = doctor.User.Email,
            Speciality = doctor.Speciality,
            LicenseNumber = doctor.LicenseNumber,
            Address = doctor.Address,
            PhoneNumber = "", // Temporaire - à ajouter plus tard
            Latitude = doctor.Latitude,
            Longitude = doctor.Longitude,
            AverageRating = doctor.AverageRating
        });
    }

    /// <summary>
    /// Update doctor profile
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateDoctorProfileDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == int.Parse(userId));

        if (doctor == null)
            return NotFound(new { message = "Doctor not found." });

        // Update fields
        if (!string.IsNullOrEmpty(dto.FirstName))
            doctor.FirstName = dto.FirstName;
        if (!string.IsNullOrEmpty(dto.LastName))
            doctor.LastName = dto.LastName;
        if (!string.IsNullOrEmpty(dto.Speciality))
            doctor.Speciality = dto.Speciality;
        if (!string.IsNullOrEmpty(dto.LicenseNumber))
            doctor.LicenseNumber = dto.LicenseNumber;
        if (!string.IsNullOrEmpty(dto.Address))
            doctor.Address = dto.Address;

        // PhoneNumber - commenté car pas encore dans la base
        // if (!string.IsNullOrEmpty(dto.PhoneNumber))
        //     doctor.User.PhoneNumber = dto.PhoneNumber;

        doctor.Latitude = dto.Latitude;
        doctor.Longitude = dto.Longitude;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Profile updated successfully." });
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _db.Users.FindAsync(int.Parse(userId));
        if (user == null)
            return NotFound(new { message = "User not found." });

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully." });
    }

    /// <summary>
    /// Get doctor stats (patients, consultations, rating)
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var doctor = await _db.Doctors
            .FirstOrDefaultAsync(d => d.UserId == int.Parse(userId));

        if (doctor == null)
            return NotFound(new { message = "Doctor not found." });

        var consultations = await _db.Consultations
            .Where(c => c.DoctorId == doctor.Id)
            .ToListAsync();

        var patients = consultations.Select(c => c.PatientId).Distinct().Count();

        var stats = new
        {
            TotalPatients = patients,
            TotalConsultations = consultations.Count,
            AverageRating = doctor.AverageRating
        };

        return Ok(stats);
    }
}