using Microsoft.EntityFrameworkCore;
using SympNet.Application.DTOs.Auth;
using SympNet.Application.DTOs.Doctor;
using SympNet.Application.DTOs.Patient;
using SympNet.Domain.Entities;
using SympNet.Infrastructure.Data;
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

    // ─────────────────────────────────────────────────────────────
    //  PATIENT registers himself (from mobile)
    // ─────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────
    //  ADMIN creates a doctor → sends email with credentials
    // ─────────────────────────────────────────────────────────────
    public async Task<AuthResponseDto> CreateDoctorByAdminAsync(CreateDoctorDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new AppException("Email already exists.");

        // FIX: use cryptographically secure random password
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

        // Send credentials email
        await _email.SendDoctorCredentialsAsync(dto.Email, dto.FirstName, tempPassword);

        return new AuthResponseDto
        {
            Role = user.Role,
            Email = user.Email,
            UserId = user.Id,
            Message = $"Doctor created successfully. Credentials sent to {dto.Email}."
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  LOGIN for all roles
    // ─────────────────────────────────────────────────────────────
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        // FIX: always run BCrypt even on null user to prevent timing attacks
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

    // ─────────────────────────────────────────────────────────────
    //  FORGOT PASSWORD → sends reset token via email
    // ─────────────────────────────────────────────────────────────
    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        // FIX: always add a delay to prevent user enumeration via timing
        await Task.Delay(Random.Shared.Next(200, 500));

        if (user == null) return; // silent — don't reveal if email exists

        // Generate a URL-safe random token
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        // FIX: store HASHED token in DB (never plain text)
        user.PasswordResetToken = BCrypt.Net.BCrypt.HashPassword(rawToken);
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        // Send the RAW token to user (only they have it)
        await _email.SendPasswordResetEmailAsync(user.Email, rawToken);
    }

    // ─────────────────────────────────────────────────────────────
    //  RESET PASSWORD → verify token and set new password
    // ─────────────────────────────────────────────────────────────
    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        // Load all non-expired candidates, then verify token via BCrypt
        var candidates = await _db.Users
            .Where(u => u.PasswordResetTokenExpiry > DateTime.UtcNow
                     && u.PasswordResetToken != null)
            .ToListAsync();

        // FIX: BCrypt.Verify handles the hash comparison securely
        var user = candidates.FirstOrDefault(u =>
            BCrypt.Net.BCrypt.Verify(dto.Token, u.PasswordResetToken!));

        if (user == null)
            throw new AppException("Invalid or expired reset token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        await _db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────

    // FIX: cryptographically secure random (replaces new Random())
    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789@#!";
        var bytes = RandomNumberGenerator.GetBytes(12);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}