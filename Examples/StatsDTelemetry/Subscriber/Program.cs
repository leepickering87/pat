﻿using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pat.Subscriber;
using Pat.Subscriber.NetCoreDependencyResolution;
using Pat.Subscriber.Telemetry.StatsD;

namespace Subscriber
{
    internal class Program
    {
        private static async Task Main()
        {
            var serviceProvider = InitialiseIoC();
            
            var tokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, args) =>
            {
                var log = serviceProvider.GetService<ILogger<Program>>();
                log.LogInformation("Subscriber Shutdown Requested");
                args.Cancel = true;
                tokenSource.Cancel();
            };
            
            var subscriber = serviceProvider.GetService<Pat.Subscriber.Subscriber>();
            await subscriber.Initialise(new[] { Assembly.GetExecutingAssembly() });
            await subscriber.ListenForMessages(tokenSource);
        }

        private static ServiceProvider InitialiseIoC()
        {
            var connection = "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOURKEY";
            var topicName = "pat";

            var subscriberConfiguration = new SubscriberConfiguration
            {
                ConnectionStrings = new[] {connection},
                TopicName = topicName,
                SubscriberName = "PatExampleSubscriber",
                UseDevelopmentTopic = false
            };

            var serviceProvider = new ServiceCollection()
                .AddPatLite(subscriberConfiguration)
                .AddLogging(b => b.AddConsole())
                .AddTransient<IStatisticsReporter, StatisticsReporter>()
                .AddSingleton(new StatisticsReporterConfiguration
                {
                    Tenant = "UK",
                    Environment = "Production",
                    StatsDHost = "telemetry.mycompany.com",
                    StatsDPort = 8125
                })
                .AddHandlersFromAssemblyContainingType<FooHandler>()
                .BuildServiceProvider();

            return serviceProvider;
        }
    }
}
