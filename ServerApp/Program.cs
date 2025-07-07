using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;

class Program
{
    private static readonly ConcurrentDictionary<int, WebSocket> clients = new();
    private static int nextClientId = 1;
    private static readonly object idLock = new();

    public static async Task Main(string[] args)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/ws/");
        listener.Start();
        Console.WriteLine("Server started at ws://localhost:5000/ws/");

        // ✅ Start both tasks
        var listenerTask = AcceptClientsAsync(listener);
        var logTask = LogStatusLoopAsync();

        await Task.WhenAll(listenerTask, logTask);
    }

    private static async Task AcceptClientsAsync(HttpListener listener)
    {
        while (true)
        {
            var context = await listener.GetContextAsync();
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                continue;
            }

            var wsContext = await context.AcceptWebSocketAsync(null);
            var ws = wsContext.WebSocket;

            int clientId;
            lock (idLock) { clientId = nextClientId++; }

            clients.TryAdd(clientId, ws);
            Console.WriteLine($"Client connected: {clientId}");

            _ = Task.Run(() => HandleClientAsync(clientId, ws));
        }
    }

    private static async Task HandleClientAsync(int clientId, WebSocket ws)
    {
        var buffer = new byte[1024];
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"[{clientId}] says: {raw}");

                // Parse message for target IDs
                string[] parts = raw.Split(':', 2);
                string[]? targets = null;
                string payload;

                if (parts.Length == 2 && parts[0].Contains(','))
                {
                    // Format: 1,2,3:Hello
                    targets = parts[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    payload = parts[1];
                }
                else if (parts.Length == 2 && int.TryParse(parts[0], out _))
                {
                    // Format: 5:Hi -> single target
                    targets = new[] { parts[0].Trim() };
                    payload = parts[1];
                }
                else
                {
                    // No target, broadcast
                    payload = raw;
                }

                var fullMessage = Encoding.UTF8.GetBytes($"From {clientId}: {payload}");
                var msgSegment = new ArraySegment<byte>(fullMessage);

                if (targets == null)
                {
                    // Broadcast
                    foreach (var kv in clients)
                    {
                        if (kv.Value.State == WebSocketState.Open)
                            await kv.Value.SendAsync(msgSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                else
                {
                    // Send to selected targets
                    foreach (var idStr in targets)
                    {
                        if (int.TryParse(idStr, out int targetId) &&
                            clients.TryGetValue(targetId, out var targetWs) &&
                            targetWs.State == WebSocketState.Open)
                        {
                            await targetWs.SendAsync(msgSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        else
                        {
                            Console.WriteLine($"Client {idStr} not found or closed.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client {clientId} error: {ex.Message}");
        }

        clients.TryRemove(clientId, out _);
        Console.WriteLine($"Client disconnected: {clientId}");
    }

    private static async Task LogStatusLoopAsync()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine($"[{DateTime.Now}] Connected Clients: {clients.Count}");

            foreach (var kv in clients)
            {
                Console.WriteLine($"  Client {kv.Key} - State: {kv.Value.State}");
            }

            Console.WriteLine("Waiting for messages... (Press Ctrl+C to quit)");
            await Task.Delay(5000); // Refresh every 5 seconds
        }
    }
}
