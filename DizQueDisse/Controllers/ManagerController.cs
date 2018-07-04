using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DizQueDisse.Data;
using DizQueDisse.Models;
using DizQueDisse.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;


namespace DizQueDisse.Controllers
{
    [Authorize]
    public class ManagerController : Controller
    {
        MyDbContext _context;
        AppService _business;

        public ManagerController(MyDbContext context, AppService business)
        {
            _context = context;
            _business = business;
            //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1#designing-services-for-dependency-injection
        }

        /// <summary>
        /// Returns Manager view, containing a selection of tweets to be screened,
        /// and a form for each tweet that allows to change its publishing state.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/manager")]
        public IActionResult Index()
        {
            // Possible publishing states to be included in the form:
            //(defined here, to avoid inconsistencies)
            PublishingState[] checkerOptions =
                { PublishingState.Unchecked , PublishingState.Approved, PublishingState.Rejected, PublishingState.StandBy};
            Dictionary<string, int> dic = new Dictionary<string, int>();
            foreach (PublishingState s in checkerOptions)
                dic.Add(s.ToString(), (int)s);

            ViewData["checkerOptions"] = dic;
            return View("Manager", _business.GetSelectedTweetsByState(PublishingState.Unchecked));
        }

        /// <summary>
        /// Allows manager to load tweets to be screened, selecting by publishing state
        /// and including only those older than a given tweet. 
        /// </summary>
        /// <param name="state">The publishing state of the tweets to be loaded.</param>
        /// <param name="older">Include only tweets older that the tweet with this id.</param>
        /// <returns>Array of tweet ids and their respective states</returns>
        [HttpGet("/manager/more/state/{state}/{older?}")]
        public IActionResult MoreByState(string state, string older = null)
        {
            ulong olderNumber = _business.ParseOlderThanThis(older);
            bool invalidOlder = older != null && olderNumber == ulong.MaxValue;
            bool invalidState = !Enum.TryParse(typeof(PublishingState), state, out object pstate);
            if (invalidOlder || invalidState)
                return BadRequest();

            return Ok(_business.GetSelectedTweetsByState((PublishingState)pstate, olderNumber));
        }

        /// <summary>
        /// Allows manager to load tweets to be screened, selecting by contributor's 
        /// screen name and including only those older than a given tweet. 
        /// </summary>
        /// <param name="screenName">Include only tweets belonging to the contributor with this screen name.</param>
        /// <param name="older">Include only tweets older that the tweet with this id.</param>
        /// <returns>Array of tweet ids and their respective states</returns>
        [HttpGet("/manager/more/screenname/{screenName}/{older?}")]
        public IActionResult MoreByScreenName(string screenName,string older = null)
        {
            ulong olderNumber = _business.ParseOlderThanThis(older);
            bool invalidOlder = older != null && olderNumber == ulong.MaxValue;
            bool invalidUser = screenName == null || screenName == "" || screenName.Length > 50 || 
                _context.Contributors.Include(c => c.TwitterUser).FirstOrDefault(x => x.TwitterUser.screen_name == screenName) == null;
            if (invalidOlder || invalidUser)
                return BadRequest();

            return Ok(_business.GetSelectedTweetsByScreenName(screenName, olderNumber));
        }

        /// <summary>
        /// Allows manager to load a tweet by id.
        /// </summary>
        /// <param name="id">The id of the tweet to be loaded.</param>
        /// <returns>Array containing the tweet id and its state</returns>
        [HttpGet("/manager/more/tweetid/{id}")]
        public IActionResult MoreById( string id)
        {
            bool invalidId = id == null || id == "";
            SelectedTweet st = _context.SelectedTweets.FirstOrDefault(x => x.TweetId == id);
            if (invalidId || st == null)
                return BadRequest();

            return Ok(new TweetIdAndStateVM[]{ new TweetIdAndStateVM(st)});
        }

        /// <summary>
        /// Enables the manager to update the state of multiple tweets
        /// </summary>
        /// <param name="newStates">the array of tweets ids and their respective new states.</param>
        [HttpPost("/manager/update")]
        public IActionResult Update([FromBody] UpdatedStateBM[] newStates)
        {
            if (newStates == null || newStates.Length == 0)
                return BadRequest();

            foreach (UpdatedStateBM u in newStates)
            {
                SelectedTweet s = _context.SelectedTweets.FirstOrDefault(x => x.TweetId == u.TweetId);
                if (s != null)
                {
                    s.CurrentState = u.NewState;
                    s.CheckedAt = DateTime.UtcNow;
                }
                _context.SaveChanges();
            }
            return Ok();
        }
    }
}
