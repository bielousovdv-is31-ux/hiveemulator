using DevOpsProject.Shared.Models;

namespace DevOpsProject.CommunicationControl.API.DTO.Client.Request
{
    public class AddInterferenceRequest
    {
        public double RadiusKM { get; init; }
        public Location Location { get; init; }
    }
}
