using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace HttpWrapper
{
    partial class HttpWrapper
    {
        private static HttpListener listener   = new();
        private static string       url        = "http://localhost:55209/";
        private static string       blssBinary = "BeatLeaderScoreScanner";

        public static void Main(string[] args)
        {
            // Make sure binary exists
            if (!File.Exists(blssBinary) && !File.Exists(blssBinary + ".exe"))
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
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss zzz} | {request.HttpMethod} | {request.Url} | {request.UserHostName} | {request.UserAgent}");

                string? replayInput = request.QueryString.Get("input");
                if (string.IsNullOrWhiteSpace(replayInput))
                {
                    await Console.Out.WriteLineAsync("Rejecting due to malformed replayInput.");
                    await BadRequest(response);
                    continue;
                }

                int     page = 1;
                string? pageInput = request.QueryString.Get("page");
                if (!string.IsNullOrWhiteSpace(pageInput))
                {
                    if (!int.TryParse(pageInput, out page))
                    {
                        await Console.Out.WriteLineAsync("Rejecting due to malformed pageInput.");
                        await BadRequest(response);
                        continue;
                    }
                }

                float   minimumScore = 0;
                string? minimumScoreInput = request.QueryString.Get("minimum-score");
                if (!string.IsNullOrWhiteSpace(minimumScoreInput))
                {
                    if (!float.TryParse(minimumScoreInput, out minimumScore))
                    {
                        await Console.Out.WriteLineAsync("Rejecting due to malformed minimumScoreInput.");
                        await BadRequest(response);
                        continue;
                    }
                }

                bool requireScoreLoss = request.QueryString["require-score-loss"] != null;

                bool requireFc = request.QueryString["require-fc"] != null;

                var blssStartinfo = new ProcessStartInfo()
                {
                    FileName = blssBinary,
                    Arguments = $"--output-format json " +
                                (page != 1 ? $"--page {page} " : "") +
                                (minimumScore > 0 ? $"--minimum-score {minimumScore} " : "") +
                                (requireScoreLoss ? "--jitter-require-score-loss " : "") +
                                (requireFc ? "--require-fc " : "") +
                                $"-- {SanitizeForCommandLine(replayInput)}",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                };
                var blss = Process.Start(blssStartinfo);
                if (blss == null)
                {
                    await Console.Out.WriteLineAsync("Unable to start BLSS.");
                    await BadRequest(response);
                    continue;
                }

                var blssOut = await blss.StandardOutput.ReadToEndAsync();
                var blssErr = await blss.StandardError.ReadToEndAsync();
                blss.StandardOutput.Close();
                blss.StandardError .Close();

                if (!string.IsNullOrWhiteSpace(blssErr))
                {
                    await Console.Out.WriteLineAsync("Rejecting due to BLSS error. Output:\n" + blssErr);
                    await BadRequest(response);
                    continue;
                }

                byte[] data = Encoding.UTF8.GetBytes(blssOut);
                response.ContentType = "application/json";
                response.ContentEncoding = Encoding.UTF8;
                response.ContentLength64 = data.LongLength;
#if DEBUG
                response.Headers.Add("Access-Control-Allow-Origin", "*");
#endif

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
            if (string.IsNullOrWhiteSpace(input))
            {
                return "";
            }

            if (Uri.TryCreate(input, UriKind.Absolute, out Uri? result) && result != null)
            {
                input = HttpUtility.UrlEncode(input);
            }
            else
            {
                input = selectNonAscii().Replace(input, "");
                input = input.Split(" ")[0]
                             .TrimStart('-')
                             .Replace("\"", "")
                             .Replace("'" , "")
                             .Replace("$" , "")
                             .Replace("`" , "")
                             .Replace("\\", "")
                             .Replace("!" , "");
            }

            return input;
        }

        [GeneratedRegex(@"[^\u0020-\u007F]+")]
        private static partial Regex selectNonAscii();
    }
}
