using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DizQueDisse.Models
{
    public class Contributor
    {
        public Guid ContributorId { get; set; }

        public string UserId { get; set; }
        public TwitterUser TwitterUser { get; set; }

        public DateTime? LastUpdate { get; set; }

        public List<SelectedTweet> SelectedTweets { get; set; }

    }
}
