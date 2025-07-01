using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System;

var builder = WebApplication.CreateBuilder(args);
var apiGatewayUrl = builder.Configuration.GetValue<string>("API_GATEWAY_URL") ?? "http://nginx_gateway/";

// Servicios
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    // Frontend -> Nginx -> Backend. Nginx espera /auth/
    client.BaseAddress = new Uri(apiGatewayUrl + "auth/");
});

builder.Services.AddHttpClient<IGameService, GameService>(client =>
{
    // Frontend -> Nginx -> Backend. Nginx espera /api/game/
    client.BaseAddress = new Uri(apiGatewayUrl + "api/game/");
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddHttpClient<ICommentService, CommentService>(client =>
{
    // Frontend -> Nginx -> Backend. Nginx espera /comments/
    client.BaseAddress = new Uri(apiGatewayUrl + "comments/"); // Asegura la barra final
});

builder.Services.AddHttpClient<IFriendshipService, FriendshipService>(client =>
{
    // Frontend -> Nginx -> Backend. Nginx espera /friendship/
    client.BaseAddress = new Uri(apiGatewayUrl + "friendship/");
});

builder.Services.AddHttpClient<IGameListService, GameListService>(client =>
{
    // Frontend -> Nginx -> Backend. Nginx espera /lists/
    client.BaseAddress = new Uri(apiGatewayUrl + "lists/");
});

builder.Services.AddHttpClient<IGameListItemService, GameListItemService>(client =>
{
    // Frontend -> Nginx -> Backend. Nginx espera /lists/ (para items específicos de listas)
    // Tu Nginx tiene location ^~ /lists/, y este servicio hace llamadas como GetItemsByListIdAsync(list.Id)
    // Si tus rutas en el backend son /lists/{id}/items, entonces el BaseAddress es /lists/
    client.BaseAddress = new Uri(apiGatewayUrl + "lists/");
});

builder.Services.AddHttpClient<IModerationReportService, ModerationReportService>(client =>
{
    // Frontend -> Nginx -> Backend. Nginx espera /moderation/
    client.BaseAddress = new Uri(apiGatewayUrl + "moderation/");
});

builder.Services.AddHttpClient<IModerationSanctionService, ModerationSanctionService>(client =>
{
    // Frontend -> Nginx -> Backend. Nginx espera /moderation/
    client.BaseAddress = new Uri(apiGatewayUrl + "moderation/");
});

builder.Services.AddHttpClient<IReviewService, ReviewService>(client =>
{
    // Frontend -> Nginx -> Backend. Nginx espera /reviews/
    client.BaseAddress = new Uri(apiGatewayUrl + "reviews/");
});

builder.Services.AddHttpClient<IUserManagerService, UserManagerService>(client =>
{
    // Frontend -> Nginx -> Backend. Nginx espera /api/users/ (basado en la última corrección de colisión de rutas)
    client.BaseAddress = new Uri(apiGatewayUrl + "profiles/");
});

builder.Services.AddHttpClient<IGameTrackingService, GameTrackingService>(client =>
{
    // Frontend -> Nginx -> Backend. Nginx espera /api/track/ (basado en la última corrección de colisión de rutas)
    client.BaseAddress = new Uri(apiGatewayUrl + "track/");
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