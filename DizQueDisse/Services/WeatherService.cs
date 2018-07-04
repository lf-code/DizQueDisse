using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DizQueDisse.Data;
using DizQueDisse.Models;

namespace DizQueDisse.Services
{
    /// <summary>
    /// Weather Services builds and publishes a tweet with weather forecast information, 
    /// whenever it is prompted to do so by the MainTimerService.
    /// </summary>
    public class WeatherService
    {

        private readonly IPMAService _ipmaService;
        private readonly TwitterService _twitterService;
        private readonly ILogger _logger;

        public WeatherService(IPMAService injectedIpmaService, TwitterService twitterService, ILogger<WeatherService> logger)
        {
            _twitterService = twitterService;
            _ipmaService = injectedIpmaService;
            _logger = logger;
        }


        /// <summary>
        /// This method allows MainTimerService to invoke this service, prompt it to publish a tweet with weather forecast.
        /// </summary>
        /// <returns>Weather or not this service was invoked successfully and was able to publish the tweet correctly.</returns>
        public async Task<bool> Act()
        {
            _logger.LogInformation("[{0}] WeatherService Act started...", DateTime.UtcNow);

            // 0) If it is past midday, get forecast for tomorrow's weather, otherwise get forecast for today:
            DateTime forecasteDate = DateTime.UtcNow.Hour > 12 ? DateTime.UtcNow.AddDays(1) : DateTime.UtcNow;

            // 1) Obtain weather reports fom IPMAService
            WeatherReport[] weather = await _ipmaService.GetWeatherReports(forecasteDate);

            // 2) build a formatted string from such reports.
            string weatherString = GetWeatherString(weather);

            // 3) try to publish such string and return the result:
            if (!(await TweetWeather(weatherString)))
                return false;

            _logger.LogInformation("[{0}] WeatherService Act ended successfully", DateTime.UtcNow);

            return true;
        }

        /// <summary>
        /// Builds a formatted string, to be published as a tweet, containing
        /// the weather information in WeatherReports from IPMAService.
        /// </summary>
        /// <param name="weatherReports">WeatherReports from IPMAService</param>
        /// <returns>Formatted string with weather forecast info</returns>
        string GetWeatherString(WeatherReport[] weatherReports)
        {
            DateTime forecastDate = weatherReports[0].Date;

            //randomize order of weather reports so tweets look more 
            //distinct from each other and are not blocked by twitter api:
            Random r = new Random();
            var sortedWeatherReports = weatherReports.OrderBy(x => r.Next(0, 1000000));

            //build formatted string:
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Previsão para dia {forecastDate.ToString("dd-MM-yyyy")}:");
            foreach (WeatherReport w in sortedWeatherReports)
                sb.AppendLine($"{w.Location}: {w.WeatherType} - MIN: {w.TempMin}ºC MAX: {w.TempMax}ºC ");

            return sb.ToString();
        }


        /// <summary>
        /// This method uses TwitterService to publish a tweet with a given weather information.
        /// </summary>
        /// <param name="weatherString">Text with weather info</param>
        /// <returns>whether or not such tweet was published successfully</returns>
        async Task<bool> TweetWeather(string weatherString)
        {
            return await _twitterService.SendTweet(weatherString);
        }


    }
}
