using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ComunidadHispanoRustAPI", "AnotherPanda", "1.0.0")]

    class ComunidadHispanoRustAPI : CovalencePlugin
    {
        private HttpListener httpListener;
        private const string apiKey = "rusterosunidosjamasseranvencidos";
        private const int serverPort = 28017;

        #region Initialization and HTTP Server

        private void Init()
        {
            StartHttpServer();
        }

        private void Unload()
        {
            StopHttpServer();
            Puts("HTTP server successfully stopped.");
        }

        private void StartHttpServer()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://*:{serverPort}/");
            httpListener.Start();
            httpListener.BeginGetContext(OnRequest, httpListener);
            Puts($"HTTP server started on port {serverPort}");
        }

        private void StopHttpServer()
        {
            if (httpListener != null)
            {
                try
                {
                    httpListener.Stop();
                    httpListener.Close();
                    httpListener = null;
                    Puts("The HTTP server has been successfully closed.");
                }
                catch (Exception ex)
                {
                    Puts($"Error closing the HTTP server: {ex.Message}");
                }
            }
        }

        #endregion

        #region HTTP Request Handling

        private void OnRequest(IAsyncResult result)
        {
            if (!httpListener.IsListening) return;

            HttpListenerContext context = httpListener.EndGetContext(result);
            httpListener.BeginGetContext(OnRequest, httpListener);

            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string responseText = "Invalid request";
            int statusCode = 400;

            try
            {
                if (request.Headers["Authorization"] != apiKey)
                {
                    responseText = "Unauthorized";
                    statusCode = 401;
                }
                else
                {
                    if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/stats")
                    {
                        responseText = GetServerStats();
                        statusCode = 200;
                    }
                    else if (request.HttpMethod == "POST")
                    {
                        using (StreamReader reader = new StreamReader(request.InputStream))
                        {
                            string requestBody = reader.ReadToEnd();
                            Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestBody);

                            // Implementation of POST endpoints
                        }
                    }
                }
            }
            catch (Exception e)
            {
                responseText = $"Error: {e.Message}";
                statusCode = 500;
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseText);
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        #endregion

        #region API Functions

        private string GetServerStats()
        {
            int onlinePlayers = players.Connected.Count();
            int maxPlayers = ConVar.Server.maxplayers;

            return JsonConvert.SerializeObject(new
            {
                players = $"{onlinePlayers}/{maxPlayers}",
                server = server.Name
            });
        }

        #endregion
    }
}
