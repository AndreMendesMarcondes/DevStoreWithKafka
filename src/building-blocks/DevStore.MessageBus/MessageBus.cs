using Confluent.Kafka;
using DevStore.Core.Messages.Integration;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevStore.MessageBus
{
    public class MessageBus : IMessageBus
    {
        private readonly string _bootstrapserver;
        public MessageBus(string bootstrapserver)
        {
            _bootstrapserver = bootstrapserver;
        }

        public async Task ConsumerAsync<T>(
                          string topic,
                          Func<T, Task> onMessage,
                          CancellationToken cancellation) where T : IntegrationEvent
        {
            _ = Task.Factory.StartNew(async () =>
            {
                var config = new ConsumerConfig
                {
                    GroupId = "grupo-curso",
                    BootstrapServers = _bootstrapserver,
                    EnableAutoCommit = false,
                    EnablePartitionEof = true,
                };

                using var consumer = new ConsumerBuilder<string, string>(config).Build();

                consumer.Subscribe(topic);

                while (!cancellation.IsCancellationRequested)
                {
                    var result = consumer.Consume();

                    if (result.IsPartitionEOF)
                    {
                        continue;
                    }

                    var message = JsonConvert.DeserializeObject<T>(result.Message.Value);

                    await onMessage(message);

                    consumer.Commit();
                }
            }, cancellation, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public async Task ProducerAsync<T>(string topic, T message) where T : IntegrationEvent
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _bootstrapserver,
            };

            var payload = JsonConvert.SerializeObject(message);

            var producer = new ProducerBuilder<string, string>(config).Build();

            var result = await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = payload
            });

            await Task.CompletedTask;
        }
    }
}