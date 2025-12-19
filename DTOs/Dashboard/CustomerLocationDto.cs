using System;

namespace EcomerceBE.DTOs.Dashboard
{
    public class CustomerLocationDto
    {
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
        public double Percentage { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}


