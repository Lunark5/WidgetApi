namespace DonationAlertsApi;

public class User
{
    public int AppId { get; set; }
    
    public string RedirectUri { get; set; }
    
    public string ClientSecret { get; set; }
    
    public Uri GoalWidgetUri { get; set; }

    public string Scope { get; set; } =
        "oauth-donation-index oauth-user-show oauth-donation-subscribe oauth-goal-subscribe";
}