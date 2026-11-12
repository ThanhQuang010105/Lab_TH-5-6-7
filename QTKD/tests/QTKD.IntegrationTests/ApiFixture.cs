using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using QTKD.Infrastructure.Persistence;

namespace QTKD.IntegrationTests;

/// <summary>
/// Khởi động API thật (WebApplicationFactory) nối vào database RIÊNG "qtkd_test".
/// Chuỗi kết nối lấy từ: biến môi trường QTKD_TEST_CONNECTION, hoặc User Secrets của QTKD.Api
/// (đổi Database thành qtkd_test). Mật khẩu không nằm trong Git.
/// </summary>
public class ApiFixture : IAsyncLifetime
{
    // Trùng UserSecretsId trong QTKD.Api.csproj
    private const string ApiUserSecretsId = "b2aaaf95-1822-4c7a-b17e-63fd3c89c48c";
    public const string TestDatabaseName = "qtkd_test";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private WebApplicationFactory<Program> _factory = default!;
    public HttpClient Client { get; private set; } = default!;
    public string ConnectionString { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        ConnectionString = BuildTestConnectionString();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");                       // Program tự migrate ở Development
            b.UseSetting("ConnectionStrings:Default", ConnectionString);
        });
        Client = _factory.CreateClient();                          // khởi động host -> migrate qtkd_test

        // Đưa database test về dữ liệu mẫu ban đầu
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        await DbSeeder.ResetAndSeedAsync(db);
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    /// <summary>Mở DbContext mới để đối chiếu dữ liệu trực tiếp trong PostgreSQL.</summary>
    public QtkdDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<QtkdDbContext>().UseNpgsql(ConnectionString).Options;
        return new QtkdDbContext(options);
    }

    private static string BuildTestConnectionString()
    {
        var cs = Environment.GetEnvironmentVariable("QTKD_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(cs))
        {
            var cfg = new ConfigurationBuilder().AddUserSecrets(ApiUserSecretsId).Build();
            cs = cfg.GetConnectionString("Default");
        }
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "Chưa có chuỗi kết nối. Đặt biến môi trường QTKD_TEST_CONNECTION hoặc User Secrets cho QTKD.Api.");

        var builder = new NpgsqlConnectionStringBuilder(cs) { Database = TestDatabaseName };
        return builder.ConnectionString;     // luôn ép về qtkd_test, không bao giờ chạm database thật
    }

    // ── helper gọi API ──
    public static async Task<T> ReadAsync<T>(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<T>(Json))!;
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFixture> { }
