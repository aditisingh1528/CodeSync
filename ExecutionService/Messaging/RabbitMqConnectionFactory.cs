using RabbitMQ.Client;

namespace ExecutionService.Messaging
{
    public interface IRabbitMqConnectionFactory
    {
        IConnection CreateConnection();
    }

    public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
    {
        private readonly RabbitMqOptions _options;

        public RabbitMqConnectionFactory(RabbitMqOptions options) => _options = options;

        public IConnection CreateConnection()
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                DispatchConsumersAsync = true
            };

            return factory.CreateConnection();
        }
    }
}
