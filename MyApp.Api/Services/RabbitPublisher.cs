using System;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using MyApp.Api.Hubs;
using MyApp.Shared;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyApp.Api.Services;

public interface IMessagePublisher
{
    void PublishChat(ChatMessage message);
}

public sealed class RabbitPublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _conn;
    private readonly IModel _channel;
    private readonly IHubContext<ChatHub> _hub;

    private const string Exchange = "chat.fanout";
    private const string Queue    = "chat.messages";

    public RabbitPublisher(IConfiguration cfg, IHubContext<ChatHub> hub)
    {
        _hub = hub;

        var factory = new ConnectionFactory
        {
            HostName = cfg.GetValue<string>("Rabbit:Host") ?? "localhost",
            UserName = cfg.GetValue<string>("Rabbit:User") ?? "guest",
            Password = cfg.GetValue<string>("Rabbit:Pass") ?? "guest",
            DispatchConsumersAsync = true
        };
        _conn = factory.CreateConnection();
        _channel = _conn.CreateModel();

        // Fan-out so many consumers can bind if you want
        _channel.ExchangeDeclare(Exchange, type: ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare(Queue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(Queue, Exchange, routingKey: "");
        
        // Setup the consumer
        StartConsuming();
    }

     private void StartConsuming()
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (s, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var msg = JsonSerializer.Deserialize<ChatMessage>(json)!;

                // Broadcast to all connected clients via SignalR
                await _hub.Clients.All.SendAsync("ReceiveChat", msg);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // Log error or handle it appropriately
                Console.WriteLine($"Error processing message: {ex.Message}");
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(Queue, autoAck: false, consumer);
    }

    public void PublishChat(ChatMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;

        _channel.BasicPublish(exchange: Exchange,
                              routingKey: "",
                              basicProperties: props,
                              body: body);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _conn.Dispose();
    }
}