using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QTKD.Application.Interfaces;
using QTKD.Infrastructure.Persistence;

namespace QTKD.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("Default")
                 ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Default");

        services.AddDbContext<QtkdDbContext>(o => o.UseNpgsql(cs));
        services.AddScoped<IQtkdDbContext>(sp => sp.GetRequiredService<QtkdDbContext>());
        return services;
    }
}
