using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using DizQueDisse.Models;
using Microsoft.Extensions.Logging;
using DizQueDisse.Secrets;

namespace DizQueDisse.Services { 

    /// <summary>
    /// This services allows the app to interact with TwitterApi.
    /// </summary>
    public class TwitterService
    {
        
        public HttpClient httpClient { get; set; }
        TwitterOAuthUtil OAuthUtil;

        string bearerToken;

        private readonly ILogger _logger;

        public TwitterService(ILogger<TwitterService> logger)
        {
            _logger = logger;
            httpClient = new HttpClient();
            OAuthUtil = new TwitterOAuthUtil();
        }

        #region AUTHENTICATION

        /// <summary>
        /// Generates AuthenticationHeaderValue to authorize requests to twitter API.
        /// (https://developer.twitter.com/en/docs/basics/authentication/guides/authorizing-a-request)
        /// </summary>
        /// <param name="requestParameters">The custom parameters of the request.</param>
        /// <param name="httpMethod">The http method of the request.</param>
        /// <param name="url">The url to which the request will be made.</param>
        /// <returns>AuthenticationHeaderValue with the parameters to authorize the request.</returns>
        public AuthenticationHeaderValue getAuthenticationHeaderValue(
            Dictionary<string, string> requestParameters, string httpMethod, string url)
        {
            //USER-AUTHENTICATION(https://developer.twitter.com/en/docs/basics/authentication/guides/authorizing-a-request)
            //CREATE SIGNATURE(https://developer.twitter.com/en/docs/basics/authentication/guides/creating-a-signature.html)

            // 1) Percent Encode all parameters:
            Dictionary<string, string> allEncodedParameters = new Dictionary<string, string>();
            foreach (var x in OAuthUtil.paramatersDic.Concat(requestParameters))
                allEncodedParameters.Add(PercentEncode(x.Key), PercentEncode(x.Value));

            // 2) get signature base string:
            string allParameterString = AggregateParameters(allEncodedParameters, "&");
            string signature_base_string = PercentEncode(httpMethod) + "&" + PercentEncode(url) + "&" + PercentEncode(allParameterString);

            // 3) get signing key: 
            string signingKey = PercentEncode(OAuthUtil.OAUTH_CONSUMER_SECRET) + "&" + PercentEncode(OAuthUtil.OAUTH_SECRET);

            // 4) calculate oauth_signature [HMACSHA1]:
            var aux = new System.Security.Cryptography.HMACSHA1(Encoding.ASCII.GetBytes(signingKey));
            var hashValue = aux.ComputeHash(Encoding.ASCII.GetBytes(signature_base_string));
            string oauth_signature = Convert.ToBase64String(hashValue);

            // 5) generate AuthenticationHeaderValue:
            Dictionary<string, string> oauthDic = new Dictionary<string, string>(allEncodedParameters); //include request parameters!
            oauthDic.Add(PercentEncode("oauth_signature"), PercentEncode(oauth_signature));
            string oauthFinalString = AggregateParameters(oauthDic, ", ", true);
            return new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", oauthFinalString);
        }


        /// <summary>
        /// Adds basic authentication header to httpClient. 
        /// Obtains the respective bearerToken first if it does not exist yet. 
        /// </summary>
        public async Task SetAuthorization()
        {
            if (bearerToken == null)
                await SetBearerToken();

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            httpClient.DefaultRequestHeaders.Add("User-Agent", OAuthUtil.UserAgent);
        }

        /// <summary>
        /// Obtains and stores the bearer token that will be used to authenticate requests. 
        /// https://developer.twitter.com/en/docs/basics/authentication/overview/application-only
        /// </summary>
        public async Task SetBearerToken()
        {
            // 1) set Headers:
            var byteArray = Encoding.ASCII.GetBytes($"{OAuthUtil.OAUTH_CONSUMER_KEY}:{OAuthUtil.OAUTH_CONSUMER_SECRET}");
            string encoded_value = Convert.ToBase64String(byteArray); //Base64 encoded bearer token credentials
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded_value);
            httpClient.DefaultRequestHeaders.Add("User-Agent", OAuthUtil.UserAgent);

            // 2) set Content and execute request:
            var content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await httpClient.PostAsync("https://api.twitter.com/oauth2/token", content);

            // 3) deserialize reponse object and obtain the bearer Token:
            string jsonString = await response.Content.ReadAsStringAsync();
            Dictionary<string, string> dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            bearerToken = dic["access_token"];
        }

        #endregion


