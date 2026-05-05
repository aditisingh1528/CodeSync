using System.Text;
using System.Text.Json;
using ExecutionService.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ExecutionService.Messaging
{
    public class ProjectDeletedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory              _scopeFactory;
        private readonly RabbitMqOptions                   _options;
        private readonly ILogger<ProjectDeletedConsumer>   _logger;
        private IConnection? _connection;
        private IModel?      _channel;

        public ProjectDeletedConsumer(
            IServiceScopeFactory            scopeFactory,
            RabbitMqOptions                 options,
            ILogger<ProjectDeletedConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _options      = options;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Connect();
                    _logger.LogInformation(
                        "ExecutionService Saga consumer connected. Listening on '{Queue}'.",
                        _options.ProjectDeletedQueue);

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

                Disconnect();
                await Task.Delay(_options.RetryDelayMilliseconds, stoppingToken);
            }
        }

        private void Connect()
        {
            var factory = new ConnectionFactory
            {
                HostName             = _options.HostName,
                Port                 = _options.Port,
                UserName             = _options.UserName,
                Password             = _options.Password,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel    = _connection.CreateModel();

            _channel.QueueDeclare(
                queue:      _options.ProjectDeletedQueue,
                durable:    true,
                exclusive:  false,
                autoDelete: false,
                arguments:  null);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, args) => await HandleAsync(args);

            _channel.BasicConsume(
                queue:    _options.ProjectDeletedQueue,
                autoAck:  false,
                consumer: consumer);
        }

        private async Task HandleAsync(BasicDeliverEventArgs args)
        {
            ProjectDeletedEvent? ev = null;

            try
            {
                ev = JsonSerializer.Deserialize<ProjectDeletedEvent>(
                    Encoding.UTF8.GetString(args.Body.ToArray()));

                if (ev is null)
                {
                    _channel?.BasicAck(args.DeliveryTag, multiple: false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var repo  = scope.ServiceProvider.GetRequiredService<IExecutionJobRepository>();
                var count = await repo.DeleteByProjectAsync(ev.ProjectId);

                _logger.LogInformation(
                    "Saga: ExecutionService deleted {Count} job(s) for ProjectId={ProjectId}.",
                    count, ev.ProjectId);

                _channel?.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // Compensation: requeue for retry — eventual consistency.
                _logger.LogError(ex,
                    "Saga compensation needed: ExecutionService failed to delete jobs for ProjectId={ProjectId}. " +
                    "Message will be requeued for retry.",
                    ev?.ProjectId);

                _channel?.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        }

        public override void Dispose()
        {
            Disconnect();
            base.Dispose();
        }

        private void Disconnect()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _channel    = null;
            _connection = null;
        }
    }
}
