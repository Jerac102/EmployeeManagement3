using EmployeeManagement.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EmployeeManagementDbContext>(options => options.UseSqlServer(connectionString));
        services.AddTransient<EmployeeManagementService>();
        return services;
    }
}
