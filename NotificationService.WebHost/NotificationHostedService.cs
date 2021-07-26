using Common.EasyNetQ.Lib;
using Common.EasyNetQ.Message;
using EasyNetQ;
using NotificationService.Lib.Connection;
using NotificationService.Lib.Helper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NLog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationService.Lib
{
    public class NotificationHostedService : IHostedService
    {
        private readonly IConnectionManager _connectionManager;
        public readonly IBus Bus;
        readonly ILogger _logger = LogManager.GetLogger("Common");
        private readonly IHubContext<NotificaitionHub, IClientNotification> _hubContext;
        public readonly IRabbitHelper RabbitHelper;
        private readonly IConfiguration _configuration;
        private static ushort _prefetchCount { get; set; }
        private static ushort _requeueSeconds { get; set; }
        private static int _notificationMaxRetryCount { get; set; }
        public NotificationHostedService(IHubContext<NotificaitionHub, IClientNotification> hubContext,IConnectionManager connectionManager, IRabbitHelper rabbitHelper, IConfiguration  configuration)
        {
            _configuration = configuration;
            _connectionManager = connectionManager;
            _hubContext = hubContext;
            RabbitHelper = rabbitHelper;
            var rabbitConnStr = _configuration.GetConnectionString("RabbitMQ");

            Bus = RabbitBusBuilder.AutoCreateBus(rabbitConnStr);

            _prefetchCount = _configuration.GetValue<ushort>("RabbitMQ:PrefetchCount");
            _requeueSeconds = _configuration.GetValue<ushort>("RabbitMQ:RequeueSeconds");
            _notificationMaxRetryCount = _configuration.GetValue<int>("NotificationMaxRetryCount");
        }
        public Task Notification(NotifyMessage messageObject, CancellationToken cancellationToken)
        {
            var message = JsonConvert.SerializeObject(messageObject);
            if (_connectionManager.GetConnections(messageObject.UserId).Count() > 0)
            {
                return _hubContext.Clients.User(messageObject.UserId).Notify(message);
            }
            else
            {
                Thread.Sleep(_requeueSeconds * 1000);
                throw new Exception($"{messageObject.UserId} is not online, drop message...");
            }
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            RabbitHelper.Declare<NotifyMessage>(Bus,c=>c.WithDeadLetterMaxRetryCount(_notificationMaxRetryCount));
            SubScribe();
            _logger.Info("Notification Service Start...");
            return Task.CompletedTask;
        }

        public Task SubScribe()
        {
           return RabbitHelper.SubscribeAsync<NotifyMessage>(Bus, "", (message, cts) =>
           Task.Factory.StartNew(() =>
                  Notification(message, cts)
           )
           , (config) => config.WithPrefetchCount(_prefetchCount));
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Bus.Dispose();
            _logger.Info("Notification Service Stop...");
            return Task.CompletedTask;
        }
    }
}
