using System.Net.WebSockets;
using System.Text;

using var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://localhost:5000/ws/"), CancellationToken.None);
Console.WriteLine("Connected to WebSocket server.");

// Start background receive task
_ = Task.Run(async () =>
{
    var buffer = new byte[1024];
    while (ws.State == WebSocketState.Open)
    {
        try
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"\n[Received]: {message}");
            Console.Write("Send: ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Receive error: {ex.Message}");
            break;
        }
    }
});

// Send loop
while (ws.State == WebSocketState.Open)
{
    Console.Write("Send: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;

    var bytes = Encoding.UTF8.GetBytes(input);
    await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
}
