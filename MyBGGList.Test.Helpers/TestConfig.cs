using Microsoft.Extensions.Configuration;

namespace MyBGGList.Test.Helpers;
public class TestConfig
{
    public string? ConnectionString { get; set; }
    public TestConfig()
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json")
            .AddEnvironmentVariables()
            .Build();

        ConnectionString = configBuilder.GetConnectionString("TestDbConnection");
    }
}
