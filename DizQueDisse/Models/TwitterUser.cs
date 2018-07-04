using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DizQueDisse.Models
{
    public class TwitterUser
    {
        [Key]
        public string id_str { get; set; }
        public string name { get; set; }
        public string screen_name { get; set; }
        public string description { get; set; }
        public int? followers_count { get; set; }
        public int? friends_count { get; set; }
        public int? listed_count { get; set; }
        public string created_at { get; set; }
        public int? favourites_count { get; set; }
        public bool? verified { get; set; }
        public int? statuses_count { get; set; }
        public string lang { get; set; }
        public string profile_image_url { get; set; }

        [JsonIgnore]
        public Contributor Contributor { get; set; }
    }
}
