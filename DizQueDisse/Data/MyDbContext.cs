using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DizQueDisse.Models;

namespace DizQueDisse.Data
{

    public class MyDbContext : IdentityDbContext<MyUser>
    {
        //App
        public DbSet<Tweet> Tweets { get; set; }
        public DbSet<TwitterUser> TwitterUsers { get; set; }
        public DbSet<Contributor> Contributors { get; set; }
        public DbSet<SelectedTweet> SelectedTweets { get; set; }

        //Quotes
        public DbSet<Quote> Quotes { get; set; }

        public MyDbContext(DbContextOptions<MyDbContext> options)
            : base(options)
        {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //App
            modelBuilder.Entity<TwitterUser>()
            .HasOne(p => p.Contributor)
            .WithOne(i => i.TwitterUser)
            .HasForeignKey<Contributor>(b => b.UserId).IsRequired(true);

            modelBuilder.Entity<Tweet>()
            .HasOne(p => p.SelectedTweet)
            .WithOne(i => i.Tweet)
            .HasForeignKey<SelectedTweet>(b => b.TweetId).IsRequired(true);

            modelBuilder.Entity<SelectedTweet>().Property(x => x.CurrentState).HasDefaultValue(PublishingState.Unchecked);

            //Quotes
            modelBuilder.Entity<Quote>().Property(x => x.Source).IsRequired(false);
            modelBuilder.Entity<Quote>().Property(x => x.Likes).IsRequired(false);
            modelBuilder.Entity<Quote>().Property(x => x.TweetedAt).IsRequired(false);
            modelBuilder.Entity<Quote>().Property(x => x.Author).IsRequired(true);
            modelBuilder.Entity<Quote>().Property(x => x.Text).IsRequired(true);

        }
    }
}
