using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using RabbitMQ.Client;

namespace ZavaBank.ZavaNotifyWorker
{
    internal sealed class NotificationMessage
    {
        public string AlertType;
        public string TransactionId;
        public string Amount;
        public string Currency;
        public string CustomerId;
        public string CustomerName;
        public string CustomerEmail;
        public string Details;
    }

    internal class Program
    {
        private static volatile bool _keepRunning = true;
        private static string _rabbitHost;
        private static int _rabbitPort;
        private static string _rabbitVHost;
        private static string _rabbitUser;
        private static string _rabbitPassword;
        private static string _notificationQueue;
        private static string _deadLetterExchange;
        private static int _pollIntervalMs;
        private static string _smtpHost;
        private static int _smtpPort;
        private static string _smtpFrom;

        private static void Main(string[] args)
        {
            LoadConfig();

            Console.CancelKeyPress += OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += delegate { _keepRunning = false; };

            Console.WriteLine("[NotifyWorker] Starting notification worker.");
            Console.WriteLine("[NotifyWorker] Queue={0}, RabbitMQ={1}:{2}, SMTP={3}:{4}", _notificationQueue, _rabbitHost, _rabbitPort, _smtpHost, _smtpPort);

            while (_keepRunning)
            {
                try
                {
                    PollQueue();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[NotifyWorker] Poll cycle failed: " + ex.Message);
                }

                if (_keepRunning)
                {
                    Thread.Sleep(_pollIntervalMs);
                }
            }

            Console.WriteLine("[NotifyWorker] Graceful shutdown complete.");
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("[NotifyWorker] Ctrl+C received. Stopping...");
            _keepRunning = false;
            e.Cancel = true;
        }

        private static void LoadConfig()
        {
            _rabbitHost = Env("RABBITMQ_HOST", "RabbitMQ.Host", "rabbitmq");
            _rabbitPort = EnvInt("RABBITMQ_PORT", "RabbitMQ.Port", 5672);
            _rabbitVHost = Env("RABBITMQ_VHOST", "RabbitMQ.VHost", "/zavabank");
            _rabbitUser = Env("RABBITMQ_USER", "RabbitMQ.User", "zava_app");
            _rabbitPassword = Env("RABBITMQ_PASSWORD", "RabbitMQ.Password", "zava_pass");
            _notificationQueue = Env("NOTIFICATIONS_QUEUE", "RabbitMQ.NotificationQueue", "q.notify.email");
            _deadLetterExchange = Env("RABBITMQ_DLX", "RabbitMQ.DeadLetterExchange", "zava.dlx");
            _pollIntervalMs = EnvInt("WORKER_POLL_INTERVAL_MS", "Worker.PollIntervalMs", 5000);
            _smtpHost = Env("SMTP_HOST", "Smtp.Host", "mailhog");
            _smtpPort = EnvInt("SMTP_PORT", "Smtp.Port", 1025);
            _smtpFrom = Env("SMTP_FROM", "Smtp.From", "no-reply@zavabank.local");
        }

        private static string Env(string envKey, string appSettingKey, string defaultValue)
        {
            var env = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env;
            }

            var appSetting = ConfigurationManager.AppSettings[appSettingKey];
            if (!string.IsNullOrWhiteSpace(appSetting))
            {
                return appSetting;
            }

            return defaultValue;
        }

        private static int EnvInt(string envKey, string appSettingKey, int defaultValue)
        {
            var value = Env(envKey, appSettingKey, defaultValue.ToString());
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : defaultValue;
        }

        private static ConnectionFactory CreateFactory()
        {
            return new ConnectionFactory
            {
                HostName = _rabbitHost,
                Port = _rabbitPort,
                VirtualHost = _rabbitVHost,
                UserName = _rabbitUser,
                Password = _rabbitPassword,
                AutomaticRecoveryEnabled = false
            };
        }

