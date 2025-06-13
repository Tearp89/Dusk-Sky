using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ✅ Agrega servicios ANTES de Build
builder.Services.AddRazorPages();
builder.Services.AddHttpClient(); // Esto no es necesario si ya tienes la siguiente línea
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.BaseAddress = new Uri("http://authservice.local/auth/");

});

builder.Services.AddHttpClient<IGameService, GameService>(client =>
{
    client.BaseAddress = new Uri("http://games.local/api/game/"); 
});

builder.Services.AddHttpClient<ICommentService, CommentService>(client =>
{
    client.BaseAddress = new Uri("http://commentservice.local/comments"); 
});

builder.Services.AddHttpClient<IFriendshipService, FriendshipService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8007/friendships");

});
builder.Services.AddHttpClient<IGameListService, GameListService>(client =>
{
    client.BaseAddress = new Uri("http://gamelistservice.local/"); // Ajusta según tu puerto real
});

builder.Services.AddHttpClient<IGameListItemService, GameListItemService>(client =>
{
    client.BaseAddress = new Uri("http://gamelistservice.local/lists");
});

builder.Services.AddHttpClient<IModerationReportService, ModerationReportService>(client =>
{
    client.BaseAddress = new Uri("http://moderationservice.local/moderation/");
});

builder.Services.AddHttpClient<IModerationSanctionService, ModerationSanctionService>(client =>
{
     client.BaseAddress = new Uri("http://moderationservice.local/moderation/");
});

builder.Services.AddHttpClient<IReviewService, ReviewService>(client =>
{
    client.BaseAddress = new Uri("http://review.local/");
});



builder.Services.AddHttpClient<IUserManagerService, UserManagerService>(client =>
{
    client.BaseAddress = new Uri("http://usermanager.local/"); 
});

builder.Services.AddHttpClient<IGameTrackingService, GameTrackingService>(client =>
{
    client.BaseAddress = new Uri("http://track.local/track/"); 
});


builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});
builder.Services.AddScoped<UserSessionManager>(); 
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/StartPage"; // o donde esté tu login
        options.AccessDeniedPath = "/AccessDenied"; // opcional
    });

builder.Services.AddAuthorization();
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // o AddDebug si lo prefieres
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});




var app = builder.Build(); // ✅ Ahora sí construyes la app

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

var cultureInfo = new System.Globalization.CultureInfo("en-US");
cultureInfo.NumberFormat.NumberDecimalSeparator = ".";

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseCookiePolicy();
app.UseAuthentication(); // 👈 Obligatorio
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/StartPage"));
app.MapRazorPages();

app.Run();
