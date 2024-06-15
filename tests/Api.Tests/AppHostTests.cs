namespace Api.Tests;

using System.Net;

public class AppHostTests(AppHostFixture appHostFixture)
    : AppHostContext(appHostFixture)
{
    [Fact]
    public async Task GetApiResource_ReturnsOkStatusCode()
    {
        // Arrange
        // Act
        var httpClient = this.AppHost.CreateHttpClient("api");

        var response = await httpClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
