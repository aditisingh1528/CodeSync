using System.Text;
using System.Text.Json;
using NotificationService.Models;
using NotificationService.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Messaging
{
    public class NotificationConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory        _scopeFactory;
        private readonly IRabbitMqConnectionFactory  _connectionFactory;
        private readonly RabbitMqOptions             _options;
        private readonly ILogger<NotificationConsumer> _logger;
        private IConnection? _connection;
        private IModel?      _channel;

        public NotificationConsumer(
            IServiceScopeFactory       scopeFactory,
            IRabbitMqConnectionFactory connectionFactory,
            RabbitMqOptions            options,
            ILogger<NotificationConsumer> logger)
        {
            _scopeFactory      = scopeFactory;
            _connectionFactory = connectionFactory;
            _options           = options;
            _logger            = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Connect(stoppingToken);
                    _logger.LogInformation("NotificationConsumer connected. Listening on '{Queue}'.", _options.ExecutionEventsQueue);

                    while (!stoppingToken.IsCancellationRequested &&
                           _connection?.IsOpen == true &&
                           _channel?.IsOpen    == true)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "RabbitMQ unavailable at {Host}:{Port}. Retrying in {Delay}ms. Error: {Error}",
                        _options.HostName, _options.Port, _options.RetryDelayMilliseconds, ex.Message);
                }

                CloseConnections();
                await Task.Delay(_options.RetryDelayMilliseconds, stoppingToken);
            }
        }

        private void Connect(CancellationToken stoppingToken)
        {
            _connection = _connectionFactory.CreateConnection();
            _channel    = _connection.CreateModel();

            // Declare the queue so it exists whether ExecutionService or us starts first.
            _channel.QueueDeclare(
                queue:      _options.ExecutionEventsQueue,
                durable:    true,
                exclusive:  false,
                autoDelete: false,
                arguments:  null);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, args) => await HandleMessageAsync(args);

            _channel.BasicConsume(
                queue:     _options.ExecutionEventsQueue,
                autoAck:   false,
                consumer:  consumer);
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs args)
        {
            try
            {
                var json  = Encoding.UTF8.GetString(args.Body.ToArray());
                var ev    = JsonSerializer.Deserialize<ExecutionCompletedEvent>(json);

                if (ev is null)
                {
                    _channel?.BasicAck(args.DeliveryTag, multiple: false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                var notification = new Notification
                {
                    UserId    = ev.UserId,
                    Message   = ev.Message,
                    Type      = ev.Success ? NotificationType.JobCompleted : NotificationType.JobFailed,
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                };

                await repo.CreateAsync(notification);

                _logger.LogInformation(
                    "Notification saved for User {UserId}: [{Type}] {Message}",
                    ev.UserId, notification.Type, notification.Message);

                _channel?.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process notification event.");
                _channel?.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        }

        public override void Dispose()
        {
            CloseConnections();
            base.Dispose();
        }

        private void CloseConnections()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _channel    = null;
            _connection = null;
        }
    }
}
