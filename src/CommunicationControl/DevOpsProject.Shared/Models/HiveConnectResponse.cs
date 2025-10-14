using System.Text.Json.Serialization;

namespace DevOpsProject.Shared.Models
{
    public class HiveConnectResponse
    {
        public bool ConnectResult { get; set; }
        public HiveOperationalArea OperationalArea { get; set; }
        public List<InterferenceModel> Interferences { get; set; }
    }
}
