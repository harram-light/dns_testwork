using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Numerics;
using System.Collections.Generic;

public class RPCServer
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "admin"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        Dictionary<string, object> arguments = new();
        arguments.Add("x-message-ttl", 10000);
        channel.QueueDeclare(queue: "factorial_queue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: arguments);

        channel.BasicQos(0, 1, false);
        var consumer = new EventingBasicConsumer(channel);
        channel.BasicConsume(queue: "factorial_queue",
                             autoAck: false,
                             consumer: consumer);

        Console.WriteLine(" [x] Awaiting Factorial requests");

        consumer.Received += (model, ea) =>
        {
            string response = null;

            var body = ea.Body.ToArray();
            var props = ea.BasicProperties;
            var replyProps = channel.CreateBasicProperties();
            replyProps.CorrelationId = props.CorrelationId;

            try
            {
                var input = BitConverter.ToInt64(body);
                Console.WriteLine(" [.] Factorial({0})", input);
                response = GetFactorial(input).ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(" [.] " + e.Message);
                response = "";
            }
            finally
            {
                var responseBytes = Encoding.UTF8.GetBytes(response);
                channel.BasicPublish(exchange: "",
                                     routingKey: props.ReplyTo,
                                     basicProperties: replyProps,
                                     body: responseBytes);

                channel.BasicAck(deliveryTag: ea.DeliveryTag,
                                 multiple: false);
            }
        };

        Console.WriteLine("Press [enter] to exit.");
        Console.ReadLine();
    }

    private static BigInteger CalculateNode(long l, long r)
    {
        if (l > r)
            return 1;

        if (l == r)
            return l;

        if (r - l == 1)
            return (BigInteger)l * r;

        long m = (l + r) / 2;

        return CalculateNode(l, m) * CalculateNode(m + 1, r);
    }

    private static BigInteger GetFactorial(long number)
    {
        if (number < 0)
            return 0;

        if (number == 0)
            return 1;

        if (number == 1 || number == 2)
            return number;

        return CalculateNode(2, number);
    }
}