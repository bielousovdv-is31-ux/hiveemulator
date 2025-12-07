using Microsoft.Extensions.DependencyInjection;

namespace DevOpsProject.Shared.Simulation;

public static class SimulationUtilityExtensions
{
    public static IServiceCollection AddSimulationUtility(this IServiceCollection services)
    {
        return services.AddSingleton<ISimulationUtility, SimulationUtility>();
    }
}
