using FactorialServer.Services.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FactorialServer.Services.Implementations
{
    public class FactorialCalculator : IFactorialCalculator
    {
        private IMemoryCache _cache;
        private FactorialRMQClient _factorialClient;

        public FactorialCalculator(IMemoryCache cache)
        {
            _cache = cache;
            _factorialClient = new FactorialRMQClient();
        }

        public async Task<string> CalculateAsync(long number)
        {
            try
            {
                if (_cache.TryGetValue(number, out object _cacheResult))
                    return await Task.FromResult((string)_cacheResult);
                else
                {
                    var result = await _factorialClient.CallAsync(number);

                    if (!string.IsNullOrEmpty(result))
                        _cache.Set(number, result);

                    return result;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                _factorialClient.Close();
            }
        }
    }

    public class FactorialRMQClient
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string replyQueueName;
        private readonly EventingBasicConsumer consumer;
        private readonly IBasicProperties props;
        private readonly TaskCompletionSource<string> tcs = new();

        public FactorialRMQClient()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "admin"
            };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            Dictionary<string, object> args = new();
            args.Add("x-message-ttl", 10000);
            channel.QueueDeclare(queue: "factorial_queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: args);

            replyQueueName = channel.QueueDeclare().QueueName;
            consumer = new EventingBasicConsumer(channel);
            props = channel.CreateBasicProperties();
            props.CorrelationId = Guid.NewGuid().ToString();
            props.ReplyTo = replyQueueName;

            consumer.Received += (model, ea) =>
            {
                var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                if (ea.BasicProperties.CorrelationId == props.CorrelationId)
                    tcs.SetResult(response);
            };

            channel.BasicConsume(consumer: consumer, queue: replyQueueName, autoAck: true);
        }

        public void Close()
        {
            _ = Task.Run(() =>
            {
                channel.Close();
                connection.Close();
            });
        }

        public async Task<string> CallAsync(long number)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(9000);
            cts.Token.Register(() =>
            {
                tcs.SetResult(null);
            });

            channel.BasicPublish(exchange: "",
                                 routingKey: "factorial_queue",
                                 basicProperties: props,
                                 body: BitConverter.GetBytes(number));

            return await tcs.Task;
        }
    }
}