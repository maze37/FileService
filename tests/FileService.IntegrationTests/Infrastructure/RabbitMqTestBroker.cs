using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FileService.IntegrationTests.Infrastructure;

public sealed class RabbitMqTestBroker : IAsyncDisposable
{
    private const string Queue = "tests.asset-events";
    private readonly IContainer _container = new ContainerBuilder("rabbitmq:4-management-alpine")
        .WithEnvironment("RABBITMQ_DEFAULT_USER", "tests")
        .WithEnvironment("RABBITMQ_DEFAULT_PASS", "tests")
        .WithPortBinding(5672, true)
        .WithPortBinding(15672, true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilInternalTcpPortIsAvailable(5672)
            .UntilInternalTcpPortIsAvailable(15672))
        .Build();
    private HttpClient _management = null!;

    public string ConnectionString =>
        $"amqp://tests:tests@{_container.Hostname}:{_container.GetMappedPublicPort(5672)}";

    public async Task StartAsync()
    {
        await _container.StartAsync();
        _management = new HttpClient
        {
            BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(15672)}/api/")
        };
        _management.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("tests:tests")));

        using var exchange = await _management.PutAsJsonAsync("exchanges/%2F/file-events",
            new { type = "topic", durable = true, auto_delete = false, arguments = new { } });
        exchange.EnsureSuccessStatusCode();
        using var queue = await _management.PutAsJsonAsync($"queues/%2F/{Queue}",
            new { durable = true, auto_delete = false, arguments = new Dictionary<string, object> { ["x-queue-type"] = "quorum" } });
        queue.EnsureSuccessStatusCode();
        using var binding = await _management.PostAsJsonAsync($"bindings/%2F/e/file-events/q/{Queue}",
            new { routing_key = "asset.*.*", arguments = new { } });
        binding.EnsureSuccessStatusCode();
    }

    public async Task<JsonElement[]> ReadAsync()
    {
        using var response = await _management.PostAsJsonAsync($"queues/%2F/{Queue}/get",
            new { count = 100, ackmode = "ack_requeue_false", encoding = "auto" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement[]>())!;
    }

    public async Task<JsonElement> WaitForAsync(string routingKey, Guid assetId)
    {
        var timeout = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < timeout)
        {
            foreach (var message in await ReadAsync())
            {
                using var payload = JsonDocument.Parse(message.GetProperty("payload").GetString()!);
                if (message.GetProperty("routing_key").GetString() == routingKey &&
                    payload.RootElement.GetProperty("assetId").GetGuid() == assetId)
                    return payload.RootElement.Clone();
            }
            await Task.Delay(100);
        }
        throw new TimeoutException($"No {routingKey} for asset {assetId} received within 30 seconds");
    }

    public async Task PurgeAsync()
    {
        using var response = await _management.DeleteAsync($"queues/%2F/{Queue}/contents");
        response.EnsureSuccessStatusCode();
    }

    public async Task StopApplicationAsync()
    {
        var result = await _container.ExecAsync(["rabbitmqctl", "stop_app"]);
        Assert.Equal(0, result.ExitCode);
    }

    public async Task StartApplicationAsync()
    {
        var result = await _container.ExecAsync(["rabbitmqctl", "start_app"]);
        Assert.Equal(0, result.ExitCode);
    }

    public async ValueTask DisposeAsync()
    {
        _management?.Dispose();
        await _container.DisposeAsync();
    }
}
