using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DizQueDisse.Models
{
    public class TweetIdAndStateVM
    {
        public string TweetId { get; set; }
        public string PublishingState { get; set; }

        public TweetIdAndStateVM() { }
        public TweetIdAndStateVM(SelectedTweet st)
        {
            TweetId = st.TweetId;
            PublishingState = ((int)st.CurrentState).ToString(); ;
        }
    }
}
