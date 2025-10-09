using DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Services.Interfaces
{
    public interface IHiveMindService
    {
        Task ConnectHive();
        bool AddInterference(InterferenceModel interferenceModel);
        void RemoveInterference(Guid interferenceId);
        void StopAllTelemetry();
    }
}