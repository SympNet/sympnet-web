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
    private readonly ConsultationService _service;

    public ConsultationController(ConsultationService service)
    {
        _service = service;
    }

    [HttpGet("doctor/{doctorId}")]
    public async Task<IActionResult> GetDoctorConsultations(int doctorId)
    {
        var result = await _service.GetDoctorConsultationsAsync(doctorId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetConsultationByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConsultationDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        var patientId = int.Parse(userIdClaim);

        var result = await _service.CreateConsultationAsync(patientId, dto);
        return Ok(result);
    }

    [HttpPut("{id}/ai-result")]
    public async Task<IActionResult> UpdateAIResult(int id, [FromBody] UpdateAIResultDto dto)
    {
        await _service.UpdateAIResultAsync(id, dto);
        return Ok(new { message = "Résultat IA mis à jour." });
    }
}