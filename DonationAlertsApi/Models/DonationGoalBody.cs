using System.Text.Json.Serialization;

namespace DonationAlertsApi.Models;

public class DonationGoalBody
{
    [JsonPropertyName("data")]
    public DonationGoalData Data { get; set; }
}