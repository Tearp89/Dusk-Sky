var builder = WebApplication.CreateBuilder(args);

// ✅ Agrega servicios ANTES de Build
builder.Services.AddRazorPages();
builder.Services.AddHttpClient(); // Esto no es necesario si ya tienes la siguiente línea
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8001/auth/");
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
