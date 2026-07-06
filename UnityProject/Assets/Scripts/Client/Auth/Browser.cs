using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.OidcClient.Browser;
using UnityEngine;

namespace Client.Auth
{
    public class Browser : IBrowser
    {
        public int Port { get; private set; }

        public Browser()
        {
            Port = GetAvailablePort();
        }
        
        private static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            
            listener.Start();
            int assignedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            
            return assignedPort;
        }

        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            listener.Start();

            Application.OpenURL(options.StartUrl);

            try
            {
                var context = await listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                string responseString = @"
                    <html>
                        <body style='background-color: #121212; color: #ffffff; font-family: Segoe UI, sans-serif; text-align: center; padding-top: 10%'>
                            <h1 style='color: #4CAF50;'>Authentication Successful!</h1>
                            <p>You can close this browser tab and return to Terraria.</p>
                            <script>setTimeout(function() { window.close(); }, 2000);</script>
                        </body>
                    </html>";

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                using var output = response.OutputStream;
                await output.WriteAsync(buffer, 0, buffer.Length, cancellationToken);

                return new BrowserResult
                {
                    ResultType = BrowserResultType.Success,
                    Response = request.Url.ToString()
                };
            }
            catch (Exception ex)
            {
                return new BrowserResult
                {
                    ResultType = BrowserResultType.UnknownError,
                    Error = ex.Message
                };
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
