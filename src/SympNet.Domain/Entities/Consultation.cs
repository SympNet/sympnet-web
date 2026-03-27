namespace SympNet.Domain.Entities;

public class Consultation
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string SymptomDescription { get; set; } = "";
    public string BodyPart { get; set; } = "";
    public string Status { get; set; } = "Pending"; // Pending / InProgress / Done

    // Résultat IA — stocké en JSON
    public string? AIDiagnosisJson { get; set; }
    public double? AIConfidenceScore { get; set; }
    public string? AIUrgencyLevel { get; set; } // Green / Orange / Red

    // Navigation
    public User Patient { get; set; } = null!;
    public User Doctor { get; set; } = null!;
}