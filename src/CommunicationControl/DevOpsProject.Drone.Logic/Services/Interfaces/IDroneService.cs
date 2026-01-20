using DevOpsProject.Shared.Models;

namespace DevOpsProject.Drone.Logic.Services.Interfaces;

public interface IDroneService
{
    void StartMoving(Location destination);
    void StopMoving();
}