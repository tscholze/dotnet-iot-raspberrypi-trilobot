using TriloBot.Blazor.Components;
using TriloBot.Blazor.SignalR;
using Microsoft.Extensions.FileProviders;
using LiveStreamingServerNet;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddSingleton<TriloBot.TriloBot>()
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Ensure SignalR is added to the service collection.
builder.Services.AddSignalR();
builder.Services.AddCors();

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

// Add middleware to serve the /photos directory as static files
var photosPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "photos");
if (!Directory.Exists(photosPath))
{
    Directory.CreateDirectory(photosPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(photosPath),
    RequestPath = "/photos"
});

app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());


var cancellationToken = new CancellationTokenSource();

_ = Task.Run(async () =>
{
    using var server = LiveStreamingServerBuilder.Create()
        .ConfigureLogging(options => options.AddConsole())
        .Build();

    await server.RunAsync(new IPEndPoint(IPAddress.Any, 1935), cancellationToken.Token);
}, cancellationToken.Token);

Console.CancelKeyPress += (s, e) =>
{
    cancellationToken.Cancel();
    e.Cancel = true; // Prevent the process from terminating immediately
};

app.Run();
