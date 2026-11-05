using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Middleware;
using QTKD.Application;
using QTKD.Infrastructure;
using QTKD.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ── Middleware xử lý lỗi nghiệp vụ (đặt trước UseSwagger / MapControllers) ─
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
    await db.Database.MigrateAsync();

    if (args.Contains("--reset-db"))
    {
        Console.WriteLine("--> Đang reset toàn bộ dữ liệu database về ban đầu...");
        await DbSeeder.ResetAndSeedAsync(db);
        Console.WriteLine("--> Reset database thành công!");
        return;
    }

    await DbSeeder.SeedAsync(db);
}

app.MapControllers();
app.Run();

public partial class Program { }   // để Tuần 07 dùng WebApplicationFactory<Program>
