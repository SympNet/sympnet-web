namespace SympNet.Application.DTOs.Doctor;

public class DoctorProfileDto
{
	public int Id { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Speciality { get; set; } = string.Empty;
	public string LicenseNumber { get; set; } = string.Empty;
	public string Address { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public double Latitude { get; set; }
	public double Longitude { get; set; }
	public double AverageRating { get; set; }
}