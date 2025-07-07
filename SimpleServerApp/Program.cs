using System.Net;
using System.Net.WebSockets;
using System.Text;

var listener = new HttpListener();
listener.Prefixes.Add("http://localhost:5000/ws/");
listener.Start();
Console.WriteLine("WebSocket server started at ws://localhost:5000/ws/");

while (true)
{
    var context = await listener.GetContextAsync();

    if (context.Request.IsWebSocketRequest)
    {
        var wsContext = await context.AcceptWebSocketAsync(null);
        Console.WriteLine("Client connected");

        var buffer = new byte[1024];

        while (wsContext.WebSocket.State == WebSocketState.Open)
        {
            var result = await wsContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received: {message}");

            var reply = Encoding.UTF8.GetBytes($"Echo: {message}");
            await wsContext.WebSocket.SendAsync(new ArraySegment<byte>(reply), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
    else
    {
        context.Response.StatusCode = 400;
        context.Response.Close();
    }
}
