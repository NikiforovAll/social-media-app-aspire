namespace Api.Tests;

using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

public class AppHostFixture : IAsyncLifetime
{
    public DistributedApplication? AppHost { get; set; }

    public async Task InitializeAsync()
    {
        var appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();

        this.AppHost = await appHost.BuildAsync();
        await this.AppHost.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (this.AppHost is not null)
        {
            await this.AppHost.StopAsync();
            await this.AppHost.DisposeAsync();
        }
    }
}

[CollectionDefinition(nameof(AppHostCollection))]
public sealed class AppHostCollection : ICollectionFixture<AppHostFixture>;

[Collection(nameof(AppHostCollection))]
public abstract class AppHostContext(AppHostFixture fixture)
{
    public HttpClient Client { get; } =
        fixture.AppHost?.CreateHttpClient(
            "api",
            null,
            _ =>
                _.ConfigureHttpClient(client =>
                        client.Timeout = Timeout.InfiniteTimeSpan
                    )
                    .AddStandardResilienceHandler(resilience =>
                    {
                        resilience.TotalRequestTimeout.Timeout =
                            TimeSpan.FromSeconds(60);
                        resilience.AttemptTimeout.Timeout =
                            TimeSpan.FromSeconds(10);
                        resilience.Retry.MaxRetryAttempts = 10;
                        resilience.CircuitBreaker.SamplingDuration =
                            TimeSpan.FromSeconds(300);
                    })
        )!;
}
