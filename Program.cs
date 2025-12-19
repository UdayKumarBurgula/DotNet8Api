using DotNet8Api;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// read and normalize path, then call the AsyncExamples reader that accepts a file path
var configured = builder.Configuration["Export:FilePath"];
if (string.IsNullOrWhiteSpace(configured))
    throw new InvalidOperationException("Export:FilePath is not configured.");

var filePath = Path.IsPathRooted(configured)
    ? configured
    : Path.Combine(builder.Environment.ContentRootPath, configured);

//// use the existing helper that reads by path
//var fileContents = await AsyncExamples.AsyncAwaitExample_ReadFileAsync(filePath);
//Console.WriteLine(fileContents);

//// OR call the IConfiguration-based helper directly:
//var fileContents2 = await AsyncExamples.ReadFileFromConfigurationAsync(builder.Configuration);

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

// Original weather endpoint (unchanged behavior)
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

// Add these two endpoints (place before app.Run())

app.MapGet("/books", async (Microsoft.AspNetCore.Hosting.IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.ContentRootPath, "upload", "books.xml");
    if (!File.Exists(path))
        return Results.NotFound($"File not found: {path}");

    var content = await AsyncExamples.AsyncAwaitExample_ReadFileAsync(path);
    return Results.Content(content, "application/xml");
})
.WithName("GetBooksXml");

app.MapGet("/exports", async (Microsoft.Extensions.Configuration.IConfiguration config, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env) =>
{
    // read configured path or fall back to upload/export.json
    var configured = config["Export:FilePath"];
    var filePath = string.IsNullOrWhiteSpace(configured)
        ? Path.Combine(env.ContentRootPath, "upload", "export.json")
        : (Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured));

    if (!File.Exists(filePath))
        return Results.NotFound($"File not found: {filePath}");

    var content = await AsyncExamples.AsyncAwaitExample_ReadFileAsync(filePath);
    return Results.Content(content, "application/json");
})
.WithName("GetExportsJson");

// New minimal-API route that calls AsyncExamples.AsyncAwaitExample()
app.MapGet("/async-simple", async () =>
{
    // call the example async method in AsyncExamples
    var result = await AsyncExamples.AsyncAwaitExample_MultipleApiCalls().ConfigureAwait(false);
    return Results.Ok(result);
})
.WithName("AsyncAwaitExample");


//// New demo endpoint that explains async/await internals with runnable examples.
//app.MapGet("/async-demo", async () =>
//{
//    await AsyncExamples.AsyncAwaitExample();

//    // 1) High-level async/await (what you normally write)
//    // Compiler transforms this into a state machine behind the scenes.
//    //string highLevel = await AsyncAwaitExample();

//    // 2) Low-level manual awaiter usage that mimics what the compiler does.
//    // This shows the three core steps:
//    //   a) call GetAwaiter()
//    //   b) check IsCompleted
//    //   c) if not completed, attach a continuation via OnCompleted (or UnsafeOnCompleted)
//    //string manual = await ManualAwaitExample();

//    return new { highLevel, manual };
//})
//.WithName("AsyncInternalsDemo");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


// Move the ManualAwaitExample method above the top-level statements to fix CS8803

// -----------------------------
// Async internals examples
// -----------------------------

// 2) Manual implementation using awaiter and TaskCompletionSource
// This demonstrates the same algorithm the compiler-generated state machine uses.
//static Task<string> ManualAwaitExample()
//{
//    var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

//    // Start an asynchronous operation
//    Task delayTask = Task.Delay(100);

//    // Step a) Get the awaiter from the task
//    var awaiter = delayTask.GetAwaiter();

//    // Step b) Check if the awaiter already completed
//    if (awaiter.IsCompleted)
//    {
//        // Completed synchronously: get result (also rethrows exceptions) and set the result.
//        try
//        {
//            awaiter.GetResult(); // propagates exception if any
//            tcs.SetResult("Manual Done (sync path)");
//        }
//        catch (Exception ex)
//        {
//            tcs.SetException(ex);
//        }
//    }
//    else
//    {
//        // Step c) Not completed: schedule a continuation
//        // OnCompleted will be invoked when the awaiter completes.
//        awaiter.OnCompleted(() =>
//        {
//            // This continuation is analogous to the state machine's MoveNext method.
//            try
//            {
//                // GetResult will rethrow any exceptions that happened inside the awaited task.
//                awaiter.GetResult();
//                tcs.SetResult("Manual Done (continuation path)");
//            }
//            catch (Exception ex)
//            {
//                tcs.SetException(ex);
//            }
//        });
//    }

//    // Return a Task that completes when we call SetResult/SetException on the TCS.
//    return tcs.Task;
//}