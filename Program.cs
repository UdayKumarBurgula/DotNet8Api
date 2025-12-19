using static System.Net.Mime.MediaTypeNames;
using System.Text;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

int[] arr = { 10, 20, 30, 40 };
Span<int> s = arr.AsSpan(1, 2); // 20,30 (no new array)
s[0] = 99;                      // arr becomes 10,99,30,40

app.MapGet("/weatherforecast", async () =>
{

    using (FileStream destination = new FileStream(
            @"D:\Develop\Dotnet\temp\test.txt",
            FileMode.Create,
            FileAccess.ReadWrite))
    using (BufferedStream bufferedStream = new BufferedStream(destination, 128))
    {
        var text = "Blazing";

        ReadOnlyMemory<byte> memory =
            new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(text));

        await bufferedStream.WriteAsync(memory);
        await bufferedStream.FlushAsync();
    }

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
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}