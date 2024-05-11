using Serilog;
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();  
builder.Services.AddSignalR();

var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
if (!Directory.Exists(logPath))
{
    Directory.CreateDirectory(logPath);
}

// Continue with setting up logging or other startup tasks


builder.Host.UseSerilog((hostingContext,services,loggerConfiguration) => {
    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration).WriteTo.Console().WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day);
});

// Register FingerprintService as a Singleton to ensure a single instance across the application
builder.Services.AddSingleton<FingerprintService>();  

var app = builder.Build();
var fingerprintService = app.Services.GetRequiredService<FingerprintService>();
fingerprintService.Initialize();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();  // Ensure controllers are used

app.MapHub<NotificationHub>("/notificationhub");

app.UseStaticFiles(); 


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
