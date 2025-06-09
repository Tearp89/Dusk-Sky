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
    client.BaseAddress = new Uri("http://localhost:8008/comments"); 
});

builder.Services.AddHttpClient<IFriendshipService, FriendshipService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8007/friendships");

});
builder.Services.AddHttpClient<IGameListService, GameListService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8004"); // Ajusta según tu puerto real
});

builder.Services.AddHttpClient<IGameListItemService, GameListItemService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8004");
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
    client.BaseAddress = new Uri("https://review.local/"); // o http://localhost:PORT/
});

builder.Services.AddHttpClient<IUserManagerService, UserManagerService>(client =>
{
    client.BaseAddress = new Uri("https://usermanager.local/"); // o http://localhost:8003/
});


builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});

var app = builder.Build(); // ✅ Ahora sí construyes la app

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCookiePolicy();

app.MapGet("/", () => Results.Redirect("/StartPage"));
app.MapRazorPages();

app.Run();
