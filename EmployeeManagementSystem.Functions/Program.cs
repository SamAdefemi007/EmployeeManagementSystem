using EmployeeManagementSystem.DataAccess;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

// Only needed if you want to host OpenAPI, custom routing, etc.
// If you don't need it, you can remove this line.
builder.ConfigureFunctionsWebApplication();

// Example: If you want Application Insights, uncomment and configure

// builder.Services.AddApplicationInsightsTelemetryWorkerService()
//   .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton(serv =>
{
    var logger = serv.GetRequiredService<ILogger<EmployeeManagementRepository>>();

    try
    {
        var cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosConnectionString");
        var databaseName = Environment.GetEnvironmentVariable("DatabaseName");
        var containerName = Environment.GetEnvironmentVariable("ContainerName");
        var partitionKey = Environment.GetEnvironmentVariable("PartitionKey");

        // Basic validation
        if (string.IsNullOrWhiteSpace(cosmosConnectionString))
        {
            logger.LogError("CosmosConnectionString environment variable is missing or empty.");
            throw new InvalidOperationException("Missing CosmosConnectionString environment variable.");
        }
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            logger.LogError("DatabaseName environment variable is missing or empty.");
            throw new InvalidOperationException("Missing DatabaseName environment variable.");
        }
        if (string.IsNullOrWhiteSpace(containerName))
        {
            logger.LogError("ContainerName environment variable is missing or empty.");
            throw new InvalidOperationException("Missing ContainerName environment variable.");
        }
        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            logger.LogError("PartitionKey environment variable is missing or empty.");
            throw new InvalidOperationException("Missing PartitionKey environment variable.");
        }

        // Construct and initialize your repository
        var repository = new EmployeeManagementRepository(cosmosConnectionString, logger);
        repository.InitializeConnectionAsync(databaseName, containerName, partitionKey)
                  .GetAwaiter()
                  .GetResult();

        logger.LogInformation("Successfully initialized the EmployeeManagementRepository");
        return repository;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred while configuring EmployeeManagementRepository.");
        // Rethrow so the Functions runtime is aware the host startup failed.
        throw;
    }
});

builder.Services.AddScoped<IEmployeeManagementRepository, EmployeeManagementRepository>();

builder.Build().Run();
