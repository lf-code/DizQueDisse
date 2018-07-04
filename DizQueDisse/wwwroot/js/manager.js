/**
 * Adds functionality that is specify of manager view to the 
 * core functionality set in site_core.js
 */
var AddManagerFunctionality = function () {

    // ----- DATA -----

    //Dictionary that stores the currently selected state for each loaded tweet
    //{tweetid : state} from api enum PublishingState { Unchecked=0, Approved=1, Rejected=2, StandBy=3, Published=4, Old=5}
    this.updatedTweetStates = {};

    //The current loadby type - 'state', 'screename' or 'tweetid' - and its actual value
    //this sets the criteria for loading tweets. One can load tweets with a given state or
    // of a given contributor or even load a single tweet indicating its id.
    this.currentTypeAndParameter = {type:"state",parameter:"0"}



    // ----- LOGIC -----

    /**
     * updates data in 'updatedTweetStates' in reaction to an event
     * (Tweet was created, a new state was selected, etc)
     * @param {any} tweetIdAndState The id and state of the tweet
     * @param {any} isNewTweet Whether the tweet was created or already exists
     */
    this.updateTweetStateLocal = function (tweetIdAndState, isNewTweet = false) {
        this.updatedTweetStates[tweetIdAndState.tweetId] = tweetIdAndState.publishingState;
        //If new tweet besides updating data, update also attributes in DOM (avoids adding logic in event reaction)
        if (isNewTweet) 
            this.DOM_InsertTweetIdInFormAttributes(tweetIdAndState);
        this.DOM_updateMenuTable();
    }


    /**
     * Updates tweet state by calling the respective http method and handling its response
     * and also loads different tweets for a new screenning for approval.
     */
    this.updateStates = function () {
        let data = [];
        for (let k of Object.keys(this.updatedTweetStates))
            data.push({ tweetId: k, newState: this.updatedTweetStates[k] });

        this.HTTP_updateTweets(data)
            .then(x => {
                this.DOM_showUpdateResultMessage(true);
                setTimeout(() => this.loadBy(),800);
            }).fail(x =>
                { this.DOM_showUpdateResultMessage(false); }
            );
    }


    /**
     * Sets a new load type and parameter, and loads tweets using this new setting.
     * @param {any} byType the new loadby type (state, screenname or tweetid)
     * @param {any} byParameter the actual value for that type 
     */
    this.newTypeAndParameter = function(byType, byParameter) {
        this.currentTypeAndParameter.type = byType;
        this.currentTypeAndParameter.parameter = byParameter;
        this.loadBy();
    }


    /**
     * Removes all currently loaded tweets, 
     * and loads new ones using the current type of load and the respective parameter.
     */
    this.loadBy = function() {
        this.updatedTweetStates = {};
        this.DOM_EmptyTweetContainer();
        this.loadMoreUrl = `/manager/more/${this.currentTypeAndParameter.type}/${this.currentTypeAndParameter.parameter}`;
        this.loadMoreTweets(false);
    }



    // ----- HTTP -----

    /**
     * Executes the http request to the server that updates the states of the tweets 
     * @param {any} updatedStatesArray array with the ids and new statesof the tweets to be updated
     */
    this.HTTP_updateTweets = function (updatedStatesArray) {
        return $.post({ url: "/manager/update", data: JSON.stringify(updatedStatesArray), contentType: 'application/json', });
    }



    // ----- EVENTS -----

    //whenever a form is changed ( a new state is selected), update data info accordingly
    $(".tweets-container").on("change", ".tweet-wrapper form input", (event) => {
        var tweetid = $(event.target).attr("id").substring($(event.target).attr("id").lastIndexOf("-") + 1);
        var newState = Number($(event.target).attr("value"));
        this.updateTweetStateLocal(new tweetIdAndStateVM(tweetid, newState));
    });


    //listens to TweetCreated event raised by core functionality, 
    //so that local information is updated accordingly.
    $(".tweets-container")[0].addEventListener("TweetCreated", (event) => {
        let tweetid = $(event.target).attr("data-tweetid");
        let state = $(event.target).attr("data-state");
        this.updateTweetStateLocal(new tweetIdAndStateVM(tweetid,state),true);
    }, false);


    // clicking on update button runs the update states action
    $("#update-button").on("click", (event) => {
        this.updateStates();
    })


    // clicking loadby button changes the current Type of Loading and the respective parameter. 
    $("#loadby-button").on("click", (event) => {
        let byType = $(".loadby label>input:checked")[0].value;
        let byParameter = $(".loadby label input:checked ~ span.secondary-input").find("input, select")[0].value;
        this.newTypeAndParameter(byType, byParameter);
    })



    // ----- DOM -----

    /**
     * Empties tweet container by removing all tweet-wrappers in it.
     */
    this.DOM_EmptyTweetContainer = function() {
        $(".tweets-container .tweet-wrapper").remove();
        this.DOM_updateMenuTable()
    }


    /**
     * Displays a message with the result of the update request to the server
     * @param {any} isSuccess the result of the request
     */
    this.DOM_showUpdateResultMessage = function(isSuccess) {
        let msg = isSuccess ? "update com sucesso" : "update falhou!";
        $("#menu-table+.messages").empty();
        $("#menu-table+.messages").append(`<div class='update-msg'>${msg}</div>`);
        $(".update-msg").fadeIn();
        setTimeout(() => $(".update-msg").fadeOut(), 3000);
    }

    /**
     * Updates the template Form with the actual id and state of the loaded tweet
     * @param {any} tweetIdAndState the id and state of the loaded tweet
     */
    this.DOM_InsertTweetIdInFormAttributes = function (tweetIdAndState) {
        // set id:
        let html = $(`.tweet-wrapper[data-tweetid='${tweetIdAndState.tweetId}'] form`).html();
        html = html.replace(/-0/g, `-${tweetIdAndState.tweetId}`);
        $(`.tweet-wrapper[data-tweetid='${tweetIdAndState.tweetId}'] form`).html(html);

        //set state and check it:
        let auxQuery = `.tweet-wrapper[data-tweetid='${tweetIdAndState.tweetId}'] form input[value=${tweetIdAndState.publishingState}]`;
        $(auxQuery).prop("checked", true);
    }


    /**
     * Update menu table with the correct count for each state
     */
    this.DOM_updateMenuTable = function() {
        $("#menu-table td[data-total]").text(Object.keys(this.updatedTweetStates).length);
        $("#menu-table td[data-state]").each((i, elem) => {
            let state = $(elem).attr("data-state");
            let count = Object.values(this.updatedTweetStates).reduce((accumulator, currentValue) => {
                if (Number(currentValue) === Number(state))
                    accumulator++;
                return accumulator;
            },0);
            $(elem).text(count);
        });
    }
        
}

var main = function () {
    //Setting customization variables of core functionality (site_core.js):
    this.changeBackground = false;
    this.loadMoreUrl = "/manager/more/state/0";

    //Add core functionality:
    AddCoreFunctionality.apply(this);

    //Add manager view functionality:
    AddManagerFunctionality.apply(this);

    //Core Functionality has the entry-point.
}

$(document).ready(
    main.bind({}) //binds an empty obj as 'this', to which all functionality will be added
);