        /// <summary>
        /// Uploads the media to be then used in tweets with media. 
        /// Necessary for tweets with images. 
        /// https://developer.twitter.com/en/docs/media/upload-media/uploading-media/media-best-practices
        /// </summary>
        /// <param name="media">the bytes of the media (image, for instance) to be upload</param>
        /// <returns>The 'media id string' that references the uploaded media; null, if upload fails;</returns>
        public async Task<string> UploadMedia(byte[] media)
        {
            // 1) Setup authorization:
            Dictionary<string, string> request_parameters = new Dictionary<string, string>();
            string httpMethod = "POST"; //uppercase
            string url = "https://upload.twitter.com/1.1/media/upload.json";

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = getAuthenticationHeaderValue(request_parameters, httpMethod, url);

            // 2) setup content:
            ByteArrayContent mediaContent = new ByteArrayContent(media);
            System.Net.Http.MultipartFormDataContent content = new MultipartFormDataContent();
            content.Add(mediaContent, "media");

            // 3) execute request and get media_id:
            var response = await httpClient.PostAsync(url, content);
            string jsonString = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
                return JsonConvert.DeserializeObject<MediaId>(jsonString).media_id_string;
            else
                return null;
        }

        /// <summary>
        /// Publishes a tweet through Twitter Api.
        /// </summary>
        /// <param name="text">The text of the tweet.</param>
        /// <param name="media_ids">The media id of any media to be included in the tweet.</param>
        /// <returns>true if the tweet was published with success; false, otherwise;</returns>
        public async Task<bool> SendTweet(string text, string media_ids = null)
        {
            // 1) Setup parameters:
            Dictionary<string, string> requestParameters = new Dictionary<string, string>();
            requestParameters.Add("status", text); //tweet text
            if (media_ids != null)
                requestParameters.Add("media_ids", media_ids); //image ids if any

            string url = "https://api.twitter.com/1.1/statuses/update.json";
            string httpMethod = "POST";

            // 2) Set user context and authorization
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = getAuthenticationHeaderValue(requestParameters, httpMethod, url);

            // 3) Create http content (percent encode parameters)
            Dictionary<string, string> encodedRequestParameters =
                new Dictionary<string, string>(requestParameters.Select(
                    kvp => new KeyValuePair<string, string>(PercentEncode(kvp.Key), PercentEncode(kvp.Value))));

            string c = AggregateParameters(encodedRequestParameters, "&");
            StringContent content = new StringContent(c, Encoding.UTF8, "application/x-www-form-urlencoded");

            // 4) Execute httpRequest
            HttpResponseMessage response = await httpClient.PostAsync(url, content);
            string contentString = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode) //log result
            {
                _logger.LogInformation("Tweet was successfully sent on {0} UTC.", DateTime.UtcNow);
            }
            else
            {
                _logger.LogCritical("Failed to send tweet on {0} UTC !!", DateTime.UtcNow);
                _logger.LogCritical("ContentString: {0}", contentString);
            }

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Returns an array of TwitterUser objects built from the 
        /// relevant information obtained from TwitterAPI, for twitter users
        /// with screen names provided.
        /// </summary>
        /// <param name="screenNames">Array with the screen names</param>
        /// <returns>Array of Twitter Users with the given screen names.</returns>
        public async Task<TwitterUser[]> GetMultipleUsersByScreenName(string[] screenNames) {
            return await GetMultipleUsers("screen_name", screenNames);
        }

        /// <summary>
        /// Returns an array of TwitterUser objects built from the 
        /// relevant information obtained from TwitterAPI, for twitter users
        /// with the given ids.
        /// </summary>
        /// <param name="screenNames">Array with the ids</param>
        /// <returns>Array of Twitter Users with the given ids.</returns>
        public async Task<TwitterUser[]> GetMultipleUsersById(string[] ids) {
            return await GetMultipleUsers("user_id", ids);
        }

        /// <summary>
        /// Obtains information about multiple users from the API,
        /// using a given filter (screen name or id), and builds TwitterUser objects
        /// through the deserialization of the response.
        /// </summary>
        /// <param name="dataType">The filter criteria ("user_id" or "screen_name")</param>
        /// <param name="data">Array with the actual ids or names of those users we want to get</param>
        /// <returns>Array of Twitter Users matching the filter data; null if unsuccessfull request;</returns>
        private async Task<TwitterUser[]> GetMultipleUsers(string dataType, string[] data)
        {
            // 1) build the string with the filter data:
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sb.Append(data[i] + (i == data.Length - 1 ? "" : ","));
            string dataString = sb.ToString();

            // 2) authenticate and execute request:
            await SetAuthorization();
            string url = $"https://api.twitter.com/1.1/users/lookup.json?{dataType}={dataString}";
            var response = await httpClient.GetAsync(url);
            string jsonString = await response.Content.ReadAsStringAsync();

            // 3) ifresponse is successful, construct TwitterUser through deserialization:
            if (response.IsSuccessStatusCode)
                return JsonConvert.DeserializeObject<List<TwitterUser>>(jsonString).ToArray();
            else
                return null;
        }

