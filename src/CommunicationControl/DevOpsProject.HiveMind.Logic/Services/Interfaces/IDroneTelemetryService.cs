using DevOpsProject.HiveMind.Logic.Models;

namespace DevOpsProject.HiveMind.Logic.Services.Interfaces;

public interface IDroneTelemetryService
{
    bool TryAdd(DroneTelemetryModel model);
    bool TryRemove(string droneId);
    void LogTelemetry();
    DroneTelemetryModel GetTelemetryModel(string droneId);
    void UpdateHiveMindLocation();
    void Update(DroneTelemetryModel model);
}