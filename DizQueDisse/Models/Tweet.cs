using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DizQueDisse.Models
{
    public class Tweet
    {
        [Key]
        public string id_str { get; set; }

        public string created_at { get; set; }
        public string text { get; set; }
        public int? quote_count { get; set; }
        public int? reply_count { get; set; }
        public int? retweet_count { get; set; }
        public int? favorite_count { get; set; }

        [JsonIgnore]
        public SelectedTweet SelectedTweet { get; set; }

    }
}
