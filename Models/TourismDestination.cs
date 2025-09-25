using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TesteLLMs.Models
{
    public class TourismDestination
    {
        public string Destination { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ApproximateAnnualTourists { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string MajorityReligion { get; set; } = string.Empty;
        public string FamousFoods { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string BestTimeToVisit { get; set; } = string.Empty;
        public string CostOfLiving { get; set; } = string.Empty;
        public string Safety { get; set; } = string.Empty;
        public string CulturalSignificance { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
