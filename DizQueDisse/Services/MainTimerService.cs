using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using DizQueDisse.Models;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DizQueDisse.Data;
using Microsoft.Extensions.Logging;

namespace DizQueDisse.Services
{
    /// <summary>
    /// The service runs in background and every few (5) minutes checks one Contributor 
    /// and gets his 5 best tweets (those most liked and retweeted)
    /// among his last 50 tweets, and saves them as SelectedTweets in the database,
    /// to be later screened by the manager for potential publication on website.
    /// </summary>
    public class MainTimerService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly TwitterService _twitterService;

        private Timer MainTimer;

        public MainTimerService(IServiceProvider services, AppService appService, ILogger<MainTimerService> logger, TwitterService twitterService)
        {
            _services = services;
            _logger = logger;
            _twitterService = twitterService;

            AddSeedContributors();
        }

        #region Timer

        /// <summary>
        /// Sets up the main timer that calls 'Check' every 5 minutes.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("[ {0} ] MainTimer is starting...", DateTime.UtcNow);

            MainTimer = new Timer(Check, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            return Task.CompletedTask;
        }

        /// <summary>
        /// //Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("[ {0} ] MainTimer is stopping!",DateTime.UtcNow);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes MainTimer when the service is itself disposed.
        /// </summary>
        public void Dispose()
        {
            MainTimer.Dispose();
        }

        /// <summary>
        /// The action to be raised by main timer. It simply checks the next Contributor.
        /// </summary>
        /// <param name="obj">Required by Timer constructor, ignore.</param>
        public void Check(object obj)
        {
            CheckNextContributor();
        }

        #endregion


        #region Logic

        /// <summary>
        /// Adds seed Contributors to the database, if there are none yet.
        /// </summary>
        public void AddSeedContributors()
        {
            // 1) check if there already are contributors, if so, do nothing:
            int countContributors = 0;
            using (var serviceScope = _services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<MyDbContext>();
                countContributors = context.Contributors.Count();
            }
            if (countContributors > 0)
                return;

            // 2) if there are no contributors, add some based on the list:
            string[] seedNames = { "DiogoBataguas", "DiogoBeja‏", "bifeahcasa‏", "danielcarapeto‏", "TiagoCCaetano‏", "ogajo_‏", "FernandaFreitas‏", "OPauloAlmeida‏", "JoannaAzevedo‏", "RitaDaNova‏", "raminhoseffect‏", "catarinamatos‏", "hugosousacomedy‏", "guilhermefon‏", "RuiHCruz‏", "cvazmarques‏", "RuiSinelCordes‏", "pmnribeiro‏", "fhf‏", "Manzarra‏", "SalvasMartinha‏", "O_Unas‏", "Corpodormente‏", "fernandoalvim‏", "JoseDePina‏" };

            // 3) Try and get users from get users from twitter api :
            TwitterUser[] users = null;
            try
            {
                users = _twitterService.GetMultipleUsersByScreenName(seedNames).Result;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            //4) if users are successfully retrieved from Api, add them to database:
            if (users != null)
                AddTweetUsersToDB(users);
        }

        /// <summary>
        /// Adds Twitter Users and the respective Contributors to the database,
        /// skipping those who already exist.
        /// </summary>
        /// <param name="users"></param>
        private void AddTweetUsersToDB(TwitterUser[] users)
        {
            using (var serviceScope = _services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<MyDbContext>();

                foreach (TwitterUser u in users)
                {
                    //skip those which already exist:
                    if (context.TwitterUsers.Find(u.id_str) != null)
                        continue;

                    context.TwitterUsers.Add(u);
                    Contributor c = new Contributor();
                    c.TwitterUser = u;
                    c.LastUpdate = DateTime.UtcNow;
                    context.Contributors.Add(c);
                }

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Gets the contributor with the oldest update date, and checkes its tweets 
        /// </summary>
        /// <returns>The Contributor that was checked.</returns>
        private Contributor CheckNextContributor()
        {
            Contributor c = null;
            using (var serviceScope = _services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<MyDbContext>();
                c = context.Contributors.Include(x => x.TwitterUser).OrderBy(x => x.LastUpdate).First();
                CheckTweets(c).Wait();
            }
            return c;
        }

        /// <summary>
        /// Checks the tweets for a given contributor, that is, gets his last 50 tweets,
        /// selects the five best ( those with most likes/retweets) and if they are not in the
        /// database, add them, if they were already added, update them.
        /// </summary>
        /// <param name="contributor">The Contributor whose tweets will be checked.</param>
        public async Task CheckTweets(Contributor contributor)
        {
            const int LAST_N_TWEETS = 50;
            Tweet[] tweets = null;
            
            // 1) Try and get the last 50 tweets:
            string screen_name = contributor?.TwitterUser?.screen_name;
            if (screen_name == null)
                throw new ArgumentNullException();
            try
            {
                tweets = await _twitterService.GetTweets(screen_name, LAST_N_TWEETS);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            
            using (var serviceScope = _services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<MyDbContext>();
                Contributor c = context.Contributors.Single(p => p.ContributorId == contributor.ContributorId);

                //2) always update contributors lastChecked date, even if one fails to get tweets:
                c.LastUpdate = DateTime.UtcNow;

                if (tweets != null && tweets.Length > 0)
                {
                    const int HOW_MANY_BEST_TWEETS = 5;

                    //3) if tweets were successfully retrieved, select the five best ones:
                    Tweet[] best = tweets
                        .OrderByDescending(t => t.favorite_count + t.retweet_count)
                        .Where(t => t.favorite_count + t.retweet_count > 0)
                        .Take(HOW_MANY_BEST_TWEETS).ToArray();

                    context.Entry(c).Collection(x => x.SelectedTweets).Load();
                    string[] existingIds = c.SelectedTweets.Select(t => t.TweetId).ToArray();

                    // 4) Add selected tweets to database, as SelectedTweets:
                    foreach (var t in best)
                    {
                        if (existingIds.Contains(t.id_str))
                        {
                            //Update Tweet's selectedAt date, if it already exists and it is still unchecked:
                            SelectedTweet selectedTweetedFromDb = c.SelectedTweets.First(x => x.TweetId == t.id_str);
                            if (selectedTweetedFromDb.CurrentState == PublishingState.Unchecked)
                            {
                                selectedTweetedFromDb.SelectedAt = DateTime.UtcNow;
                                context.Update(t);
                            }
                        }
                        else
                        {
                            //Add if it does not exist yet:
                            SelectedTweet newSelectedTweet = new SelectedTweet();
                            newSelectedTweet.Tweet = t;
                            newSelectedTweet.SelectedAt = DateTime.UtcNow;
                            c.SelectedTweets.Add(newSelectedTweet);
                        }
                    }
                }

                await context.SaveChangesAsync();
            }

        }

        #endregion

    }
}