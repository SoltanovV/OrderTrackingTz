using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OrderTracking.Infrastructure.Persistence;
using OrderTracking.Infrastructure.Services;
using OrderTracking.Infrastructure.Services.Background;
using OrderTracking.Infrastructure.Utilities;

namespace OrderTracking.Tests;

public sealed class TestHost : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            connection.Open();
            var options = new DbContextOptionsBuilder<OrdersDbContext>().UseSqlite(connection).Options;
            services.RemoveAll<DbContextOptions<OrdersDbContext>>();
            services.AddSingleton(options);
            var relay = services.Single(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(RabbitMqWorker));
            services.Remove(relay);
            using var db = new OrdersDbContext(options);
            db.Database.EnsureCreated();
        });
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}
