using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;


namespace FunctionAppXpto
{
    //Stop function app on portal before publishing, start it agin after publishing
    //Don't forget to allow app to access vault, both on app properties and vault properties
    //log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

    public static class FunctionQuote
    {
        [FunctionName("FunctionQuote")]
        public static void Run([TimerTrigger("0 30 7,11,20 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            try
            {
                MyHelpers.MakeRequest("quote");
            }catch(Exception e)
            {
                log.Info($"MyException: {e.ToString()}");
            }

        }

    }
}
