using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace FunctionAppXpto
{
    public static class FunctionWeather
    {
        [FunctionName("FunctionWeather")]
        public static void Run([TimerTrigger("0 0 6,18 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            try
            {
                MyHelpers.MakeRequest("weather");
            }
            catch (Exception e)
            {
                log.Info($"MyException: {e.ToString()}");
            }
        }
    }
}
