using System.Globalization;

namespace DevOpsProject.Shared.Models
{
    public struct Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public override string ToString()
        {
            var invariantCulture = CultureInfo.InvariantCulture;
            return $"({Latitude.ToString("F6", invariantCulture)},{Longitude.ToString("F6", invariantCulture)}))";
        }
    }
}
