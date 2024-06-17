namespace Aspire.Hosting;

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Elasticsearch;

/// <summary>
/// Provides extension methods for adding Elasticsearch resources to the application model.
/// </summary>
public static class ElasticsearchBuilderExtensions
{
    private const int ElasticsearchPort = 9200;
    private const int ElasticsearchInternalPort = 9300;

    public static IResourceBuilder<ElasticsearchResource> AddElasticsearch(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? password = null,
        int? port = null,
        int? internalPort = null
    )
    {
        var passwordParameter =
            password?.Resource
            ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(
                builder,
                $"{name}-password"
            );

        var elasticsearch = new ElasticsearchResource(name, passwordParameter);

        return builder
            .AddResource(elasticsearch)
            .WithImage(
                ElasticsearchContainerImageTags.Image,
                ElasticsearchContainerImageTags.Tag
            )
            .WithImageRegistry(ElasticsearchContainerImageTags.Registry)
            .WithHttpEndpoint(
                targetPort: ElasticsearchPort,
                port: port,
                name: ElasticsearchResource.PrimaryEndpointName
            )
            .WithEndpoint(
                targetPort: ElasticsearchInternalPort,
                port: internalPort,
                name: ElasticsearchResource.InternalEndpointName
            )
            .WithEnvironment("discovery.type", "single-node")
            .WithEnvironment("xpack.security.enabled", "true")
            .WithEnvironment(context =>
                context.EnvironmentVariables["ELASTIC_PASSWORD"] =
                    elasticsearch.PasswordParameter
            );
    }

    public static IResourceBuilder<ElasticsearchResource> WithDataVolume(
        this IResourceBuilder<ElasticsearchResource> builder,
        string? name = null,
        bool isReadOnly = false
    ) =>
        builder.WithVolume(
            name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"),
            "/usr/share/elasticsearch/data",
            isReadOnly
        );

    public static IResourceBuilder<ElasticsearchResource> WithDataBindMount(
        this IResourceBuilder<ElasticsearchResource> builder,
        string source,
        bool isReadOnly = false
    ) =>
        builder.WithBindMount(
            source,
            "/usr/share/elasticsearch/data",
            isReadOnly
        );
}

internal static class VolumeNameGenerator
{
    /// <summary>
    /// Creates a volume name with the form <c>$"{applicationName}-{resourceName}-{suffix}</c>, e.g. <c>"myapplication-postgres-data"</c>.
    /// </summary>
    /// <remarks>
    /// If the application name contains chars that are invalid for a volume name, the prefix <c>"volume-"</c> will be used instead.
    /// </remarks>
    public static string CreateVolumeName<T>(
        IResourceBuilder<T> builder,
        string suffix
    )
        where T : IResource
    {
        if (!HasOnlyValidChars(suffix))
        {
            throw new ArgumentException(
                $"The suffix '{suffix}' contains invalid characters. Only [a-zA-Z0-9_.-] are allowed.",
                nameof(suffix)
            );
        }

        // Create volume name like "myapplication-postgres-data"
        var applicationName = builder
            .ApplicationBuilder
            .Environment
            .ApplicationName;
        var resourceName = builder.Resource.Name;
        return $"{(HasOnlyValidChars(applicationName) ? applicationName : "volume")}-{resourceName}-{suffix}";
    }

    private static bool HasOnlyValidChars(string name)
    {
        // According to the error message from docker CLI, volume names must be of form "[a-zA-Z0-9][a-zA-Z0-9_.-]"
        var nameSpan = name.AsSpan();

        for (var i = 0; i < nameSpan.Length; i++)
        {
            var c = nameSpan[i];

            if (i == 0 && !(char.IsAsciiLetter(c) || char.IsNumber(c)))
            {
                // First char must be a letter or number
                return false;
            }
            else if (
                !(
                    char.IsAsciiLetter(c)
                    || char.IsNumber(c)
                    || c == '_'
                    || c == '.'
                    || c == '-'
                )
            )
            {
                // Subsequent chars must be a letter, number, underscore, period, or hyphen
                return false;
            }
        }

        return true;
    }
}
