using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System;
// using System.Net.Http; // Ya no necesario si no configuras handlers específicos aquí.

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddRazorPages();

// --- HttpClient Configurations (Todas las llamadas a Nginx serán HTTP) ---
// No hay lógica condicional para el ambiente, ya que siempre será HTTP.
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.BaseAddress = new Uri("http://nginx_gateway/auth/"); // <-- Asegura la barra final
});

builder.Services.AddHttpClient<IGameService, GameService>(client =>
{
    client.BaseAddress = new Uri("http://nginx_gateway/api/game/"); // Asegura la barra final
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddHttpClient<ICommentService, CommentService>(client =>
{
    client.BaseAddress = new Uri("http://nginx_gateway/comments/"); // <-- Añade barra final
    // Si tu backend espera /comments (sin barra), entonces el proxy_pass en Nginx debe ser http://commentservice_backend
    // y tu llamada en el servicio debe ser await _client.GetAsync("");
    // Es mejor que la base de la URL en Nginx incluya el segmento raíz.
    // Es decir, si el backend es reviews, y tus APIs son /reviews/recent,
    // tu BaseAddress debería ser http://nginx_gateway/reviews/
    // y tu llamada en el servicio GetAsync("recent")
});

builder.Services.AddHttpClient<IFriendshipService, FriendshipService>(client =>
{
    client.BaseAddress = new Uri("http://nginx_gateway/friendship/"); // <-- Añade barra final
});

builder.Services.AddHttpClient<IGameListService, GameListService>(client =>
{
    client.BaseAddress = new Uri("http://nginx_gateway/lists/"); // Asegura la barra final
});

builder.Services.AddHttpClient<IGameListItemService, GameListItemService>(client =>
{
    client.BaseAddress = new Uri("http://nginx_gateway/lists/"); // <-- Asegura la barra final
    // Y en tu GameListItemService.cs, la llamada sería algo como GetAsync("items") o GetAsync("{listId}/items")
});

builder.Services.AddHttpClient<IModerationReportService, ModerationReportService>(client =>
{
    client.BaseAddress = new Uri("http://nginx_gateway/moderation/"); // Asegura la barra final
});

builder.Services.AddHttpClient<IModerationSanctionService, ModerationSanctionService>(client =>
{
    client.BaseAddress = new Uri("http://nginx_gateway/moderation/"); // Asegura la barra final
});

builder.Services.AddHttpClient<IReviewService, ReviewService>(client =>
{
    client.BaseAddress = new Uri("http://nginx_gateway/reviews/"); // <-- Asegura la barra final
});

builder.Services.AddHttpClient<IUserManagerService, UserManagerService>(client =>
{
    client.BaseAddress = new Uri("http://nginx_gateway/profiles/"); // <-- Asegura la barra final
});

builder.Services.AddHttpClient<IGameTrackingService, GameTrackingService>(client =>
{
    client.BaseAddress = new Uri("http://nginx_gateway/track/"); // Asegura la barra final
});


// --- Configuraciones Estándar de Servicios de ASP.NET Core (se aplican independientemente del ambiente) ---
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
        // options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Puedes descomentar esto si nunca usarás HTTPS en el frontend, pero es mejor que Nginx se encargue
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

builder.WebHost.UseUrls("http://+:80"); // El puerto interno que tu servidor Kestrel del frontend escucha

var app = builder.Build();

// --- Configuración del Pipeline de Peticiones HTTP ---
// Puedes mantener la lógica de ambiente aquí para DeveloperExceptionPage si lo deseas
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Configuración de la cultura
var cultureInfo = new CultureInfo("en-US");
cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// app.UseHttpsRedirection(); // ¡Asegúrate de que esto siga COMENTADO! No queremos redirección HTTP a HTTPS interna.

app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapFallbackToPage("/StartPage");

app.Run();