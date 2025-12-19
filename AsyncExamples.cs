using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace DotNet8Api
{
    public static class AsyncExamples
    {
        /// <summary>
        /// Simple async/await example moved to a separate class.
        /// Call: await AsyncExamples.AsyncAwaitExample();
        /// </summary>
        public static async Task<string> AsyncAwaitExample()
        {
            // Simulate asynchronous work
            await Task.Delay(150).ConfigureAwait(false);

            // Return a value after the await completes
            return "AsyncAwait Done (from AsyncExamples)";
        }

        /// <summary>
        /// Calls https://jsonplaceholder.typicode.com/posts and returns the JSON payload as string.
        /// Prefer passing an HttpClient (from DI) to reuse connections.
        /// </summary>
        public static async Task<string> AsyncAwaitExample_APICall(HttpClient? httpClient = null)
        {
            var disposeClient = false;
            if (httpClient is null)
            {
                httpClient = new HttpClient();
                disposeClient = true;
            }

            try
            {
                using var response = await httpClient.GetAsync("https://jsonplaceholder.typicode.com/posts").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return content;
            }
            finally
            {
                if (disposeClient)
                {
                    httpClient.Dispose();
                }
            }
        }

        public static Task<string> ManualAwaitExample()
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            // Start an asynchronous operation
            Task.Delay(150).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.SetException(t.Exception!);
                }
                else
                {
                    tcs.SetResult("ManualAwait Done (from AsyncExamples)");
                }
            });
            return tcs.Task;
        }

        public static async Task<string> AsyncAwaitExample_DummyApiCall()
        {   // Simulate an asynchronous API call
            await Task.Delay(200).ConfigureAwait(false);
            return "Dummy API Call Result";
        }

        public static async Task<string> AsyncAwaitExample_MultipleApiCalls()
        {
            var task1 = AsyncAwaitExample_APICall();
            var task2 = AsyncAwaitExample_APICall();
            var task3 = AsyncAwaitExample_APICall();
            var results = await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);
            return string.Join(", ", results);
        }

        public static async Task<string> AsyncAwaitExample_FirstApiCall()
        {
            var task1 = AsyncAwaitExample_DummyApiCall();
            var task2 = AsyncAwaitExample_DummyApiCall();
            var task3 = AsyncAwaitExample_DummyApiCall();
            var firstCompleted = await Task.WhenAny(task1, task2, task3).ConfigureAwait(false);
            return await firstCompleted.ConfigureAwait(false);
        }

        public static async Task<string> AsyncAwaitExample_ExceptionHandling()
        {
            try
            {
                await Task.Delay(100).ConfigureAwait(false);
                throw new InvalidOperationException("Simulated exception in async method");
            }
            catch (Exception ex)
            {
                return $"Caught exception: {ex.Message}";
            }
        }

        public static async Task<string> AsyncAwaitExample_ParallelApiCalls()
        {
            var tasks = new Task<string>[3];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = AsyncAwaitExample_DummyApiCall();
            }
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return string.Join(", ", results);
        }

        public static async Task<string> AsyncAwaitExample_CancellationToken(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                return "Completed without cancellation";
            }
            catch (OperationCanceledException)
            {
                return "Operation was cancelled";
            }
        }

        public static async Task<string> AsyncAwaitExample_ProgressReporting(IProgress<int> progress)
        {
            for (int i = 0; i <= 100; i += 20)
            {
                await Task.Delay(500).ConfigureAwait(false);
                progress.Report(i);
            }
            return "Progress reporting completed";
        }

        public static async Task<string> AsyncAwaitExample_ReadFileAsync(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string content = await reader.ReadToEndAsync().ConfigureAwait(false);
                return content;
            }
        }

        /// <summary>
        /// Read file path from IConfiguration and return file contents asynchronously.
        /// Example config key: "Export:FilePath" or pass a different key.
        /// Call from Program.cs: await AsyncExamples.ReadFileFromConfigurationAsync(builder.Configuration);
        /// </summary>
        public static async Task<string> ReadFileFromConfigurationAsync(IConfiguration configuration, string configKey = "Export:FilePath")
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            var filePath = configuration[configKey];
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new InvalidOperationException($"Configuration key '{configKey}' is missing or empty.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Configured file not found.", filePath);
            }

            using var reader = new StreamReader(filePath);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }
    }
}