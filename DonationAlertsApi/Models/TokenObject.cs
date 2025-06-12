using System.Text.Json.Serialization;

namespace DonationAlertsApi.Models;

public class TokenObject
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    
    [JsonPropertyName("socket_connection_token")]
    public string SocketConnectionToken { get; set; }
}