namespace Microsoft.Extensions.Hosting;

using Aspire.Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class AspireElasticClientsElasticsearchExtensions
{
    private const string DefaultConfigSectionName =
        "Aspire:Elastic:Clients:Elasticsearch";

    /// <summary>
    /// Registers <see cref="ElasticsearchClient"/> instance for connecting to Elasticsearch with Elastic.Clients.Elasticsearch client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientSettings">An optional delegate that can be used for customizing ElasticsearchClientSettings.</param>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="ElasticClientsElasticsearchSettings.ConnectionString"/> is not provided.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="ElasticClientsElasticsearchSettings.Cloud"/> is not provided and <see cref="ElasticClientsElasticsearchSettings.UseCloud" /> is true .</exception>
    public static void AddElasticClientsElasticsearch(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<ElasticClientsElasticsearchSettings>? configureSettings = null,
        Action<ElasticsearchClientSettings>? configureClientSettings = null
    ) =>
        builder.AddElasticClientsElasticsearch(
            DefaultConfigSectionName,
            configureSettings,
            configureClientSettings,
            connectionName,
            null
        );

    /// <summary>
    /// Registers <see cref="ElasticsearchClient"/> instance for connecting to Elasticsearch with Elastic.Clients.Elasticsearch client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientSettings">An optional delegate that can be used for customizing ElasticsearchClientSettings.</param>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="ElasticClientsElasticsearchSettings.ConnectionString"/> is not provided.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="ElasticClientsElasticsearchSettings.Cloud"/> is not provided and <see cref="ElasticClientsElasticsearchSettings.UseCloud" /> is true .</exception>
    public static void AddKeyedElasticClientsElasticsearch(
        this IHostApplicationBuilder builder,
        string name,
        Action<ElasticClientsElasticsearchSettings>? configureSettings = null,
        Action<ElasticsearchClientSettings>? configureClientSettings = null
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        builder.AddElasticClientsElasticsearch(
            $"{DefaultConfigSectionName}:{name}",
            configureSettings,
            configureClientSettings,
            connectionName: name,
            serviceKey: name
        );
    }

    private static void AddElasticClientsElasticsearch(
        this IHostApplicationBuilder builder,
        string configurationSectionName,
        Action<ElasticClientsElasticsearchSettings>? configureSettings,
        Action<ElasticsearchClientSettings>? configureClientSettings,
        string connectionName,
        object? serviceKey
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        var configSection = builder.Configuration.GetSection(
            configurationSectionName
        );

        ElasticClientsElasticsearchSettings settings = new();
        configSection.Bind(settings);

        if (
            builder.Configuration.GetConnectionString(connectionName)
            is string connectionString
        )
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        var optionsName = serviceKey is null
            ? Options.Options.DefaultName
            : connectionName;

        var elasticsearchClientSettings = CreateElasticsearchClientSettings(
            settings,
            connectionName,
            configurationSectionName
        );

        configureClientSettings?.Invoke(elasticsearchClientSettings);

        var elasticsearchClient = new ElasticsearchClient(
            elasticsearchClientSettings
        );

        if (serviceKey is null)
        {
            builder.Services.AddSingleton<ElasticsearchClient>(
                elasticsearchClient
            );
        }
        else
        {
            builder.Services.AddKeyedSingleton<ElasticsearchClient>(
                serviceKey,
                elasticsearchClient
            );
        }
    }

    private static ElasticsearchClientSettings CreateElasticsearchClientSettings(
        ElasticClientsElasticsearchSettings settings,
        string connectionName,
        string configurationSectionName
    )
    {
        if (settings.ConnectionString is null)
        {
            throw new InvalidOperationException(
                $"No ConnectionString specified. Ensure a valid connection string was provided in 'ConnectionStrings:{connectionName}' or for the '{configurationSectionName}:ConnectionString' configuration key."
            );
        }
        return new(new Uri(settings.ConnectionString));
    }
}
