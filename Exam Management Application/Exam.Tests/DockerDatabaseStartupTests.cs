using Exam.Web;
using Xunit;

namespace Exam.Tests;

public sealed class DockerDatabaseStartupTests
{
    [Theory]
    [InlineData(".\\SQLEXPRESS")]
    [InlineData("(localdb)\\MSSQLLocalDB")]
    [InlineData("localhost,1433")]
    [InlineData("remote-server,14333")]
    public void RejectsSqlEndpointsOutsideDocker(string server)
    {
        Assert.Throws<InvalidOperationException>(() =>
            DockerDatabaseStartup.Connection($"Server={server};Database=ExamAppDocker;Integrated Security=True"));
    }

    [Theory]
    [InlineData("tcp:127.0.0.1,14333")]
    [InlineData("localhost,14333")]
    public void AllowsDockerDatabaseAndIsolatedTestDatabases(string server)
    {
        var settings = DockerDatabaseStartup.Connection($"Server={server};Database=ExamAppTests_example;Integrated Security=True");
        Assert.Equal("ExamAppTests_example", settings.InitialCatalog);
    }

    [Fact]
    public void RequiresAnExplicitDatabase()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DockerDatabaseStartup.Connection("Server=tcp:127.0.0.1,14333;Integrated Security=True"));
    }
}
