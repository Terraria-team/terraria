using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Client.Auth
{
    public class Browser : IDisposable
    {
        private static readonly int[] CandidatePorts = { 9000, 9001, 9002, 9003, 9004 };

        private readonly HttpListener _listener;
        public int Port { get; }

        public Browser()
        {
            (Port, _listener) = BindToFirstAvailablePort(CandidatePorts);
        }

        private static (int port, HttpListener listener) BindToFirstAvailablePort(int[] ports)
        {
            foreach (var port in ports)
            {
                try
                {
                    var listener = new HttpListener();
                    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                    listener.Start();
                    return (port, listener);
                }
                catch (HttpListenerException)
                {
                    // Port in use so retry
                }
            }

            throw new InvalidOperationException(
                $"Could not bind OAuth redirect listener on any of the ports: {string.Join(", ", ports)}. " +
                "Close any apps using those ports and try again.");
        }


        public async Task<string> WaitForRedirectAsync()
        {
            try
            {
                var context = await _listener.GetContextAsync();
                string redirectUrl = context.Request.Url.ToString();

                string html = @"
                    <html>
                        <body style='background-color:#121212;color:#fff;font-family:Segoe UI,sans-serif;text-align:center;padding-top:10%'>
                            <h1 style='color:#4CAF50;'>Authentication Successful!</h1>
                            <p>You can close this browser tab and return to Terraria.</p>
                            <script>setTimeout(function(){ window.close(); }, 2000);</script>
                        </body>
                    </html>";

                byte[] buffer = Encoding.UTF8.GetBytes(html);
                context.Response.ContentLength64 = buffer.Length;
                using var output = context.Response.OutputStream;
                await output.WriteAsync(buffer, 0, buffer.Length);

                return redirectUrl;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Browser] Error waiting for OAuth redirect: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            try { _listener.Stop(); } catch { /* ignored */ }
        }
    }
}
