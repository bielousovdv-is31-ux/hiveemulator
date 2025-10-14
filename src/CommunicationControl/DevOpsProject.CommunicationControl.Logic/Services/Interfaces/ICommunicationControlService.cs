using DevOpsProject.Shared.Models;

namespace DevOpsProject.CommunicationControl.Logic.Services.Interfaces
{
    public interface ICommunicationControlService
    {
        Task<bool> DisconnectHive(string hiveId);
        Task<HiveModel> GetHiveModel(string hiveId);
        Task<InterferenceModel> GetInterferenceModel(Guid interferenceId);
        Task<List<HiveModel>> GetAllHives();
        Task<List<InterferenceModel>> GetAllInterferences();
        Task<HiveOperationalArea> ConnectHive(HiveModel model);
        Task<bool> IsHiveConnected(string hiveId);
        Task<DateTime> AddTelemetry(HiveTelemetryModel model);
        Task<string> SendHiveControlSignal(string hiveId, Location destination);

        Task<Guid> SetInterference(InterferenceModel model);

        Task<bool> DeleteInterference(Guid interferenceId);

        Task NotifyHivesOnDeletedInterference(Guid interferenceId);

        Task NotifyHivesAboutAddedInterference(Guid interferenceId);
        Task<string> SendHiveStopSignal(string hiveId);
    }
}
