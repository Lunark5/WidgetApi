using System.Text.Json.Serialization;

namespace DonationAlertsApi.Models;

public class OauthTokenBody
{
    [JsonPropertyName("data")]
    public OauthTokenData Data { get; set; }
}