using Microsoft.EntityFrameworkCore;
using SympNet.Application.DTOs.Consultation;
using SympNet.Domain.Entities;
using SympNet.Infrastructure.Data;

namespace SympNet.Infrastructure.Services;

public class ConsultationService
{
    private readonly AppDbContext _db;

    public ConsultationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ConsultationDto>> GetDoctorConsultationsAsync(int doctorId)
    {
        var consultations = await _db.Consultations
            .Where(c => c.DoctorId == doctorId)
            .Include(c => c.Patient)
            .ThenInclude(p => ((Patient)p.Patient))
            .Include(c => c.Doctor)
            .ThenInclude(d => ((Doctor)d.Doctor))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return consultations.Select(c => new ConsultationDto
        {
            Id = c.Id,
            PatientId = c.PatientId,
            PatientName = GetPatientFullName(c.Patient),
            PatientEmail = c.Patient.Email,
            CreatedAt = c.CreatedAt,
            SymptomDescription = c.SymptomDescription,
            BodyPart = c.BodyPart,
            Status = c.Status,
            AIConfidenceScore = c.AIConfidenceScore,
            AIUrgencyLevel = c.AIUrgencyLevel,
            AIDiagnosisJson = c.AIDiagnosisJson
        }).ToList();
    }

    public async Task<ConsultationDto?> GetConsultationByIdAsync(int id)
    {
        var consultation = await _db.Consultations
            .Include(c => c.Patient)
            .ThenInclude(p => ((Patient)p.Patient))
            .Include(c => c.Doctor)
            .ThenInclude(d => ((Doctor)d.Doctor))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (consultation == null) return null;

        return new ConsultationDto
        {
            Id = consultation.Id,
            PatientId = consultation.PatientId,
            PatientName = GetPatientFullName(consultation.Patient),
            PatientEmail = consultation.Patient.Email,
            CreatedAt = consultation.CreatedAt,
            SymptomDescription = consultation.SymptomDescription,
            BodyPart = consultation.BodyPart,
            Status = consultation.Status,
            AIConfidenceScore = consultation.AIConfidenceScore,
            AIUrgencyLevel = consultation.AIUrgencyLevel,
            AIDiagnosisJson = consultation.AIDiagnosisJson
        };
    }

    private string GetPatientFullName(User user)
    {
        if (user.Patient != null)
        {
            return $"{user.Patient.FirstName} {user.Patient.LastName}";
        }
        return user.Email;
    }

    public async Task<ConsultationDto> CreateConsultationAsync(int patientId, CreateConsultationDto dto)
    {
        // Trouver un médecin disponible
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.User.IsActive && d.IsValidated);

        if (doctor == null) throw new Exception("Aucun médecin disponible.");

        var consultation = new Consultation
        {
            PatientId = patientId,
            DoctorId = dto.DoctorId > 0 ? dto.DoctorId : doctor.Id,
            SymptomDescription = dto.SymptomDescription,
            BodyPart = dto.BodyPart,
            Status = "Pending"
        };

        _db.Consultations.Add(consultation);
        await _db.SaveChangesAsync();

        return await GetConsultationByIdAsync(consultation.Id) ?? throw new Exception("Erreur création.");
    }

    public async Task UpdateAIResultAsync(int consultationId, UpdateAIResultDto dto)
    {
        var consultation = await _db.Consultations.FindAsync(consultationId);
        if (consultation == null) return;

        consultation.AIDiagnosisJson = dto.AIDiagnosisJson;
        consultation.AIConfidenceScore = dto.AIConfidenceScore;
        consultation.AIUrgencyLevel = dto.AIUrgencyLevel;
        consultation.Status = "Analyzed";
        await _db.SaveChangesAsync();
    }
}