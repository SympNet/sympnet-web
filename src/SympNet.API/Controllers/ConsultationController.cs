using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SympNet.Application.DTOs.Consultation;
using SympNet.Infrastructure.Services;
using System.Security.Claims;

namespace SympNet.API.Controllers;

[ApiController]
[Route("api/consultations")]
[Authorize]
public class ConsultationController : ControllerBase
{
    private readonly ConsultationService _consultationService;
    private readonly AIAnalysisService _aiService;
    private readonly ILogger<ConsultationController> _logger;

    public ConsultationController(
        ConsultationService consultationService,
        AIAnalysisService aiService,
        ILogger<ConsultationController> logger)
    {
        _consultationService = consultationService;
        _aiService = aiService;
        _logger = logger;
    }

    [HttpGet("doctor/{doctorId}")]
    public async Task<IActionResult> GetDoctorConsultations(int doctorId)
    {
        var result = await _consultationService.GetDoctorConsultationsAsync(doctorId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _consultationService.GetConsultationByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConsultationDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        var patientId = int.Parse(userIdClaim);

        var result = await _consultationService.CreateConsultationAsync(patientId, dto);
        return Ok(result);
    }

    [HttpPut("{id}/ai-result")]
    public async Task<IActionResult> UpdateAIResult(int id, [FromBody] UpdateAIResultDto dto)
    {
        await _consultationService.UpdateAIResultAsync(id, dto);
        return Ok(new { message = "Résultat IA mis à jour." });
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeWithAI([FromBody] AnalyzeRequest request)
    {
        try
        {
            _logger.LogInformation("Analyzing symptoms for consultation {ConsultationId}", request.ConsultationId);

            var result = await _aiService.AnalyzeSymptomsAsync(request.Symptoms);

            if (result.Success)
            {
                // Sauvegarder le résultat
                await _consultationService.UpdateAIResultAsync(request.ConsultationId, new UpdateAIResultDto
                {
                    AIDiagnosisJson = result.DiagnosisJson ?? "",
                    AIConfidenceScore = result.ConfidenceScore,
                    AIUrgencyLevel = result.UrgencyLevel
                });

                return Ok(result);
            }
            else
            {
                return BadRequest(new { error = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI analysis");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class AnalyzeRequest
{
    public int ConsultationId { get; set; }
    public string Symptoms { get; set; } = "";
}