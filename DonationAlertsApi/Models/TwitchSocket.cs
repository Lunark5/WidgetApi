using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace DonationAlertsApi.Models;

public class TwitchSocket
{
    private string _token;
    private string _username;
    private TwitchClient _client;
    
    public TwitchSocket(string username, string token)
    {
        var credentials = new ConnectionCredentials(username, token);
        var clientOptions = new ClientOptions();
        var customClient = new WebSocketClient(clientOptions);
        
        _client = new TwitchClient(customClient);
        _client.Initialize(credentials);
        
        _client.OnJoinedChannel += Client_OnJoinedChannel;
        _client.OnMessageReceived += Client_OnMessageReceived;
        _client.OnNewSubscriber += Client_OnNewSubscriber;
        _client.OnConnected += Client_OnConnected;
        
        _client.Connect();
    }
    
    private async Task Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
    {
        Console.WriteLine(e.ChatMessage.Message);
    }
  
    private async Task Client_OnConnected(object sender, OnConnectedArgs e)
    {
        Console.WriteLine($"Connected to {e.AutoJoinChannel}");
    }
  
    private async Task Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
    {
        Console.WriteLine("Hey guys! I am a bot connected via TwitchLib!");
        _client.SendMessage(e.Channel, "Hey guys! I am a bot connected via TwitchLib!");
    }
        
    private async Task Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
    {
        if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
            _client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
        else
            _client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
    }
}