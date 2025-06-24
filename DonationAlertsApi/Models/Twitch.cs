namespace DonationAlertsApi.Models;

public class Twitch
{
    public string ClientId { get; set; }
    
    public string ClientSecret { get; set; }
    
    public string Scope { get; set; }
    
    public string RedirectUri { get; set; }
    
    public string AuthCode { get; set; }
    
    public string AccessToken { get; set; }
    
    public string RefreshToken { get; set; }
    
    public int ExpiresIn { get; set; }

}