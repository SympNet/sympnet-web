using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SympNet.Infrastructure.Services;

public class AIAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIAnalysisService> _logger;
    private readonly string _aiServiceUrl;

    public AIAnalysisService(HttpClient httpClient, IConfiguration config, ILogger<AIAnalysisService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _aiServiceUrl = config["AIService:Url"] ?? "http://localhost:8000";
    }

    public async Task<AIAnalysisResult> AnalyzeSymptomsAsync(string symptomDescription)
    {
        try
        {
            _logger.LogInformation("Calling AI service: {Symptom}", symptomDescription);

            var request = new { text = symptomDescription, language = "fr" };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_aiServiceUrl}/analyze", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AIAnalysisResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new AIAnalysisResult
                {
                    Success = true,
                    DiagnosisJson = JsonSerializer.Serialize(result?.Hypotheses),
                    ConfidenceScore = result?.OverallConfidence ?? 0,
                    UrgencyLevel = result?.UrgencyLevel ?? "GREEN",
                    Hypotheses = result?.Hypotheses ?? new List<DiagnosisHypothesis>(),
                    Recommendations = result?.Recommendations ?? new List<string>()
                };
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("AI service error: {Error}", error);
                return new AIAnalysisResult { Success = false, ErrorMessage = error };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI service");
            return new AIAnalysisResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<bool> IsServiceHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_aiServiceUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public class AIAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DiagnosisJson { get; set; }
    public double ConfidenceScore { get; set; }
    public string UrgencyLevel { get; set; } = "GREEN";
    public List<DiagnosisHypothesis> Hypotheses { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class DiagnosisHypothesis
{
    public string Diagnosis { get; set; } = "";
    public double Confidence { get; set; }
    public double Score { get; set; }
    public string Explanation { get; set; } = "";
    public List<string> SupportingEvidence { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class AIAnalysisResponse
{
    public string Id { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public List<object> Symptoms { get; set; } = new();
    public List<DiagnosisHypothesis> Hypotheses { get; set; } = new();
    public Dictionary<string, double> ConfidenceScores { get; set; } = new();
    public double OverallConfidence { get; set; }
    public string UrgencyLevel { get; set; } = "GREEN";
    public object? Explanation { get; set; }
    public List<string> Recommendations { get; set; } = new();
}