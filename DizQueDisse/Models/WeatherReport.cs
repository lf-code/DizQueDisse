using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DizQueDisse.Models
{
    public class WeatherReport
    {
        public DateTime Date { get; set; }
        public string Location { get; set; }
        public string TempMin { get; set; }
        public string TempMax { get; set; }
        public string WeatherType { get; set; }

    }
}
