namespace Api.Tests;

using System.Security.Cryptography;
using Api.Tests;
using Microsoft.Extensions.DependencyInjection;

public static partial class DistributedApplicationExtensions
{
    /// <summary>
    /// Replaces all named volumes with anonymous volumes so they're isolated across test runs and from the volume the app uses during development.
    /// </summary>
    /// <remarks>
    /// Note that if multiple resources share a volume, the volume will instead be given a random name so that it's still shared across those resources in the test run.
    /// </remarks>
    public static TBuilder WithRandomVolumeNames<TBuilder>(
        this TBuilder builder
    )
        where TBuilder : IDistributedApplicationTestingBuilder
    {
        // Named volumes that aren't shared across resources should be replaced with anonymous volumes.
        // Named volumes shared by mulitple resources need to have their name randomized but kept shared across those resources.

        // Find all shared volumes and make a map of their original name to a new randomized name
        var allResourceNamedVolumes = builder
            .Resources.SelectMany(r =>
                r.Annotations.OfType<ContainerMountAnnotation>()
                    .Where(m =>
                        m.Type == ContainerMountType.Volume
                        && !string.IsNullOrEmpty(m.Source)
                    )
                    .Select(m => (Resource: r, Volume: m))
            )
            .ToList();
        var seenVolumes = new HashSet<string>();
        var renamedVolumes = new Dictionary<string, string>();
        foreach (var resourceVolume in allResourceNamedVolumes)
        {
            var name = resourceVolume.Volume.Source!;
            if (!seenVolumes.Add(name) && !renamedVolumes.ContainsKey(name))
            {
                renamedVolumes[name] =
                    $"{name}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";
            }
        }

        // Replace all named volumes with randomly named or anonymous volumes
        foreach (var resourceVolume in allResourceNamedVolumes)
        {
            var resource = resourceVolume.Resource;
            var volume = resourceVolume.Volume;
            var newName = renamedVolumes.TryGetValue(
                volume.Source!,
                out var randomName
            )
                ? randomName
                : null;
            var newMount = new ContainerMountAnnotation(
                newName,
                volume.Target,
                ContainerMountType.Volume,
                volume.IsReadOnly
            );
            resource.Annotations.Remove(volume);
            resource.Annotations.Add(newMount);
        }

        return builder;
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource.
    /// </summary>
    public static HttpClient CreateHttpClient(
        this DistributedApplication app,
        string resourceName,
        bool useHttpClientFactory
    ) => app.CreateHttpClient(resourceName, null, useHttpClientFactory);

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource.
    /// </summary>
    public static HttpClient CreateHttpClient(
        this DistributedApplication app,
        string resourceName,
        string? endpointName,
        bool useHttpClientFactory
    )
    {
        if (useHttpClientFactory)
        {
            return app.CreateHttpClient(resourceName, endpointName);
        }

        // Don't use the HttpClientFactory to create the HttpClient so, e.g., no resilience policies are applied
        var httpClient = new HttpClient
        {
            BaseAddress = app.GetEndpoint(resourceName, endpointName)
        };

        return httpClient;
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured to communicate with the specified resource with custom configuration.
    /// </summary>
    public static HttpClient CreateHttpClient(
        this DistributedApplication app,
        string resourceName,
        string? endpointName,
        Action<IHttpClientBuilder> configure
    )
    {
        var services = new ServiceCollection()
            .AddHttpClient()
            .ConfigureHttpClientDefaults(configure)
            .BuildServiceProvider();
        var httpClientFactory =
            services.GetRequiredService<IHttpClientFactory>();

        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = app.GetEndpoint(resourceName, endpointName);

        return httpClient;
    }
}
