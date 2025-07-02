using System;
using Microsoft.AspNetCore.SignalR;
using MyApp.Shared;

namespace MyApp.Api.Hubs;

public class ChatHub : Hub
{
    // Optional: Override connection events for logging
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
    }

    // Optional: Add methods for direct client-server communication
    public async Task SendMessageToServer(ChatMessage message)
    {
        // Broadcast to all clients (or process the message further)
        await Clients.All.SendAsync("ReceiveChat", message);
    }
}
