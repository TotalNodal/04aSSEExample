using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static readonly List<HttpListenerResponse> clients = new List<HttpListenerResponse>();

    static async Task Main(string[] args)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();

        Console.WriteLine("SSE Server is listening...");

        Task.Run(() => SendSSEData());

        while (true)
        {
            var context = await listener.GetContextAsync();
            if (context.Request.Url.LocalPath == "/events")
            {
                HandleClient(context.Response);
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
            }
        }
    }

    static void HandleClient(HttpListenerResponse response)
    {
        response.StatusCode = 200;
        response.ContentType = "text/event-stream";
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");

        lock (clients)
        {
            clients.Add(response);
        }
    }

    static async Task SendSSEData()
    {
        while (true)
        {
            List<HttpListenerResponse> currentClients;
            lock (clients)
            {
                currentClients = new List<HttpListenerResponse>(clients);
            }

            foreach (var client in currentClients)
            {
                try
                {
                    var data = $"data: {DateTime.Now.ToString("HH:mm:ss")} - This is a real-time update\n\n";
                    var bytes = Encoding.UTF8.GetBytes(data);
                    await client.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                    await client.OutputStream.FlushAsync();
                }
                catch
                {
                    // Remove client if there's an error
                    lock (clients)
                    {
                        clients.Remove(client);
                    }
                }
            }

            await Task.Delay(1000); // Send updates every second
        }
    }
}