using System.Text.Json.Serialization;

namespace DonationAlertsApi.Models;

public class DonationGoalData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("is_active")]
    public int IsActive { get; set; }

    [JsonPropertyName("is_default")]
    public int IsDefault { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("start_amount")] 
    public int StartAmount { get; set; }
    
    [JsonPropertyName("raised_amount")] 
    public int RaisedAmount { get; set; }
    
    [JsonPropertyName("goal_amount")] 
    public int GoalAmount { get; set; }
}