namespace Api.Tests;

using System.Net;

public class AppHostTests(AppHostFixture appHostFixture)
    : AppHostContext(appHostFixture)
{
    [Fact]
    public async Task GetUsers_ReturnsOkStatusCode()
    {
        // Arrange
        // Act
        var response = await this.Client.GetAsync("/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
