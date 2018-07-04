using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DizQueDisse.Models;

namespace DizQueDisse.Services
{
    /// <summary>
    /// This service collects info from IPMA (Instituto Português do Mar e da Atmosfera) API, parses it and constructs
    /// WeatherReport objects that will then be used by the WeatherService to compose tweets.
    /// </summary>
    public class IPMAService
    {
        private static HttpClient _client = new HttpClient();

        /// <summary>
        /// Gets the list of WeatherReports for all locations included in 'locationCodes' for a given date.
        /// </summary>
        /// <param name="dateOfForecast">the date of the weather forecast</param>
        /// <returns>The list of weather report objects for each relevant location for a given date</returns>
        public async Task<WeatherReport[]> GetWeatherReports(DateTime dateOfForecast)
        {
            List<WeatherReport> allReports = new List<WeatherReport>();
            
            //construct a report for each location:
            foreach (string locationCode in locationCodes.Keys)
                allReports.Add(await GetWeatherReport(locationCode, dateOfForecast));

            return allReports.ToArray();
        }

        /// <summary>
        /// Gets weather forecast info from IPMA's API for a given date and location.
        /// It deserializes info into multiple IPMA objects and selects the relevant one 
        /// (the one that includes average temp for the date provided)
        /// and returns the final weather report object constructed from this IPMA object
        /// </summary>
        /// <param name="locationCode">The IPMA code for the location for which the weather forecast is to be obtained</param>
        /// <param name="dateOfForecast">The date for which the weather forecast is to be obtained</param>
        /// <returns>Final WeatherReport for a given location and date</returns>
        private async Task<WeatherReport> GetWeatherReport(string locationCode, DateTime dateOfForecast)
        {
            //get and deserialize all forecast (for all date/time available) for a given location
            var url = $"http://api.ipma.pt/json/alldata/{locationCode}.json";
            var response = await _client.GetAsync(url);
            string jsonString = response.Content.ReadAsStringAsync().Result;
            List<IPMA> l = JsonConvert.DeserializeObject<List<IPMA>>(jsonString);

            //select the ipma object corresponding to the given date of forecast and to a time which has average day temperature available
            string aux = dateOfForecast.ToString("yyyy-MM-dd");
            IPMA ipma = l.Where(x => x.dataPrev.StartsWith(aux) && !x.tMax.StartsWith("-99")).FirstOrDefault();

            return IPMAToWeatherReport(ipma, dateOfForecast, locationCode); 
        }


        /// <summary>
        /// Constructs a weather report object out of a ipma object
        /// that comes from IPMA's API, extracting only the relevant information.
        /// </summary>
        /// <param name="ipma">IPMA object from JSON deserialization of IPMA's API response</param>
        /// <param name="date">The weather forecast date of the report</param>
        /// <param name="locationcode">The location code of the city in the weather report</param>
        /// <returns>The WeatherReport object constructed from IPMA info</returns>
        private WeatherReport IPMAToWeatherReport(IPMA ipma, DateTime date, string locationcode)
        {
            WeatherReport wr = new WeatherReport();
            wr.Location = locationCodes[locationcode];
            wr.Date = date;
            wr.TempMax = ipma.tMax.ToString();
            wr.TempMin = ipma.tMin.ToString();
            wr.WeatherType = forecastDescPT[int.Parse(ipma.idTipoTempo)];

            return wr;
        }


        /// <summary>
        /// IPMA's location codes for the cities that should be included in final weather report.
        /// </summary>
        private Dictionary<string, string> locationCodes = new Dictionary<string, string>(new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("1110600","Lisboa"),
            new KeyValuePair<string, string>("1131200","Porto"),
            new KeyValuePair<string, string>("1060300","Coimbra"),
            new KeyValuePair<string, string>("1080500","Faro"),
        });


        /// <summary>
        /// Forecast descriptions from IPMA's website.
        /// </summary>
        private string[] forecastDescPT = {
            "Sem informação",
            "Céu limpo",
            "Céu pouco nublado",
            "Céu parcialmente nublado",
            "Céu muito nublado ou encoberto",
            "Céu nublado por nuvens altas",
            "Aguaceiros",
            "Aguaceiros fracos",
            "Aguaceiros fortes",
            "Chuva",
            "Chuva fraca ou chuvisco",
            "Chuva forte",
            "Períodos de chuva",
            "Períodos de chuva fraca",
            "Períodos de chuva forte",
            "Chuvisco",
            "Neblina",
            "Nevoeiro ou nuvens baixas",
            "Neve",
            "Trovoada",
            "Aguaceiros e trovoada",
            "Granizo",
            "Geada",
            "Chuva forte e trovoada",
            "Nebulosidade convectiva",
            "Céu com periodos muito nublado",
            "Nevoeiro",
            "Céu nublado"
        };

        /// <summary>
        /// Auxilary class that is used to deserialize the JSON object returned from IPMA's API
        /// </summary>
        private class IPMA
        {
            public string tempAguaMar { get; set; }
            public string idTipoTempo { get; set; }
            public string probabilidadePrecipita { get; set; }
            public string tMax { get; set; }
            public string utci { get; set; }
            public string dirOndulacao { get; set; }
            public string periodOndulacao { get; set; }
            public string ddVento { get; set; }
            public string tMed { get; set; }
            public string tMin { get; set; }
            public string ondulacao { get; set; }
            public string hR { get; set; }
            public string dataUpdate { get; set; }
            public string vaga { get; set; }
            public string ffVento { get; set; }
            public string idIntensidadePrecipita { get; set; }
            public string globalIdLocal { get; set; }
            public string marTotal { get; set; }
            public string idPeriodo { get; set; }
            public string dataPrev { get; set; }
            public string idFfxVento { get; set; }
            public string iUv { get; set; }
        }
    }
}
