using TriloBot.Blazor.Components;
using TriloBot.Blazor.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddSingleton<TriloBot.TriloBot>()
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Ensure SignalR is added to the service collection.
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Map SignalR hub.
app.MapHub<TriloBotHub>("/trilobotHub");

app.UseHttpsRedirection();

// Ensure Antiforgery middleware is properly configured.
app.UseAntiforgery();

// Map static assets and Razor components.
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
