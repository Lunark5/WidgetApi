using System.Text.Json.Serialization;

namespace DonationAlertsApi.Models;

public class WidgetTokenBody
{
    [JsonPropertyName("data")]
    public WidgetTokenData Data { get; set; }
}