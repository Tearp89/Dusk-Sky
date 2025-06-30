using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    // Corrected: auth_service listens on 8000 internally
    client.BaseAddress = new Uri("http://auth_service:8000/auth/");
});

builder.Services.AddHttpClient<IGameService, GameService>(client =>
{
    // Corrected: game_service_app (which maps to game_service service name) listens on 80 internally
    client.BaseAddress = new Uri("http://game_service:80/api/game/"); // Or just "http://game_service/api/game/"
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddHttpClient<ICommentService, CommentService>(client =>
{
    // Corrected: commentservice listens on 80 internally
    client.BaseAddress = new Uri("http://commentservice:80/comments"); // Or just "http://commentservice/comments"
});

builder.Services.AddHttpClient<IFriendshipService, FriendshipService>(client =>
{
    // Corrected: friendshipservice (which maps to friendship_service service name) listens on 8006 internally
    client.BaseAddress = new Uri("http://friendship_service:8006/");
});

builder.Services.AddHttpClient<IGameListService, GameListService>(client =>
{
    // Corrected: gamelistservice listens on 80 internally
    client.BaseAddress = new Uri("http://gamelistservice:80/"); // Or just "http://gamelistservice/"
});

builder.Services.AddHttpClient<IGameListItemService, GameListItemService>(client =>
{
    // Corrected: gamelistservice listens on 80 internally
    client.BaseAddress = new Uri("http://gamelistservice:80/lists"); // Or just "http://gamelistservice/lists"
});

builder.Services.AddHttpClient<IModerationReportService, ModerationReportService>(client =>
{
    // Corrected: moderationservice listens on 80 internally
    client.BaseAddress = new Uri("http://moderationservice:80/moderation/"); // Or just "http://moderationservice/moderation/"
});

builder.Services.AddHttpClient<IModerationSanctionService, ModerationSanctionService>(client =>
{
    // Corrected: moderationservice listens on 80 internally
    client.BaseAddress = new Uri("http://moderationservice:80/moderation/"); // Or just "http://moderationservice/moderation/"
});

builder.Services.AddHttpClient<IReviewService, ReviewService>(client =>
{
    // **********************************************
    // CRITICAL FIX: reviewservice listens on 8000 internally
    client.BaseAddress = new Uri("http://reviewservice:8000/");
    // **********************************************
});

builder.Services.AddHttpClient<IUserManagerService, UserManagerService>(client =>
{
    // **********************************************
    // CRITICAL FIX: user_manager_service (service name) listens on 8000 internally
    client.BaseAddress = new Uri("http://user_manager_service:8000/");
    // **********************************************
});

builder.Services.AddHttpClient<IGameTrackingService, GameTrackingService>(client =>
{
    // Corrected: user_game_tracking_service listens on 8000 internally
    client.BaseAddress = new Uri("http://user_game_tracking_service:8005/");
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});

builder.Services.AddScoped<UserSessionManager>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/StartPage";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        // options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    });

builder.Services.AddAuthorization();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.WebHost.UseUrls("http://+:80");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

var cultureInfo = new CultureInfo("en-US");
cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// app.UseHttpsRedirection(); // Comentado para Docker sin SSL

app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapFallbackToPage("/StartPage");

app.Run();