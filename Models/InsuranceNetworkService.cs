using System;
using SalamatyAPI.Models.Enums;

namespace Salamaty.API.Models
{
    public class InsuranceNetworkService
    {
        public int Id { get; set; }

        public int InsuranceProviderId { get; set; }
        public InsuranceProvider InsuranceProvider { get; set; } = null!;

        public string Name { get; set; } = null!;
        public InsuranceServiceType Type { get; set; }
       
        public string Code { get; set; } = string.Empty;

        public string Address { get; set; } = null!;
        public string? Phone { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public TimeSpan OpenFrom { get; set; }
        public TimeSpan OpenTo { get; set; }
    }
}
