using System.Text;
using System.Text.Json;
using ExecutionService.Models;
using ExecutionService.Repositories;
using ExecutionService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ExecutionService.Messaging
{
    public class ExecutionJobConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IRabbitMqConnectionFactory _connectionFactory;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<ExecutionJobConsumer> _logger;
        private IConnection? _connection;
        private IModel? _channel;

        public ExecutionJobConsumer(
            IServiceScopeFactory scopeFactory,
            IRabbitMqConnectionFactory connectionFactory,
            RabbitMqOptions options,
            ILogger<ExecutionJobConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _connectionFactory = connectionFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    StartConsumer(stoppingToken);
                    _logger.LogInformation("RabbitMQ execution consumer connected to queue '{QueueName}'.", _options.QueueName);

                    while (!stoppingToken.IsCancellationRequested &&
                           _connection?.IsOpen == true &&
                           _channel?.IsOpen == true)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "RabbitMQ is unavailable at {Host}:{Port}. Retrying in {Delay}ms. Error: {Error}",
                        _options.HostName, _options.Port, _options.RetryDelayMilliseconds, ex.Message);
                }

                CloseRabbitMqObjects();
                await Task.Delay(_options.RetryDelayMilliseconds, stoppingToken);
            }
        }

        private void StartConsumer(CancellationToken stoppingToken)
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            DeclareQueue(_channel, _options.QueueName);

            // Also declare the events queue so NotificationService can consume from it.
            DeclareQueue(_channel, _options.EventsQueueName);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, eventArgs) =>
            {
                await HandleMessageAsync(eventArgs, stoppingToken);
            };

            _channel.BasicConsume(
                queue: _options.QueueName,
                autoAck: false,
                consumer: consumer);
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
        {
            var message = JsonSerializer.Deserialize<ExecutionJobMessage>(
                Encoding.UTF8.GetString(eventArgs.Body.ToArray()));

            if (message is null)
            {
                _channel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo   = scope.ServiceProvider.GetRequiredService<IExecutionJobRepository>();
                var runner = scope.ServiceProvider.GetRequiredService<IExecutionRunner>();

                var job = await repo.GetByIdAsync(message.JobId);
                if (job is null)
                {
                    _channel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                    return;
                }

                job.Status = ExecutionJobStatus.Running;
                job.Output = null;
                job.ErrorOutput = null;
                await repo.UpdateAsync(job);

                var result = await runner.ExecuteAsync(job, cancellationToken);
                job.Status     = result.Success ? ExecutionJobStatus.Completed : ExecutionJobStatus.Failed;
                job.Output     = result.Output;
                job.ErrorOutput = result.ErrorOutput;
                job.ExecutedAt = DateTime.UtcNow;
                await repo.UpdateAsync(job);

                // Tell NotificationService the job finished.
                PublishExecutionEvent(job.Id, job.UserId, result.Success);

                _channel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Execution job {JobId} failed while processing attempt {Attempt}.", message.JobId, message.Attempt);

                if (message.Attempt < _options.MaxRetries)
                {
                    await RepublishAsync(message.JobId, message.Attempt + 1, cancellationToken);
                }
                else
                {
                    await MarkFailedAsync(message.JobId, ex.Message);
                }

                _channel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
        }

        private void PublishExecutionEvent(int jobId, int userId, bool success)
        {
            try
            {
                var ev = new
                {
                    JobId   = jobId,
                    UserId  = userId,
                    Success = success,
                    Message = success
                        ? $"Your execution job #{jobId} completed successfully."
                        : $"Your execution job #{jobId} failed."
                };

                var body  = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ev));
                var props = _channel!.CreateBasicProperties();
                props.Persistent = true;

                _channel.BasicPublish(
                    exchange:        string.Empty,
                    routingKey:      _options.EventsQueueName,
                    basicProperties: props,
                    body:            body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not publish execution event for job {JobId}: {Error}", jobId, ex.Message);
            }
        }

        private async Task RepublishAsync(int jobId, int attempt, CancellationToken cancellationToken)
        {
            await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new ExecutionJobMessage
            {
                JobId   = jobId,
                Attempt = attempt
            }));

            var properties = _channel!.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange:        string.Empty,
                routingKey:      _options.QueueName,
                basicProperties: properties,
                body:            body);
        }

        private async Task MarkFailedAsync(int jobId, string error)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IExecutionJobRepository>();
            var job  = await repo.GetByIdAsync(jobId);
            if (job is null) return;

            job.Status     = ExecutionJobStatus.Failed;
            job.ErrorOutput = $"Execution failed after {_options.MaxRetries} attempts: {error}";
            job.ExecutedAt = DateTime.UtcNow;
            await repo.UpdateAsync(job);

            PublishExecutionEvent(jobId, job.UserId, success: false);
        }

        private void DeclareQueue(IModel channel, string queueName)
        {
            channel.QueueDeclare(
                queue:      queueName,
                durable:    true,
                exclusive:  false,
                autoDelete: false,
                arguments:  null);
        }

        public override void Dispose()
        {
            CloseRabbitMqObjects();
            base.Dispose();
        }

        private void CloseRabbitMqObjects()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _channel    = null;
            _connection = null;
        }
    }
}
