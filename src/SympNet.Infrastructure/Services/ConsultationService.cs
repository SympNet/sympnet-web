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
        return await _db.Consultations
            .Where(c => c.DoctorId == doctorId)
            .Include(c => c.Patient)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ConsultationDto
            {
                Id = c.Id,
                PatientId = c.PatientId,
                // Utilisez les propriétés qui existent dans User
                PatientName = c.Patient.Email,  // Temporaire: utilise Email comme nom
                PatientEmail = c.Patient.Email,
                CreatedAt = c.CreatedAt,
                SymptomDescription = c.SymptomDescription,
                BodyPart = c.BodyPart,
                Status = c.Status,
                AIConfidenceScore = c.AIConfidenceScore,
                AIUrgencyLevel = c.AIUrgencyLevel,
                AIDiagnosisJson = c.AIDiagnosisJson
            })
            .ToListAsync();
    }

    public async Task<ConsultationDto?> GetConsultationByIdAsync(int id)
    {
        return await _db.Consultations
            .Include(c => c.Patient)
            .Where(c => c.Id == id)
            .Select(c => new ConsultationDto
            {
                Id = c.Id,
                PatientId = c.PatientId,
                PatientName = c.Patient.Email,  // Temporaire
                PatientEmail = c.Patient.Email,
                CreatedAt = c.CreatedAt,
                SymptomDescription = c.SymptomDescription,
                BodyPart = c.BodyPart,
                Status = c.Status,
                AIConfidenceScore = c.AIConfidenceScore,
                AIUrgencyLevel = c.AIUrgencyLevel,
                AIDiagnosisJson = c.AIDiagnosisJson
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ConsultationDto> CreateConsultationAsync(int patientId, CreateConsultationDto dto)
    {
        // Trouver un médecin (sans filtrer sur IsValidated si ça n'existe pas)
        var doctor = await _db.Users
            .Where(u => u.Role == "Doctor")
            .FirstOrDefaultAsync();

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
        var c = await _db.Consultations.FindAsync(consultationId);
        if (c == null) return;

        c.AIDiagnosisJson = dto.AIDiagnosisJson;
        c.AIConfidenceScore = dto.AIConfidenceScore;
        c.AIUrgencyLevel = dto.AIUrgencyLevel;
        c.Status = "InProgress";
        await _db.SaveChangesAsync();
    }
}