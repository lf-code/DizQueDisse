using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DizQueDisse.Models;
using DizQueDisse.Data;
using DizQueDisse.Services;
using Microsoft.Extensions.Logging;

namespace DizQueDisse.Controllers
{
    public class HomeController : Controller
    {
        MyDbContext _context;
        AppService _business;
        //private readonly ILogger _logger;

        public HomeController(MyDbContext context, AppService business /*, ILogger<HomeController> logger*/)
        {
            _context = context;
            _business = business;
            //_logger = logger;
        }
        
        /// <summary>
        /// Returns Homepage View, containing the most recent approved tweets.
        /// </summary>
        [HttpGet("/")]
        public IActionResult Index()
        {
            return View(_business.GetSelectedTweetsByState(PublishingState.Approved));
        }

        /// <summary>
        /// Allows client to load more approved tweets, older than a given id.
        /// </summary>
        /// <param name="older">Tweets selected must be older than the tweet with this id.</param>
        /// <returns>Array of tweet ids and their respective states</returns>
        [HttpGet("/home/more/{older}")]
        public IActionResult More(string older)
        {
            ulong olderNumber = _business.ParseOlderThanThis(older);
            bool invalidOlder = older != null && olderNumber == ulong.MaxValue;
            if(invalidOlder)
                return BadRequest();

            return Ok(_business.GetSelectedTweetsByState(PublishingState.Approved, olderNumber));
        }

        /// <summary>
        /// Returns Error View.
        /// </summary>
        [HttpGet("/home/error")]
        public IActionResult Error()
        {
            return View();
        }

    }
}
