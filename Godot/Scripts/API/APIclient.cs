using Godot;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace GridironGM.Client.Api
{
    public partial class ApiClient : Node, IBackendClient
    {
        [Export] public string BaseUrl = "http://127.0.0.1:8765";

        public Task<(int status, string body)> GetAsync(string path)
            => RequestAsync(HttpClient.Method.Get, path, null);

        public Task<(int status, string body)> PostAsync(string path, string jsonBody = "{}")
            => RequestAsync(HttpClient.Method.Post, path, jsonBody);

        public void AttachProcess(Process process)
        {
            // HTTP client does not attach to a backend process.
        }

        private Task<(int status, string body)> RequestAsync(HttpClient.Method method, string path, string jsonBody)
        {
            var tcs = new TaskCompletionSource<(int, string)>();

            var url = BaseUrl.TrimEnd('/') + path;
            var headers = new string[] { "Content-Type: application/json" };

            string bodyString = "";
            if (method != HttpClient.Method.Get && jsonBody != null)
                bodyString = jsonBody;

            var req = new HttpRequest();
            AddChild(req);

            void Handler(long result, long responseCode, string[] responseHeaders, byte[] responseBody)
            {
                req.RequestCompleted -= Handler;
                var text = Encoding.UTF8.GetString(responseBody);
                if (result != (long)HttpRequest.Result.Success)
                {
                    var message = BuildErrorMessage(url, $"Request failed: {(HttpRequest.Result)result}", text);
                    tcs.TrySetResult((0, message));
                }
                else if (responseCode < 200 || responseCode >= 300)
                {
                    var message = BuildErrorMessage(url, $"HTTP {responseCode}", text);
                    tcs.TrySetResult(((int)responseCode, message));
                }
                else
                {
                    tcs.TrySetResult(((int)responseCode, text));
                }
                req.QueueFree();
            }

            req.RequestCompleted += Handler;

            try
            {
                var err = req.Request(url, headers, method, bodyString);
                if (err != Error.Ok)
                {
                    req.RequestCompleted -= Handler;
                    tcs.TrySetResult((0, BuildErrorMessage(url, $"Request start failed: {err}", "")));
                    req.QueueFree();
                }
            }
            catch (Exception ex)
            {
                req.RequestCompleted -= Handler;
                tcs.TrySetResult((0, BuildErrorMessage(url, $"Exception: {ex.Message}", "")));
                req.QueueFree();
            }

            return tcs.Task;
        }

        private static string BuildErrorMessage(string url, string detail, string responseBody)
        {
            var message = $"Request failed for {url}: {detail}";
            if (!string.IsNullOrWhiteSpace(responseBody))
                message += $"\nResponse: {responseBody}";
            return message;
        }
    }
}
