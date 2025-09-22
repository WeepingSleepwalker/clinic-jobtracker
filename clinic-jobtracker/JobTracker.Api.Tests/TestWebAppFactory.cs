using System.Data.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using JobTracker.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace JobTracker.Api.Tests;

public sealed class TestWebAppFactory : WebApplicationFactory<Program>
{
    private DbConnection? _conn;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real AppDb registration
            var toRemove = services.Single(s => s.ServiceType == typeof(DbContextOptions<AppDb>));
            services.Remove(toRemove);

            // Shared in-memory SQLite for this factory
            _conn = new SqliteConnection("DataSource=:memory:;Cache=Shared");
            _conn.Open();

            services.AddDbContext<AppDb>(o => o.UseSqlite(_conn!));
        });

        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        db.Database.EnsureCreated(); // no seed; tests seed explicitly

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _conn?.Dispose();
    }
}
