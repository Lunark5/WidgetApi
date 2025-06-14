using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using DonationAlertsApi;
using DonationAlertsApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed((host) => true)
            .AllowAnyHeader());
});

try
{
    builder.Configuration.AddJsonFile("users.json");
}
catch
{
    var json = JsonSerializer.Serialize(new UsersJson(), new JsonSerializerOptions { WriteIndented = true });
    
    await File.WriteAllTextAsync("users.json", json);
    
    builder.Configuration.AddJsonFile("users.json");
}

var userJson = builder.Configuration.Get<UsersJson>() ?? new UsersJson();
var users = userJson?.Users ?? new List<UserInfo>();
var app = builder.Build();

var lastUser = users.FirstOrDefault();
var client = new HttpClient();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.MapGet("/api/ping", () => Results.Ok())
    .WithName("Ping");

app.MapGet("/api/login/uri", (int appId = 0) =>
    {
        lastUser = users.FirstOrDefault(findUser => findUser.AppId == appId);

        if (lastUser == null)
        {
            return Results.BadRequest($"App with id: {appId} not found");
        }

        var uriString =
            $"https://www.donationalerts.com/oauth/authorize?client_id={lastUser.AppId}&redirect_uri={lastUser.RedirectUri}&response_type=code&scope={lastUser.Scope}"
                .Replace(" ", "+");
        
        return Results.Ok(uriString);
    })
    .WithName("LoginUri");

app.MapGet("/api/login", async (string code) =>
    {
        var values = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "client_id", lastUser.AppId.ToString() },
            { "client_secret", lastUser.ClientSecret },
            { "redirect_uri", lastUser.RedirectUri },
            { "code", code }
        };

        var content = new FormUrlEncodedContent(values);
        var response = await client.PostAsync("https://www.donationalerts.com/oauth/token", content);

        if (!response.IsSuccessStatusCode)
        {
            return Results.BadRequest(response.ReasonPhrase);
        }
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<UserOauth>(responseBody);
        
        var findUser = users.FirstOrDefault(findUser => findUser.AppId == lastUser.AppId);
        
        findUser.AccessToken = token?.AccessToken;
        findUser.RefreshToken = token?.RefreshToken;
        
        await SaveUsers(users);

        return Results.Content("<script>window.close();</script>", "text/html");
    })
    .WithName("Login");

app.MapGet("api/login/check", () => string.IsNullOrEmpty(lastUser?.AccessToken) 
        ? Results.BadRequest("Authorize not finished") 
        : Results.Ok())
.WithName("Check");

app.MapGet("/api/oauth", async (int appId) =>
    {
        var user = users.FirstOrDefault(findUser => findUser.AppId == appId);

        if (user == null)
        {
            return Results.BadRequest($"App not found");
        }

        try
        {
            var tokenBody = await TryGetOauth(user.AccessToken, 3);

            user.SocketConnectionToken = tokenBody.Data.SocketConnectionToken;
            user.UserId = tokenBody.Data.Id;

            await SaveUsers(users);

            return Results.Ok(new TokenObject()
            {
                AccessToken = user.AccessToken,
                SocketConnectionToken = tokenBody.Data.SocketConnectionToken,
                UserId = tokenBody.Data.Id
            });
        }
        catch (Exception ex)
        {
            users.Remove(user);
            SaveUsers(users);
            
            return Results.BadRequest(ex.Message);
        }
    })
    .WithName("Ouath");

app.MapGet("/api/widget", async (int appId) =>
{
    var user = users.FirstOrDefault(findUser => findUser.AppId == appId);

    if (user == null)
    {
        return Results.BadRequest($"App with id: {appId} not found");
    }
    
    try
    {
        var token = user.GoalWidgetUri.Query.Replace("?token=", string.Empty);
        var widgetId = user.GoalWidgetUri.Segments.Last();
        var widgetBody = await TryGetWidget(token, 3);
        var widgetData = await TryGetDonationGoal(widgetId, widgetBody.Data.Token, 3);
        
        return Results.Ok(widgetData.Data);
    }
    catch (Exception ex)
    {
        users.Remove(user);
        SaveUsers(users);
        
        return Results.BadRequest(ex.Message);
    }
})
.WithName("Widget");

app.MapPost("/api/register", async (User user) =>
    {
        if (users.Exists(findUser => findUser.AppId == user.AppId))
        {
            return Results.Ok("User already exists");
        }

        if (string.IsNullOrEmpty(app.Urls.First()))
        {
            return Results.InternalServerError("No url found");
        }
        
        users.Add(new UserInfo()
        {
            AppId = user.AppId,
            GoalWidgetUri = user.GoalWidgetUri,
            ClientSecret = user.ClientSecret,
            RedirectUri = $"{app.Urls.First()}/api/login",
            Scope = user.Scope
        });
        
        await SaveUsers(users);
        
        return Results.Ok();
    })
    .WithName("Register");

app.Run();

#region  Private methods
async Task SaveUsers(List<UserInfo> users)
{
    userJson.Users = users;

    var json = JsonSerializer.Serialize(userJson, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync("users.json", json);

}

async Task<OauthTokenBody> TryGetOauth(string accessToken, int currentTryNumber)
{
    if (currentTryNumber < 1)
    {
        throw new Exception("Connection error");
    }
    
    try
    {
        var message = new HttpRequestMessage(HttpMethod.Get, "https://www.donationalerts.com/api/v1/user/oauth");

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(message);
        var responseBody = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<OauthTokenBody>(responseBody);
    }
    catch
    {
        return await TryGetOauth(accessToken, currentTryNumber - 1);
    }
}

async Task<WidgetTokenBody> TryGetWidget(string widgetToken, int currentTryNumber)
{
    if (currentTryNumber < 1)
    {
        throw new Exception("Connection error");
    }
    
    try
    {
        var message = new HttpRequestMessage(HttpMethod.Get, $"https://www.donationalerts.com/api/v1/token/widget?token={widgetToken}");

        var response = await client.SendAsync(message);
        var responseBody = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<WidgetTokenBody>(responseBody);
    }
    catch(Exception ex)
    {
        return await TryGetWidget(widgetToken, currentTryNumber - 1);
    }
}

async Task<DonationGoalBody> TryGetDonationGoal(string widgetId, string token, int currentTryNumber)
{
    if (currentTryNumber < 1)
    {
        throw new Exception("Connection error");
    }
    
    try
    {
        var message = new HttpRequestMessage(HttpMethod.Get, $"https://www.donationalerts.com/api/v1/donationgoal/{widgetId}");
        
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(message);
        var responseBody = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<DonationGoalBody>(responseBody);
    }
    catch(Exception ex)
    {
        return await TryGetDonationGoal(widgetId, token, currentTryNumber - 1);
    }
}
#endregion