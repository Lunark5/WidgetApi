using System.Text.Json.Serialization;

namespace DonationAlertsApi.Models;

public class WidgetTokenData
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
}