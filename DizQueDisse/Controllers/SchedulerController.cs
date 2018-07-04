using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DizQueDisse.Services;
using Microsoft.AspNetCore.Authorization;

namespace DizQueDisse.Controllers
{
    [Authorize]
    public class SchedulerController : Controller
    {
        private readonly QuoteService _quoteService;
        private readonly WeatherService _weatherService;

        public SchedulerController(QuoteService quoteService, WeatherService weatherService)
        {
            _quoteService = quoteService;
            _weatherService = weatherService;
        }

        /// <summary>
        /// Allows AzureFuctions (or the manager) to prompt QuoteService to act (construct a tweet and publish it).
        /// </summary>
        [HttpGet("/scheduler/quote")]
        public async Task<IActionResult> Quote()
        {
            bool res = await _quoteService.Act();
            return Ok(res);
        }

        /// <summary>
        /// Allows AzureFuctions (or the manager) to prompt WeatherService to act(construct a tweet and publish it).
        /// </summary>
        [HttpGet("/scheduler/weather")]
        public async Task<IActionResult> Weather()
        {
            bool res = await _weatherService.Act();
            return Ok(res);
        }
    }
}
