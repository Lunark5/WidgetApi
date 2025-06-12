using System.Text.Json.Serialization;

namespace DonationAlertsApi.Models;

public class OauthTokenData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("code")]
    public string Code { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("is_active")]
    public int IsActive { get; set; }
    
    [JsonPropertyName("avatar")]
    public string Avatar { get; set; }

    [JsonPropertyName("email")] 
    public string Email { get; set; }
    
    [JsonPropertyName("language")]
    public string Language { get; set; }
    
    [JsonPropertyName("socket_connection_token")]
    public string SocketConnectionToken { get; set; }
}