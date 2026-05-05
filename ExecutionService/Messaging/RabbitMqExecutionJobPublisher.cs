using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace ExecutionService.Messaging
{
    public class RabbitMqExecutionJobPublisher : IExecutionJobPublisher
    {
        private readonly IRabbitMqConnectionFactory _connectionFactory;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<RabbitMqExecutionJobPublisher> _logger;

        public RabbitMqExecutionJobPublisher(
            IRabbitMqConnectionFactory connectionFactory,
            RabbitMqOptions options,
            ILogger<RabbitMqExecutionJobPublisher> logger)
        {
            _connectionFactory = connectionFactory;
            _options = options;
            _logger = logger;
        }

        public async Task PublishAsync(int jobId, int attempt = 1, CancellationToken cancellationToken = default)
        {
            var message = JsonSerializer.Serialize(new ExecutionJobMessage { JobId = jobId, Attempt = attempt });
            var body = Encoding.UTF8.GetBytes(message);
            var lastError = default(Exception);

            for (var tryNumber = 1; tryNumber <= _options.MaxRetries; tryNumber++)
            {
                try
                {
                    using var connection = _connectionFactory.CreateConnection();
                    using var channel = connection.CreateModel();
                    DeclareQueue(channel);

                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;

                    channel.BasicPublish(
                        exchange: string.Empty,
                        routingKey: _options.QueueName,
                        basicProperties: properties,
                        body: body);

                    return;
                }
                catch (Exception ex) when (tryNumber < _options.MaxRetries)
                {
                    lastError = ex;
                    _logger.LogWarning(
                        "RabbitMQ publish failed for job {JobId}. Retry {Retry}/{MaxRetries}. Error: {Error}",
                        jobId,
                        tryNumber,
                        _options.MaxRetries,
                        ex.Message);
                    await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            throw new InvalidOperationException($"Could not publish execution job {jobId} to RabbitMQ.", lastError);
        }

        private void DeclareQueue(IModel channel)
        {
            channel.QueueDeclare(
                queue: _options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }
    }
}
