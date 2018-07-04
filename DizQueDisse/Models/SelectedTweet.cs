using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DizQueDisse.Models
{
    public enum PublishingState { Unchecked = 0, Approved = 1, Rejected = 2, StandBy = 3, Published = 4, Old = 5 }

    public class SelectedTweet
    {
        public Guid SelectedTweetId { get; set; }

        public string TweetId { get; set; }
        [ForeignKey("TweetId")]
        public Tweet Tweet { get; set; }

        public Guid ContributorId { get; set; }
        public Contributor Contributor { get; set; }

        public PublishingState CurrentState { get; set; }
        public DateTime? PublishingAt { get; set; }
        public DateTime? SelectedAt { get; set; }
        public DateTime? CheckedAt { get; set; }
    }
}
