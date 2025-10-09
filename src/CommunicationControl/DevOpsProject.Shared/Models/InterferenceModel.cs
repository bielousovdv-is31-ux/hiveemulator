namespace DevOpsProject.Shared.Models
{
    public class InterferenceModel
    {
        public Guid Id { get; set; }
        public double RadiusKM { get; set; }
        public Location Location { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
