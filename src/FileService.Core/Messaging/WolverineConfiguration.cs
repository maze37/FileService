using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

namespace FileService.Core.Messaging;

public static class WolverineConfiguration
{
    public static void AddWolverine(this WebApplicationBuilder builder)
    {
        string rabbitConnectionString = builder.Configuration.GetConnectionString(ConnectionStringNames.RABBIT_MQ)!;
        string postgresConnectionString = builder.Configuration.GetConnectionString(ConnectionStringNames.DATABASE)!;

        builder.Host.UseWolverine(opts =>
        {
            opts.UseRuntimeCompilation();

            opts.ApplicationAssembly = typeof(WolverineConfiguration).Assembly;
            opts.ConfigureDurableMessaging(postgresConnectionString);
            opts.ConfigureRabbitMq(rabbitConnectionString);
        }, ExtensionDiscovery.ManualOnly);
    }

    private static void ConfigureDurableMessaging(this WolverineOptions opts, string postgresConnectionString)
    {
        opts.PersistMessagesWithPostgresql(postgresConnectionString, "files");
        opts.UseEntityFrameworkCoreTransactions();
        opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
        opts.Policies.UseDurableInboxOnAllListeners();
    }
}