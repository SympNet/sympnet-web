using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.Application.DTOs.Doctor;
using SympNet.Infrastructure.Data;
using SympNet.Infrastructure.Services;

namespace SympNet.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly AppDbContext _db;

    public AdminController(AuthService authService, AppDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    /// <summary>
    /// Admin creates a doctor → doctor receives email with credentials
    /// </summary>
    [HttpPost("doctors")]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorDto dto)
    {
        try
        {
            var result = await _authService.CreateDoctorByAdminAsync(dto);
            return CreatedAtAction(nameof(CreateDoctor), result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all doctors
    /// </summary>
    [HttpGet("doctors")]
    public async Task<IActionResult> GetAllDoctors()
    {
        var doctors = await _db.Doctors
            .Include(d => d.User)
            .Select(d => new DoctorResponseDto
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Speciality = d.Speciality,
                Address = d.Address,
                Latitude = d.Latitude,
                Longitude = d.Longitude,
                AverageRating = d.AverageRating,
                IsValidated = d.IsValidated,
                Email = d.User.Email
            })
            .ToListAsync();

        return Ok(doctors);
    }

    /// <summary>
    /// Get doctor by ID
    /// </summary>
    [HttpGet("doctors/{id}")]
    public async Task<IActionResult> GetDoctorById(int id)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
            return NotFound(new { message = "Doctor not found." });

        return Ok(new DoctorResponseDto
        {
            Id = doctor.Id,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            Speciality = doctor.Speciality,
            Address = doctor.Address,
            Latitude = doctor.Latitude,
            Longitude = doctor.Longitude,
            AverageRating = doctor.AverageRating,
            IsValidated = doctor.IsValidated,
            Email = doctor.User.Email
        });
    }

    /// <summary>
    /// Activate a doctor account
    /// </summary>
    [HttpPut("doctors/{id}/activate")]
    public async Task<IActionResult> ActivateDoctor(int id)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
            return NotFound(new { message = "Doctor not found." });

        doctor.IsValidated = true;
        doctor.User.IsActive = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Doctor {doctor.FirstName} {doctor.LastName} activated." });
    }

    /// <summary>
    /// Deactivate a doctor account
    /// </summary>
    [HttpPut("doctors/{id}/deactivate")]
    public async Task<IActionResult> DeactivateDoctor(int id)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
            return NotFound(new { message = "Doctor not found." });

        doctor.User.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Doctor {doctor.FirstName} {doctor.LastName} deactivated." });
    }

    /// <summary>
    /// Delete a doctor account
    /// </summary>
    [HttpDelete("doctors/{id}")]
    public async Task<IActionResult> DeleteDoctor(int id)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
            return NotFound(new { message = "Doctor not found." });

        _db.Doctors.Remove(doctor);
        _db.Users.Remove(doctor.User);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Doctor {doctor.FirstName} {doctor.LastName} deleted." });
    }

    /// <summary>
    /// Get platform statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = new
        {
            TotalPatients = await _db.Patients.CountAsync(),
            TotalDoctors = await _db.Doctors.CountAsync(),
            ActiveDoctors = await _db.Doctors.CountAsync(d => d.IsValidated),
            PendingDoctors = await _db.Doctors.CountAsync(d => !d.IsValidated)
        };

        return Ok(stats);
    }
}