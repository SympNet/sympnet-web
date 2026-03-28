namespace SympNet.Application.DTOs.Doctor;

public class UpdateDoctorProfileDto
{
	public string? FirstName { get; set; }
	public string? LastName { get; set; }
	public string? Speciality { get; set; }
	public string? LicenseNumber { get; set; }
	public string? Address { get; set; }
	public string? PhoneNumber { get; set; }
	public double Latitude { get; set; }
	public double Longitude { get; set; }
}