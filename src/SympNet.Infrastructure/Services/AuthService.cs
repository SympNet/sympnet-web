using Microsoft.EntityFrameworkCore;
using SympNet.Application.DTOs.Auth;
using SympNet.Application.DTOs.Doctor;
using SympNet.Application.DTOs.Patient;
using SympNet.Domain.Entities;
using SympNet.Infrastructure.Data;

namespace SympNet.Infrastructure.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    private readonly EmailService _email;

    public AuthService(AppDbContext db, JwtService jwt, EmailService email)
    {
        _db = db;
        _jwt = jwt;
        _email = email;
    }

    //  PATIENT registers himself (from mobile)
    public async Task<AuthResponseDto> RegisterPatientAsync(RegisterPatientDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("Email already exists.");

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Patient",
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var patient = new Patient
        {
            UserId = user.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            PhoneNumber = dto.PhoneNumber,
            Gender = dto.Gender
        };
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = _jwt.GenerateToken(user),
            Role = user.Role,
            Email = user.Email,
            UserId = user.Id,
            Message = "Patient registered successfully."
        };
    }

    //  ADMIN creates doctor → sends email with credentials
    public async Task<AuthResponseDto> CreateDoctorByAdminAsync(CreateDoctorDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("Email already exists.");

        // Generate random temporary password
        var tempPassword = GenerateTemporaryPassword();

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            Role = "Doctor",
            IsActive = true // Active immediately since admin created it
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var doctor = new Doctor
        {
            UserId = user.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Speciality = dto.Speciality,
            LicenseNumber = dto.LicenseNumber,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            IsValidated = true // Admin already validated
        };
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();

        // Send email with credentials
        await _email.SendDoctorCredentialsAsync(
            dto.Email,
            dto.FirstName,
            tempPassword);

        return new AuthResponseDto
        {
            Role = user.Role,
            Email = user.Email,
            UserId = user.Id,
            Message = $"Doctor created successfully. Credentials sent to {dto.Email}."
        };
    }

    //  LOGIN for all roles (Admin, Doctor, Patient)
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Invalid email or password.");

        if (!user.IsActive)
            throw new Exception("Account is not active.");

        return new AuthResponseDto
        {
            Token = _jwt.GenerateToken(user),
            Role = user.Role,
            Email = user.Email,
            UserId = user.Id,
            Message = "Login successful."
        };
    }

    //  Generate random password for doctor
    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789@#!";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 10)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }

    //  Request password reset → sends token via email
    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return;

        // Generate a URL-safe token
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        user.PasswordResetToken = token; // store plain (not hashed)
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        await _email.SendPasswordResetEmailAsync(user.Email, token);
    }

    //  Verify token and set new password
    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == dto.Token &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
            throw new Exception("Invalid or expired reset token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        await _db.SaveChangesAsync();
    }
}