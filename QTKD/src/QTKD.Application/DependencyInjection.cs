using Microsoft.Extensions.DependencyInjection;
using QTKD.Application.Interfaces;
using QTKD.Application.Services;

namespace QTKD.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IContractService, ContractService>();
        return services;
    }
}
