namespace DonationAlertsApi.Models;

public class UserInfo : User
{
    public string AccessToken { get; set; }
    
    public string RefreshToken { get; set; }
    
    public string SocketConnectionToken { get; set; }
    
    public int UserId { get; set; }
}