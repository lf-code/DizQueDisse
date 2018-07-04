//VARIABLES FOR CUSTOMIZATION OF CORE FUCTIONALITY: 
//this.changeBackground; //true if background image should change every few seconds.
//this.loadMoreUrl; //url of server's endpoint for loading more tweets.

/**
 * Adds core functionality, to be shared by 'home' view and 'manager' view.
 */
var AddCoreFunctionality = function (cv) {

    /**
    * Twitter Widget:
    */
    window.twttr = (function (d, s, id) {
        var js, fjs = d.getElementsByTagName(s)[0],
            t = window.twttr || {};
        if (d.getElementById(id)) return t;
        js = d.createElement(s);
        js.id = id;
        js.src = "https://platform.twitter.com/widgets.js";
        fjs.parentNode.insertBefore(js, fjs);

        t._e = [];
        t.ready = function (f) {
            t._e.push(f);
        };

        return t;
    }(document, "script", "twitter-wjs"));



    // ----- DATA -----

    //Ids of the tweets that hava been loaded:
    this.tweetsIds = [];

    //Correct state of loading action, and its possible states:
    this.loadState = { current: "wait", options: ["wait", "more", "full"] };



    // ----- LOGIC -----

    /**
    * Entry method
    */
    this.start = function () {

        // 1) get and store tweet ids embedded in html
        $(".tweet-wrapper").each((index, elem) => {
            let tweetId = $(elem).attr("data-tweetId");
            this.tweetsIds.push(tweetId);
        })

        // 2) fill in the empty tweet wrappers with the corresponding TwitterWidgetObject 
        this.DOM_CreateTweetsInEmptyTweetWrappers();

        // HACK: change load state after 3 seconds (change this hack)
        setTimeout(() => this.changeLoadState("more"), 3000);
    }


    /**
    * Changes the state of the loading action.
    * @param {any} targetState a possible state for the loading action.
    */
    this.changeLoadState = function (targetState) {
        if (this.loadState.options.some(x => x === targetState)) {
            this.loadState.current = targetState;
            this.DOM_UpdateDueNewLoadState(this.loadState.current);
        }
        else { console.log("INVALID STATE: " + targetState + " " + this.loadState.options); }
    }


    /**
    * Loads more tweets from server
    * @param {any} withOlderThan
    */
    this.loadMoreTweets = function (withOlderThan = true) {

        // 1) Add the id of the oldest loaded tweet, if required 
        let oldestTweet = null;
        if (withOlderThan) {
            oldestTweet = (this.tweetsIds.sort((a, b) => { return Number(a) - Number(b); }))[0];
        }

        // 2) call http method, then process the response
        this.changeLoadState("wait");
        this.HTTP_loadMoreTweets(oldestTweet).then(data => {
            if (data.length === 0) {
                this.changeLoadState("full");
            } else {
                //create a wrapper for each new tweet loaded:
                for (var t of data) {
                    this.DOM_addTweetWrapper(new tweetIdAndStateVM(t.tweetId, t.publishingState));
                    this.tweetsIds.push(t.tweetId);
                }
                //fill in the wrappers with the respective TwitterWidgetObject:
                this.DOM_CreateTweetsInEmptyTweetWrappers();
                this.changeLoadState("more");
            }
        });
    };



    // ----- DOM -----

    /**
    * Disables buttons that call http functions, while an http
    * request is being executed
    */
    this.DOM_DisableHttpButtonsDuringRequest = function () {
        $("button[httpbutton]").attr("disabled", true);
    }


    /**
    * Enables back the buttons once the http request is complete.
    */
    this.DOM_EnableHttpButtonsAfterRequest = function () {
        $("button[httpbutton]").attr("disabled", false);
    }


    /**
    * Adds an empty tweetWrapper div for a given tweet.
    * @param {any} tweetIdAndStateVM object with tweet's id and state
    */
    this.DOM_addTweetWrapper = function (tweetIdAndStateVM) {
        let el = $("#tweet-wrapper-template").first().clone();
        el.addClass("tweet-wrapper");
        el.removeAttr("id");
        el.attr("data-tweetid", tweetIdAndStateVM.tweetId);
        el.attr("data-state", tweetIdAndStateVM.publishingState);
        el.find(".mytweet").attr("data-tweetid", tweetIdAndStateVM.tweetId);
        $(".tweets-container .loadmore").before(el);
    }


    /**
    * Creates the tweet widget in each empty tweetWrapper.
    * Dispach an event when each TwitterWidgetObject is complete.
    */
    this.DOM_CreateTweetsInEmptyTweetWrappers = function () {
        $(".tweet-wrapper .mytweet:empty").each((index, elem) => {
            let tweetId = $(elem).attr("data-tweetid");
            let el = $(elem)[0];
            twttr.widgets.createTweet(tweetId, el, {}).then(
                x => {
                    this.DOM_showTweet($(x).attr("data-tweet-id"))
                        ; $(x)[0].parentNode.parentNode.dispatchEvent(event);
                }
            );
        });
    }


    /**
    * Updates 'loadmore' button according to the state of loading:
    * 'wait' - it is loading; 
    * 'more' - loaded, there are more tweets to load;
    * 'full' - loaded, there are no more tweets to load;
    * @param {any} newLoadState
    */
    this.DOM_UpdateDueNewLoadState = function (newLoadState) {
        switch (newLoadState) {
            case "wait": { //display spinner
                $(".loadmore button").hide();
                $(".loadmore .wait").show();
            } break;
            case "more": { //display button
                $(".loadmore button").show();
                $(".loadmore .wait").hide();
            } break;
            case "full": { //display nothing
                $(".loadmore button").hide();
                $(".loadmore .wait").hide();
            } break;
        }
    }


    /**
    * Makes a given tweet visible by removing 'donotshow' class
    * @param {any} id tweet id
    */
    this.DOM_showTweet = function (id) {
        $(".tweet-wrapper[data-tweetid=" + id + "]").removeClass("donotshow");
    }


    /**
    * Sets a timer to change the background image every 20 seconds
    */
    this.DOM_changeBackgroundImage = function () {

        var DOM_images = ["obama", "microphone", "advice", "application", "battery", "chatting"];
        var DOM_currentIndex = 0;

        var DOM_nextIndex = function () {
            return DOM_currentIndex === DOM_images.length - 1 ? 0 : DOM_currentIndex + 1;
        };

        var DOM_loadNextImage = function () {
            $("<img/>").attr("src", "images/" + DOM_images[DOM_nextIndex()] + ".jpg");
        };

        //load next image before setting the timer:
        DOM_loadNextImage()

        setInterval(() => {
            DOM_currentIndex = DOM_nextIndex();
            $(".top-container").css("background-image", "url('images/" + DOM_images[DOM_currentIndex] + ".jpg')")
            DOM_loadNextImage();
        }, 20000)
    }



    // ----- HTTP -----

    //Disables all buttons (that raise http actions) while it is executing a request
    $(document).ajaxStart(() => this.DOM_DisableHttpButtonsDuringRequest());
    $(document).ajaxStop( () => this.DOM_EnableHttpButtonsAfterRequest());

    /**
    * executes http request to get more tweets.
    * loaded tweets must be older than the tweet with id provided.
    * @param {any} id tweet id
    */
    this.HTTP_loadMoreTweets = function (id) {
        return $.get(`${this.loadMoreUrl}${id === null ? '' : '/' + id}`);
    }



    // ----- EVENTS -----

    //load more tweets from the API when 'loadmore' button is clicked
    $(".loadmore button").on("click", () => this.loadMoreTweets());
        

    // Event to be raised when a new tweet is created:
    window["event"] = new CustomEvent('TweetCreated', { bubbles: true });



    // ----- ENTRY-POINT -----

    //Changing background image is optional
    if (this.changeBackground)
        this.DOM_changeBackgroundImage();

    //when twitter widget is ready call start function with 'this'
    twttr.ready(this.start.bind(this));

};


//ViewModel Objects:
var tweetIdAndStateVM = function (id = null, state = null) {
    this.tweetId = id;
    this.publishingState = state;
}
