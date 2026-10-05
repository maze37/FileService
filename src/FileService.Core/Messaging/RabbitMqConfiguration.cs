using IntegrationEvents.Files;
using IntegrationEvents.Files.Events;
using Wolverine;
using Wolverine.RabbitMQ;

namespace FileService.Core.Messaging;

public static class RabbitMqConfiguration
{    
    public static void ConfigureRabbitMq(this WolverineOptions opts, string connectionString)
    {
        opts.UseRabbitMq(new Uri(connectionString))
            .AutoProvision()
            .EnableWolverineControlQueues()
            .UseQuorumQueues()
            .DeclareExchange(FileEventsRouting.EXCHANGE, exchange =>
            {
                exchange.ExchangeType = ExchangeType.Topic;
                exchange.IsDurable = true;
            });

        opts.ConfigureFileEventsPublishing();
    }

    private static void ConfigureFileEventsPublishing(this WolverineOptions opts)
    {
        opts.PublishMessagesToRabbitMqExchange<AssetReady>(
            FileEventsRouting.EXCHANGE,
            m => FileEventsRouting.RoutingKeys.AssetReady(m.EntityType)).UseDurableOutbox();

        opts.PublishMessagesToRabbitMqExchange<AssetDeleted>(
            FileEventsRouting.EXCHANGE,
            m => FileEventsRouting.RoutingKeys.AssetDeleted(m.EntityType)).UseDurableOutbox();
    }
}