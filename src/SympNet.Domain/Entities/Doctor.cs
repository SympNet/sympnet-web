namespace SympNet.Domain.Entities;

public class Doctor
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Speciality { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public bool IsValidated { get; set; } = false;
    public double AverageRating { get; set; } = 0;
    public string? ProfilePhotoUrl { get; set; }

    public User User { get; set; } = null!;
}