        /// <summary>
        /// Obtains a given number of the most recent tweets for a given user.
        /// </summary>
        /// <param name="screen_name">The screen name of the user</param>
        /// <param name="count">the number of tweets to get</param>
        /// <returns>An array of Tweet objects constructed from the information retrieved from the Api;
        /// null if request is unseccessful</returns>
        public async Task<Tweet[]> GetTweets(string screen_name, int count)
        {
           // 1) ser autheticatin and execute request:
            await SetAuthorization();
            string url = $"https://api.twitter.com/1.1/statuses/user_timeline.json?screen_name={screen_name}&count={count}";
            var response = await httpClient.GetAsync(url);
            string jsonString = response.Content.ReadAsStringAsync().Result;

            // 2) log result
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("GetTweets for {1} successfully sent on {0} UTC.", DateTime.UtcNow, screen_name);
            }
            else
            {
                _logger.LogCritical("Failed GetTweets for {1} on {0} UTC !!", DateTime.UtcNow, screen_name);
                _logger.LogCritical("ContentString: {0}", jsonString);
            }

            // 3) If successfull, construct array of Tweet objects through response deserialization: 
            if (response.IsSuccessStatusCode)
                return JsonConvert.DeserializeObject<List<Tweet>>(jsonString).ToArray();
            else
                return null;
        }

        /// <summary>
        /// Get single user with a given id.
        /// </summary>
        /// <param name="id_str">The id string of the user.</param>
        /// <returns>TwitterUser object for the requested user; null, if request is unsuccessful; </returns>
        public  async Task<TwitterUser> GetUserFromId(string id_str)
        {
            return await GetUser("user_id=" + id_str);
        }

        /// <summary>
        /// Get single user with a given id.
        /// </summary>
        /// <param name="id_str">The id string of the user.</param>
        /// <returns>TwitterUser object for the requested user; null, if request is unsuccessful; </returns>
        public async Task<TwitterUser> GetUserFromScreenName(string screen_mame)
        {
            return await GetUser("screen_name=" + screen_mame);
        }

        /// <summary>
        /// Actually executes the Get User request for a given query string.
        /// </summary>
        /// <param name="queryString">The query string used to select twitter user.</param>
        /// <returns>TwitterUser object for the requested user; null, if request is unsuccessful; </returns>
        private async Task<TwitterUser> GetUser(string queryString)
        {
            // 1) ser autheticatin and execute request:
            await SetAuthorization();
            string url = "https://api.twitter.com/1.1/users/show.json?" + queryString;
            var response = await httpClient.GetAsync(url);
            string jsonString = response.Content.ReadAsStringAsync().Result;

            // 2) If successfull, construct TwitterUser object through response deserialization: 
            if (response.IsSuccessStatusCode)
            {
                TwitterUser user = new TwitterUser();
                JsonConvert.PopulateObject(jsonString, user);
                return user;
            }
            else
                return null;

        }

        #region AUX_METHODS_AND_CLASSES

        /// <summary>
        /// Aux method that aggregates multiple parameters into a single string. 
        /// </summary>
        /// <param name="dic">KeyValuePairs with the name and value of the paraments.</param>
        /// <param name="linkString">The string that links the multiple parameters together.</param>
        /// <param name="addQuoteMarks">Whether parameter values should be in quotes or not.</param>
        /// <returns>The string with all the input agregated.</returns>
        private string AggregateParameters(Dictionary<string, string> dic, string linkString, bool addQuoteMarks = false)
        {
            return dic.OrderBy(p => p.Key).Select(p => p.Key + "=" + (addQuoteMarks ? $"\"{p.Value}\"" : p.Value))
                .Aggregate<string, string>("", (ac, ps) => ac + (ac == "" ? "" : linkString) + ps);
        }

        /// <summary>
        /// Implementation of percent-encoding as defined at
        /// https://developer.twitter.com/en/docs/basics/authentication/guides/percent-encoding-parameters.html
        /// </summary>
        /// <param name="src">The string to be encoded.</param>
        /// <returns>The percent-encoded string.</returns>
        public string PercentEncode(string src)
        {
            //string is UTF-16 -> this method only works if all chars are utf-8 compatible (if bytes are identical)!
            //http://www.utf8-chartable.de/

            //percent enconde method as described in 
            //https://developer.twitter.com/en/docs/basics/authentication/guides/percent-encoding-parameters.html
            //https://en.wikipedia.org/wiki/Percent-encoding

            string dst = "";

            char[] unreserved = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '-', '.', '_', '~' };

            foreach (var myChar in src)
            {
                // Unreserved ASCII are copied as they are:
                if (unreserved.Contains(myChar))
                    dst += myChar.ToString();
                else
                {
                    // Reserved or non-ASCII must be percent encode -> %[byte value in string]

                    // Twitter uses utf-8 to interpret non-ASCII chars
                    byte[] bytesOfMyChar = Encoding.UTF8.GetBytes(myChar.ToString());

                    foreach (byte b in bytesOfMyChar)
                    {
                        if (b == 0x00) //ignore 'empty' bytes
                            continue;

                        //Encode byte and add to final string:
                        dst += "%" + b.ToString("x2").ToUpper();
                    }
                }

            }
            return dst;
        }


        /// <summary>
        /// Auxiliary class for JSON deserialization.
        /// </summary>
        public class MediaId { public string media_id_string; }

        #endregion

    }
}
