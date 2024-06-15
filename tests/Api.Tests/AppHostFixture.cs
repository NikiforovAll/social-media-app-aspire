namespace Api.Tests;

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
    public DistributedApplication AppHost { get; } = fixture.AppHost = default!;
}
