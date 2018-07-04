using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Services.AppAuthentication;
using System.Net.Http;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.KeyVault;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using AzureTimerFunction;

namespace FunctionAppXpto
{
    class LoginAzure
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public static class MyHelpers
    {

        public static void AuthenticateHttpClient(ref HttpClient client)
        {
            LoginAzure credentials = new LoginAzure { UserName = "manager", Password = MySecrets.Password };
            StringContent content = new StringContent(JsonConvert.SerializeObject(credentials), Encoding.UTF8, "application/json");

            var x = client.PostAsync("http://www.dizquedisse.site/account/loginazure", content).Result;

            x.Headers.TryGetValues("set-cookie", out IEnumerable<string> aux);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", (new List<string>(aux))[0]);
        }

        public static void MakeRequest(string type)
        {
            HttpClient client = new HttpClient();
            MyHelpers.AuthenticateHttpClient(ref client);
            var final = client.GetAsync($"http://www.dizquedisse.site/scheduler/{type}").Result;
            client.Dispose();
        }
    }
}
