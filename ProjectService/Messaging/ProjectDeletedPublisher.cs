using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace ProjectService.Messaging
{
    public interface IProjectDeletedPublisher
    {
        Task PublishAsync(ProjectDeletedEvent ev);
    }

    public class ProjectDeletedPublisher : IProjectDeletedPublisher
    {
        private readonly RabbitMqOptions _options;
        private readonly ILogger<ProjectDeletedPublisher> _logger;

        public ProjectDeletedPublisher(RabbitMqOptions options, ILogger<ProjectDeletedPublisher> logger)
        {
            _options = options;
            _logger  = logger;
        }

        public async Task PublishAsync(ProjectDeletedEvent ev)
        {
            var body      = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ev));
            var lastError = default(Exception);

            for (var attempt = 1; attempt <= _options.MaxRetries; attempt++)
            {
                try
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = _options.HostName,
                        Port     = _options.Port,
                        UserName = _options.UserName,
                        Password = _options.Password
                    };

                    using var connection = factory.CreateConnection();
                    using var channel    = connection.CreateModel();

                    channel.QueueDeclare(
                        queue:      _options.ProjectDeletedQueue,
                        durable:    true,
                        exclusive:  false,
                        autoDelete: false,
                        arguments:  null);

                    var props       = channel.CreateBasicProperties();
                    props.Persistent = true;

                    channel.BasicPublish(
                        exchange:        string.Empty,
                        routingKey:      _options.ProjectDeletedQueue,
                        basicProperties: props,
                        body:            body);

                    _logger.LogInformation(
                        "ProjectDeleted event published for ProjectId={ProjectId}.", ev.ProjectId);
                    return;
                }
                catch (Exception ex) when (attempt < _options.MaxRetries)
                {
                    lastError = ex;
                    _logger.LogWarning(
                        "Publish attempt {Attempt}/{Max} failed for ProjectId={ProjectId}: {Error}",
                        attempt, _options.MaxRetries, ev.ProjectId, ex.Message);
                    await Task.Delay(_options.RetryDelayMilliseconds);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            _logger.LogError(
                "Could not publish ProjectDeleted event for ProjectId={ProjectId} after {Max} attempts: {Error}",
                ev.ProjectId, _options.MaxRetries, lastError?.Message);
        }
    }
}
