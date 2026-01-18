using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Azure.Cosmos;
using Testcontainers.CosmosDb;

namespace Contacts.IntegrationTests.Fixtures;

public class CosmosDbFixture : IAsyncLifetime
{
    private readonly CosmosDbContainer _container = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview")
        .WithPortBinding(8080, true)
        .WithEnvironment("PROTOCOL", "https")  // Required for .NET SDK
        .WithEnvironment("ENABLE_EXPLORER", "false")
        .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitForHealthCheck()))
        .Build();

    public CosmosClient CreateCosmosClient()
    {
        var clientOptions = new CosmosClientOptions
        {
            ConnectionMode = Microsoft.Azure.Cosmos.ConnectionMode.Gateway,
            HttpClientFactory = () => _container.HttpClient,
        };

        var connectionString = _container.GetConnectionString();
        return new CosmosClient(connectionString, clientOptions);
    }

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    private sealed class WaitForHealthCheck() : IWaitUntil
    {
        public async Task<bool> UntilAsync(IContainer container)
        {
            using var httpClient = new HttpClient();
            var port = container.GetMappedPublicPort(8080);

            try
            {
                using var httpResponse = await httpClient.GetAsync($"http://{container.Hostname}:{port}/ready").ConfigureAwait(false);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return false;
                }

                // Wait a little longer because the Cosmos emulator healthcheck succeeds before the container is ready
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
