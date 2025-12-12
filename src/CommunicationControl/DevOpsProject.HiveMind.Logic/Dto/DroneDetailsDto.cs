using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Dto;

public sealed record DroneDetailsDto(
    string Id, 
    DroneType Type, 
    TimeSpan? CurrentLatency,
    Location? Location,
    float? Speed,
    float? Height,
    DateTimeOffset? TelemetryLastUpdatedAt,
    DroneState State,
    Location? Destination,
    string ConnectionName,
    string IpAddress, 
    int? Http1Port, 
    int? GrpcPort, 
    int? UdpPort,
    DateTimeOffset? ConnectionLastUpdatedAt);
    