        private static void PollQueue()
        {
            using (var connection = CreateFactory().CreateConnection())
            using (var channel = connection.CreateModel())
            {
                EnsureQueue(channel, _notificationQueue, _deadLetterExchange);

                var result = channel.BasicGet(_notificationQueue, false);
                if (result == null)
                {
                    Console.WriteLine("[NotifyWorker] No messages available.");
                    return;
                }

                var body = Encoding.UTF8.GetString(result.Body);
                Console.WriteLine("[NotifyWorker] Received message deliveryTag={0}", result.DeliveryTag);

                try
                {
                    var message = ParseNotificationMessage(body);
                    SendEmail(message);
                    Console.WriteLine(
                        "[NotifyWorker] Notification sent to {0} for customer {1} ({2}), alertType={3}, transaction={4}, amount={5} {6}",
                        message.CustomerEmail,
                        message.CustomerName,
                        message.CustomerId,
                        message.AlertType,
                        message.TransactionId,
                        message.Amount,
                        message.Currency);

                    channel.BasicAck(result.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[NotifyWorker] Failed to process message: " + ex.Message);
                    channel.BasicNack(result.DeliveryTag, false, false);
                }
            }
        }

        private static void EnsureQueue(IModel channel, string queueName, string deadLetterExchange)
        {
            try
            {
                channel.QueueDeclarePassive(queueName);
            }
            catch
            {
                var args = new Dictionary<string, object> { { "x-dead-letter-exchange", deadLetterExchange } };
                channel.QueueDeclare(queueName, true, false, false, args);
                Console.WriteLine("[NotifyWorker] Declared queue {0}", queueName);
            }
        }

        private static NotificationMessage ParseNotificationMessage(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidOperationException("Message body is empty.");
            }

            if (raw.TrimStart().StartsWith("<"))
            {
                return ParseXml(raw);
            }

            return ParseJson(raw);
        }

        private static NotificationMessage ParseJson(string raw)
        {
            var serializer = new JavaScriptSerializer();
            var data = serializer.Deserialize<Dictionary<string, object>>(raw);
            return new NotificationMessage
            {
                AlertType = ReadValue(data, "alertType", "AlertType", "type"),
                TransactionId = ReadValue(data, "transactionId", "TransactionId"),
                Amount = ReadValue(data, "amount", "Amount"),
                Currency = ReadValue(data, "currency", "Currency"),
                CustomerId = ReadValue(data, "customerId", "CustomerId", "userId"),
                CustomerName = ReadValue(data, "customerName", "CustomerName", "name"),
                CustomerEmail = ReadValue(data, "customerEmail", "CustomerEmail", "email"),
                Details = ReadValue(data, "details", "Details", "description")
            };
        }

        private static NotificationMessage ParseXml(string raw)
        {
            var xml = XDocument.Parse(raw);
            Func<string, string> value = name =>
            {
                var element = xml.Root != null ? xml.Root.Element(name) : null;
                return element != null ? element.Value : string.Empty;
            };

            return new NotificationMessage
            {
                AlertType = FirstNotEmpty(value("AlertType"), value("Type")),
                TransactionId = FirstNotEmpty(value("TransactionId"), value("TransactionID")),
                Amount = value("Amount"),
                Currency = value("Currency"),
                CustomerId = FirstNotEmpty(value("CustomerId"), value("UserId")),
                CustomerName = FirstNotEmpty(value("CustomerName"), value("Name")),
                CustomerEmail = FirstNotEmpty(value("CustomerEmail"), value("Email")),
                Details = FirstNotEmpty(value("Details"), value("Description"))
            };
        }

        private static string ReadValue(IDictionary<string, object> data, params string[] keys)
        {
            foreach (var key in keys)
            {
                object value;
                if (data.TryGetValue(key, out value) && value != null)
                {
                    return value.ToString();
                }
            }

            return string.Empty;
        }

        private static string FirstNotEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static void SendEmail(NotificationMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.CustomerEmail))
            {
                throw new InvalidOperationException("Customer email missing from notification message.");
            }

            var subject = string.Format(
                "Zava Bank {0}: Transaction {1}",
                string.IsNullOrWhiteSpace(message.AlertType) ? "Notification" : message.AlertType,
                string.IsNullOrWhiteSpace(message.TransactionId) ? "N/A" : message.TransactionId);

            var body = string.Format(
                "Zava Bank Alert\r\n" +
                "----------------\r\n" +
                "Alert Type: {0}\r\n" +
                "Customer Id: {1}\r\n" +
                "Customer Name: {2}\r\n" +
                "Transaction Id: {3}\r\n" +
                "Amount: {4} {5}\r\n" +
                "Details: {6}\r\n" +
                "Processed At: {7:yyyy-MM-dd HH:mm:ss}\r\n",
                ValueOrDefault(message.AlertType),
                ValueOrDefault(message.CustomerId),
                ValueOrDefault(message.CustomerName),
                ValueOrDefault(message.TransactionId),
                ValueOrDefault(message.Amount),
                ValueOrDefault(message.Currency),
                ValueOrDefault(message.Details),
                DateTime.Now);

            using (var smtp = new SmtpClient(_smtpHost, _smtpPort))
            using (var email = new MailMessage(_smtpFrom, message.CustomerEmail, subject, body))
            {
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = true;
                smtp.Send(email);
            }
        }

        private static string ValueOrDefault(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "N/A" : value;
        }
    }
}
