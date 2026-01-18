using Contacts.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Contacts.IntegrationTests.Fixtures;

public class ContactsApiFactory : WebApplicationFactory<Startup>, IAsyncLifetime
{
    public CosmosDbFixture CosmosFixture { get; } = new CosmosDbFixture();

    public async Task InitializeAsync()
    {
        await CosmosFixture.InitializeAsync();
    }

    public new async Task DisposeAsync()
    {
        await CosmosFixture.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. Setup the configuration
        IConfiguration? configuration = null;

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {

            // Add the appsettings.test.json from the test project's output directory
            var testConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.test.json");
            configBuilder.AddJsonFile(testConfigPath, optional: false, reloadOnChange: true);

            configuration = configBuilder.Build();
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<CosmosClient>();
            services.RemoveAll<Container>();

            var dbName = configuration!["Cosmos:Db"];
            var containerName = configuration!["Cosmos:Container"];

            var cosmosClient = CosmosFixture.CreateCosmosClient();

            cosmosClient.CreateDatabaseIfNotExistsAsync(dbName).GetAwaiter().GetResult();
            var db = cosmosClient.GetDatabase(dbName);
            db.CreateContainerIfNotExistsAsync(containerName, "/partitionKey").GetAwaiter().GetResult();
            var container = db.GetContainer(containerName);

            services.AddSingleton(cosmosClient);
            services.AddSingleton(container);
        });
    }
}