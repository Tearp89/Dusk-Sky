var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();  // Agregar soporte para Razor Pages
builder.Services.AddHttpClient();   // <--- Agrega esta línea aquí

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapGet("/", () => Results.Redirect("/StartPage"));
app.UseStaticFiles();

app.MapRazorPages();  // Esto mapea Razor Pages a las rutas

app.Run();