using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Models;

public sealed record DroneTelemetryModel(
    string Id,
    Location? Location,
    float Speed,
    float Height,
    DroneType DroneType,
    DateTimeOffset LastUpdatedAt,
    DroneState State,
    Location? Destination
);
