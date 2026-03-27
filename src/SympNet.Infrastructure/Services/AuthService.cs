using Microsoft.EntityFrameworkCore;
using SympNet.Application.DTOs.Auth;
using SympNet.Application.DTOs.Doctor;
using SympNet.Application.DTOs.Patient;
using SympNet.Domain.Entities;
using SympNet.Infrastructure.Data;
using SympNet.Infrastructure.Exceptions;
using System.Security.Cryptography;

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

    public async Task<AuthResponseDto> RegisterPatientAsync(RegisterPatientDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new AppException("Email already exists.");

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

    public async Task<AuthResponseDto> CreateDoctorByAdminAsync(CreateDoctorDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new AppException("Email already exists.");

        var tempPassword = GenerateTemporaryPassword();

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            Role = "Doctor",
            IsActive = true
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
            IsValidated = true
        };
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();

        await _email.SendDoctorCredentialsAsync(dto.Email, dto.FirstName, tempPassword);

        return new AuthResponseDto
        {
            Role = user.Role,
            Email = user.Email,
            UserId = user.Id,
            Message = $"Doctor created successfully. Credentials sent to {dto.Email}."
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        var passwordHash = user?.PasswordHash ?? BCrypt.Net.BCrypt.HashPassword("dummy");
        var valid = BCrypt.Net.BCrypt.Verify(dto.Password, passwordHash);

        if (user == null || !valid)
            throw new AppException("Invalid email or password.");

        if (!user.IsActive)
            throw new AppException("Account is not active.");

        return new AuthResponseDto
        {
            Token = _jwt.GenerateToken(user),
            Role = user.Role,
            Email = user.Email,
            UserId = user.Id,
            Message = "Login successful."
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
        {
            // Timing attack prevention
            await Task.Delay(1000);
            return;
        }

        // Generate a cryptographically secure token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes);

        // Hash the token before storing
        user.PasswordResetToken = BCrypt.Net.BCrypt.HashPassword(token);
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);

        await _db.SaveChangesAsync();

        // Send the email with the raw token (not hashed)
        await _email.SendPasswordResetEmailAsync(user.Email, token);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        // Find all users with unexpired reset tokens
        var candidates = await _db.Users
            .Where(u => u.PasswordResetTokenExpiry > DateTime.UtcNow
                     && u.PasswordResetToken != null)
            .ToListAsync();

        // Verify the token against the stored hash
        var user = candidates.FirstOrDefault(u =>
            BCrypt.Net.BCrypt.Verify(dto.Token, u.PasswordResetToken!));

        if (user == null)
            throw new AppException("Invalid or expired reset token.");

        // Update password and clear reset token
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _db.SaveChangesAsync();
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789@#!";
        var bytes = RandomNumberGenerator.GetBytes(12);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}