using DevOpsProject.Shared.Enums;
using System.Text.Json.Serialization;

namespace DevOpsProject.Shared.Models.HiveMindCommands
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "commandType", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
    [JsonDerivedType(typeof(HiveMindCommand), nameof(HiveMindState.None))]
    [JsonDerivedType(typeof(MoveHiveMindCommand), nameof(HiveMindState.Move))]
    [JsonDerivedType(typeof(StopHiveMindCommand), nameof(HiveMindState.Stop))]
    [JsonDerivedType(typeof(AddInterferenceToHiveMindCommand), nameof(HiveMindState.SetInterference))]
    [JsonDerivedType(typeof(DeleteInterferenceFromHiveMindCommand), nameof(HiveMindState.DeleteInterference))]
    [JsonDerivedType(typeof(AddDroneCommand), nameof(HiveMindState.AddDrone))]
    [JsonDerivedType(typeof(DeleteDroneCommand), nameof(HiveMindState.DeleteDrone))]
    [JsonDerivedType(typeof(SimulateBadConnectionCommand), nameof(HiveMindState.SimulateDeadConnection))]
    [JsonDerivedType(typeof(StopBadConnectionSimulationCommand), nameof(HiveMindState.StopDeadConnectionSimulation))]
    [JsonDerivedType(typeof(SimulateDroneStoppedOperatingCommand), nameof(HiveMindState.SimulateDroneStoppedOperating))]
    [JsonDerivedType(typeof(StopDroneStoppedOperatingSimulationCommand), nameof(HiveMindState.StopDroneStoppedOperatingSimulation))]
    public class HiveMindCommand
    {
        public HiveMindState CommandType { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
