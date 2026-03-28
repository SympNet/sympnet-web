namespace SympNet.Application.DTOs.Consultation;

public class ConsultationDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = "";
    public string PatientEmail { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string SymptomDescription { get; set; } = "";
    public string BodyPart { get; set; } = "";
    public string Status { get; set; } = "";
    public double? AIConfidenceScore { get; set; }
    public string? AIUrgencyLevel { get; set; }
    public string? AIDiagnosisJson { get; set; }
}

public class CreateConsultationDto
{
    public int DoctorId { get; set; }
    public string SymptomDescription { get; set; } = "";
    public string BodyPart { get; set; } = "";
}

public class UpdateAIResultDto
{
    public string AIDiagnosisJson { get; set; } = "";
    public double AIConfidenceScore { get; set; }
    public string AIUrgencyLevel { get; set; } = "GREEN";
}