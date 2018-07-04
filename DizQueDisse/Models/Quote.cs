using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DizQueDisse.Models
{
    public class Quote
    {
        public Guid QuoteId { get; set; }
        public string Author { get; set; }
        public string Text { get; set; }
        public string Source { get; set; }
        public int? Likes { get; set; }
        public DateTime? TweetedAt { get; set; }
    }
}
