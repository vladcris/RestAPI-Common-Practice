using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyBGList.Models;
using System.Data.Common;

namespace MyBGList.Tests;

public class Samples : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _fixture;

    public Samples(CustomWebApplicationFactory<Program> fixture)
    {
        _fixture = fixture;
    }
    [Fact]
    public async void Sample_TestHost() {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost => {
                webHost.ConfigureServices(services => {
                    services.AddControllers();
                });
                webHost.Configure(app => {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => {
                        endpoints.MapGet("/", async context => {
                            await context.Response.WriteAsync("Hello World!");
                        });
                    });
                })
            .UseTestServer();
            });
        var host = await hostBuilder.StartAsync();

        var client = host.GetTestClient();
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        content.Should().Be("Hello World!");    
    }

    [Fact]
    public async void Test_LiveHost() {
        var sut = _fixture.CreateClient();

        var response = await sut.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();   

        content.Should().Be("Hello World!");
    }
}


public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.ConfigureServices(services => {
            ServiceDescriptor? dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                    typeof(DbContextOptions<ApplicationDbContext>));

            services.Remove(dbContextDescriptor!);


            // Create open SqliteConnection so EF won't automatically close it.
            services.AddSingleton<DbConnection>(container => {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();

                return connection!;
            });

            services.AddDbContext<ApplicationDbContext>((container, options) => {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
        });


        builder.UseEnvironment("Development");
    }
}