using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DizQueDisse.Data;
using DizQueDisse.Models;

namespace DizQueDisse.Services
{
    /// <summary>
    /// This service provides auxiliary methods such as methods for querying the database,
    /// that are likely to be used by many different controllers and actions.
    /// </summary>
    public class AppService
    {
        //this represents how many tweets (at max) should be considered for each request
        const int TWEETS_PER_REQUEST = 8;

        public IServiceProvider _services { get; }

        public AppService(IServiceProvider Services)
        {
            this._services = Services;
        }


        /// <summary>
        /// Returns the most recent (TWEETS_PER_REQUEST) tweets
        /// that match a given state and that are older than a given id
        /// </summary>
        /// <param name="currentState">tweet state to be matched</param>
        /// <param name="olderThanThisNumber">Results should include only tweets that have an id older than this</param>
        /// <returns>Array of tweet ids and their respective states</returns>
        public TweetIdAndStateVM[] GetSelectedTweetsByState(PublishingState currentState, ulong olderThanThisNumber = ulong.MaxValue)
        {
            //1) define the filter: filter by state and date
            Func<SelectedTweet, bool> filter = 
                (x) => x.CurrentState == currentState && ulong.Parse(x.TweetId) < olderThanThisNumber;
            
            //2) apply the filter and return results
            return GetSelectedTweets(filter);
        }


        /// <summary>
        /// Returns the most recent (TWEETS_PER_REQUEST) tweets
        /// that belong to a given contributor and taht are older than a given id
        /// </summary>
        /// <param name="screenName">Contributor's screen name</param>
        /// <param name="olderThanThisNumber">Results should include only tweets that have an id older than this</param>
        /// <returns>Array of tweet ids and their respective states</returns>
        public TweetIdAndStateVM[] GetSelectedTweetsByScreenName(string screenName, ulong olderThanThisNumber = ulong.MaxValue)
        {
            //1) define the filter: filter by Contributor's screen name and date
            Func<SelectedTweet, bool> filter = 
                (x) => x.Contributor.TwitterUser.screen_name == screenName && ulong.Parse(x.TweetId) < olderThanThisNumber;
            
            //2) apply the filter and return results
            return GetSelectedTweets(filter);
        }


        /// <summary>
        /// This aux method queries the database, applying a given filter and retrieves 
        /// the tweets ids, and their respective states, that match that filter
        /// </summary>
        /// <param name="filter">Filter to be applied when quering the database</param>
        /// <returns>>Array of tweet ids and their respective states</returns>
        private TweetIdAndStateVM[] GetSelectedTweets(Func<SelectedTweet,bool> filter)
        {
            TweetIdAndStateVM[] array = null;

            using (var serviceScope = _services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<MyDbContext>();
                array = context.SelectedTweets
                .Include(x=>x.Contributor)
                    .ThenInclude(t=>t.TwitterUser)
                .Include(s=>s.Tweet)
                .Where(filter)
                .OrderByDescending(x => ulong.Parse(x.TweetId))
                .Take(TWEETS_PER_REQUEST)
                .Select(s => new TweetIdAndStateVM(s)).ToArray();
            }
            return array;
        }


        /// <summary>
        /// Aux Method that parses a given string as a valid and existing tweet id,
        /// and returns it in the form a number (ulong) if it is valid,
        /// or returns ulong.maxValue if it not valid, so that no tweet id can be older than it.
        /// </summary>
        /// <param name="olderThanThis">Tweet Id</param>
        /// <returns>Tweet Id as ulong number if valid, ulong.MaxValue if string is not valid/non-existing</returns>
        public ulong ParseOlderThanThis(string olderThanThis)
        {
            //1) check validity parsing string as ulong
            ulong olderThanThisNumber = ulong.MaxValue;
            if(olderThanThis!=null)
                ulong.TryParse(olderThanThis, out olderThanThisNumber);
            
            //2) if valid, check existence: id must correspond to some existing selected tweet id
            if (olderThanThisNumber != ulong.MaxValue)
            {
                using (var serviceScope = _services.CreateScope())
                {
                    var context = serviceScope.ServiceProvider.GetRequiredService<MyDbContext>();
                    if (context.SelectedTweets.FirstOrDefault(x => x.TweetId == olderThanThis) == null)
                        olderThanThisNumber = ulong.MaxValue;
                }
            }
            return olderThanThisNumber;
        }

    }
}
