using System;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using MyApp.Api.Hubs;
using MyApp.Shared;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyApp.Api.Services;

public class ChatConsumer : BackgroundService
{
    private readonly IModel _channel;
    private readonly IConnection _conn;
    private readonly IHubContext<ChatHub> _hub;

    private const string Exchange = "chat.fanout";
    private const string Queue    = "chat.messages";

    public ChatConsumer(IConfiguration cfg, IHubContext<ChatHub> hub)
    {
        _hub = hub;

        var factory = new ConnectionFactory
        {
            HostName = cfg["Rabbit:Host"] ?? "localhost",
            UserName = cfg["Rabbit:User"] ?? "guest",
            Password = cfg["Rabbit:Pass"] ?? "guest",
            DispatchConsumersAsync = true
        };
        _conn    = factory.CreateConnection();
        _channel = _conn.CreateModel();

        _channel.ExchangeDeclare(Exchange, ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare (Queue,  durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind    (Queue,  Exchange, routingKey: "");
    }

    protected override Task ExecuteAsync(CancellationToken stop)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        // consumer.Received += async (s, ea) =>
        // {
        //     var json = Encoding.UTF8.GetString(ea.Body.Span);
        //     var msg  = JsonSerializer.Deserialize<ChatMessage>(json)!;

        //     // broadcast to *all* connected clients
        //     await _hub.Clients.All.SendAsync("ReceiveChat", msg);

        //     _channel.BasicAck(ea.DeliveryTag, multiple: false);
        // };
        consumer.Received += async (s, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            var msg = JsonSerializer.Deserialize<ChatMessage>(json)!;

            // Broadcast to all connected clients via SignalR
            await _hub.Clients.All.SendAsync("ReceiveChat", msg);
            await _hub.Clients.All.SendAsync("ReceiveChatString", msg.User, msg.Text);

            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(Queue, autoAck: false, consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _conn?.Dispose();
        base.Dispose();
    }
}
