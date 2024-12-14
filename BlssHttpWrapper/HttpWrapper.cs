using System.Diagnostics;
using System.Net;
using System.Text;

namespace HttpWrapper
{
    class HttpWrapper
    {
        private static HttpListener listener = new HttpListener();
        private static string url = "http://localhost:55209/";
        private static string blssBinary = "BeatleaderScoreScanner";

        public static void Main(string[] args)
        {
            // Make sure binary exists
            if(!File.Exists(blssBinary) && !File.Exists(blssBinary + ".exe"))
            {
                Console.Error.WriteLine("Unable to start server, " + blssBinary + " not found.");
                return;
            }

            // Start listening for incoming connections
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                HttpListenerContext  context  = await listener.GetContextAsync();
                HttpListenerRequest  request  = context.Request;
                HttpListenerResponse response = context.Response;

                // Log
                Console.WriteLine($"{request.HttpMethod} | {request.Url} | {request.UserHostName} | {request.UserAgent}");

                string? replayInput = request.QueryString.Get("input");
                if(string.IsNullOrWhiteSpace(replayInput))
                {
                    await BadRequest(response);
                    continue;
                }

                int page = 1;
                string? pageInput = request.QueryString.Get("page");
                if (!string.IsNullOrWhiteSpace(pageInput))
                {
                    if (!int.TryParse(pageInput, out page))
                    {
                        await BadRequest(response);
                        continue;
                    }
                }

                var blssStartinfo = new ProcessStartInfo()
                {
                    FileName = blssBinary,
                    Arguments = $"--output-format json --page {page} {SanitizeForCommandLine(replayInput)}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                var blss = Process.Start(blssStartinfo);

                List<string> outputLines = new();
                string? line;
                while((line = await blss!.StandardOutput.ReadLineAsync()) != null)
                {
                    outputLines.Add(line);
                }
                var blssErr = await blss.StandardError.ReadToEndAsync();
                blss.StandardOutput.Close();
                blss.StandardError .Close();

                if (!string.IsNullOrWhiteSpace(blssErr))
                {
                    await BadRequest(response);
                    continue;
                }

                byte[] data = Encoding.UTF8.GetBytes("[" + string.Join(',', outputLines) + "]");
                response.ContentType = "application/json";
                response.ContentEncoding = Encoding.UTF8;
                response.ContentLength64 = data.LongLength;

                await response.OutputStream.WriteAsync(data, 0, data.Length);
                response.Close();
            }
        }

        public static async Task BadRequest(HttpListenerResponse response)
        {
            byte[] data = Encoding.UTF8.GetBytes("Bad request.");
            response.ContentType = "text/plain";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = data.LongLength;
            response.StatusCode = 400;
            await response.OutputStream.WriteAsync(data, 0, data.Length);
            response.Close();
        }

        public static string SanitizeForCommandLine(string? input)
        {
            if(string.IsNullOrWhiteSpace(input))
            {
                return "";
            }

            return input.Split(" ")[0]
                        .TrimStart('-')
                        .Replace("\"", "")
                        .Replace("'", "")
                        .Replace("$", "")
                        .Replace("`", "")
                        .Replace("\\", "");
        }
    }
}
