using ErganiManager.Core.Interfaces;
using ErganiManager.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ErganiManager.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all ErganiManager.Core services. Note: IWorkCardSubmitter is
    /// NOT registered here — it must be supplied by the host application once
    /// the Ergani API client (a later phase) is implemented, since Core has no
    /// HTTP dependency by design.
    /// </summary>
    public static IServiceCollection AddErganiManagerCore(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ICompanyContext, CompanyContextService>();
        services.AddSingleton<IConnectionStateService, ConnectionStateService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICacheSyncService, CacheSyncService>();
        services.AddScoped<IAdminBootstrapService, AdminBootstrapService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IWorkCardHistoryService, WorkCardHistoryService>();
        services.AddScoped<IOvertimeService, OvertimeService>();

        return services;
    }
}
