using Common.EasyNetQ.Lib;
using Common.Logging;
using NotificationService.Lib;
using NotificationService.Lib.Connection;
using NotificationService.Lib.Helper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Net;
using NLog;
using LogManager = NLog.LogManager;
using System;

namespace NotificationService.WebHost
{
    public class Startup
    {
        private IConfiguration _configuration { get;}
        readonly ILogger _logger = LogManager.GetLogger("Log");
        readonly string _corsOriginsName = "NotifyCorsOrigins";
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var WithRedisBackplane = _configuration.GetValue<bool>("SignalRBackPlaneSetting:WithRedisBackplane");

            services.AddSingleton<IRabbitHelper, RabbitHelper>();

            var corsOrigin = _configuration.GetSection("CorsPolicy:Origins").Get<Dictionary<string,string>>();

            services.AddCors(options => options.AddPolicy(_corsOriginsName,
            builder =>
            {
                builder.SetIsOriginAllowed(origin => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                foreach (var origin in corsOrigin)
                {
                    builder.WithOrigins(origin.Value).AllowAnyHeader().AllowAnyMethod();
                }
            }));

            services.AddHostedService<NotificationHostedService>();
            services.AddSingleton<IUserIdProvider, UserIdFactory>();

            if (WithRedisBackplane)
            {
                var redisConnStr = _configuration.GetValue<string>("Redis:ConnectionString");
                services.AddSignalR()
                    .AddMessagePackProtocol()
                    .AddStackExchangeRedis(o =>
                    {

                        var redisChannelPrefix = _configuration.GetValue<string>("SignalRBackPlaneSetting:RedisChannelPrefix");
                        //是否需要前綴詞，以區隔Hub相關的Redis儲存
                        if (!string.IsNullOrEmpty(redisChannelPrefix))
                        {
                            o.Configuration.ChannelPrefix = redisChannelPrefix;
                        }
                        o.ConnectionFactory = async writer =>
                        {
                            var config = ConfigurationOptions.Parse(redisConnStr);
                            config.EndPoints.Add(IPAddress.Loopback, 0);
                            config.SetDefaultPorts();
                            var connection = await ConnectionMultiplexer.ConnectAsync(config, writer);
                            connection.ConnectionFailed += (_, e) =>
                            {
                                _logger.Error("Connection to Redis failed.");
                                Console.WriteLine("Connection to Redis failed.");
                            };

                            if (!connection.IsConnected)
                            {
                                _logger.Error("Did not connect to Redis.");
                                Console.WriteLine("Did not connect to Redis.");
                            }

                            return connection;
                        };
                    });
                //以Redis存取連線中的使用者
                services.AddSingleton<IConnectionManager, RedisConnectionManager>();
                services.AddSingleton<IKeyValuePairHelper>(_ => new RedisHelper(new RedisHelper.RedisConfig()
                {
                    ConnectionString = _configuration.GetValue<string>("Redis:ConnectionString"),
                    DatabaseSelect = _configuration.GetValue<int>("Redis:DatabaseSelect"),
                    DatabaseSelectByLock = _configuration.GetValue<int>("Redis:DatabaseSelectByLock"),
                    DataEffectiveTime = _configuration.GetValue<int?>("Redis:DataEffectiveTime"),
                    LockExpirySeconds = _configuration.GetValue<double?>("Redis:LockExpirySeconds"),
                    LockWaitSeconds = _configuration.GetValue<double?>("Redis:LockWaitSeconds"),
                    LockRetrySeconds = _configuration.GetValue<double?>("Redis:LockRetrySeconds")
                }));
            }
            else
            {
                services.AddSignalR();
                //以本機記憶體存取連線中的使用者
                services.AddSingleton<IConnectionManager, SimpleConnectionManager>();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //app.UseAuthentication();
            app.UseCors(_corsOriginsName);
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<NotificaitionHub>("/wsnHub", options =>
                {
                    options.Transports = HttpTransportType.WebSockets;
                });
            });
        }
    }
}
