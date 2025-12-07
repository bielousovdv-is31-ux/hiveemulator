using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Dto;

public sealed record DroneDto(
    string Id, 
    DroneType Type, 
    ConnectionState ConnectionState,
    Location? Location,
    DateTimeOffset TelemetryLastUpdatedAt,
    DroneState State,
    Location? Destination,
    string ConnectionName,
    DateTimeOffset? ConnectionLastUpdatedAt);
