using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DizQueDisse.Data;
using DizQueDisse.Models;

namespace DizQueDisse.Services
{
    /// <summary>
    /// This service selectes a quote from database, generates an image from it using an
    ///// external program, and tweets it through the TwitterService.
    /// </summary>
    public class QuoteService
    { 

        private readonly TwitterService _twitterService;
        private readonly IServiceProvider _services;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger _logger;

        public QuoteService(TwitterService twitterService, IServiceProvider services, IHostingEnvironment environment, ILogger<QuoteService> logger)
        {
            _logger = logger;
            _hostingEnvironment = environment;
            _services = services;
            _twitterService = twitterService;
        }


        /// <summary>
        /// Allows QuoteService to be invoked by SchedulerController
        /// (or alternatively by MainTimerService) and publish a tweet with a quote image.
        /// </summary>
        /// <returns>true if quote image was successfully generated and published; false otherwise;</returns>
        public async Task<bool> Act()
        {
            _logger.LogInformation("[ {0} ] QuoteService act started...", DateTime.UtcNow);

            // 1) get quote text and generate the respective image:
            Quote quote = GetNextQuote();
            byte[] img = GenerateQuoteMedia(quote);
            if (img == null || img.Length == 0)
                return false;

            // 2) if quote image was generated successfully, tweet it:
            if (!(await SendQuoteTweet(img)))
                return false;

            // 3) if quote image was tweeted with success, register tweet time for such quote:
            using (var serviceScope = _services.CreateScope())
            {
                var dbcontext = serviceScope.ServiceProvider.GetRequiredService<MyDbContext>();
                dbcontext.Entry(quote).Reload();
                quote.TweetedAt = DateTime.UtcNow;
                await dbcontext.SaveChangesAsync();
            }
            
            _logger.LogInformation("[ {0} ] QuoteService act ended successfully", DateTime.UtcNow);

            return true;
        }


        /// <summary>
        /// Gets the quote in the database that should be tweeted next 
        /// (Any quote that has not been tweeted yet).
        /// </summary>
        /// <returns>The quote object to be tweeted next; null, if no quote was found;</returns>
        private Quote GetNextQuote()
        {
            Quote q = null;
            using (var serviceScope = _services.CreateScope())
            {
                var dbcontext = serviceScope.ServiceProvider.GetRequiredService<MyDbContext>();
                q = dbcontext.Quotes
                    .OrderBy(x => x.TweetedAt ?? DateTime.MinValue)
                    .First();
            }
            return q;
        }


        /// <summary>
        /// Generates an image from a given quote object. It uses an external program to do so.
        /// </summary>
        /// <param name="quote">The quote object for which an image is going to generated.</param>
        /// <returns>An array with the bytes that make up the image that was generated.</returns>
        private byte[] GenerateQuoteMedia(Quote quote)
        {
            // 1) Set up external program (.net framework) that generates the image for each quote
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = _hostingEnvironment.ContentRootPath+ "/QuoteImageGenerator/Release/MyQuoteDrawingApp.exe";
            processInfo.RedirectStandardOutput = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardInput = true;
            Process process = Process.Start(processInfo);

            // 2) SEND ENCODING FIRST as required by external program
            // because p.StandardInput.Encoding != destination Console.InputEncoding 
            process.StandardInput.WriteLine(process.StandardInput.Encoding.CodePage);
            
            // 3) pass quote input through console:
            process.StandardInput.WriteLine(quote.Text);
            process.StandardInput.WriteLine(quote.Author);

            // 4) capture console output (the bytes corresponding to the final image):
            MemoryStream m = new MemoryStream();
            process.StandardOutput.BaseStream.CopyTo(m);
            byte[] image = m.GetBuffer();
            process.Dispose();

            return image;
        }


        /// <summary>
        /// Uses TwitterSevice to tweet an image with a quote.
        /// </summary>
        /// <param name="quoteMedia">The bytes that make up the image to be tweeted</param>
        /// <returns></returns>
        async Task<bool> SendQuoteTweet(byte[] quoteMedia)
        {
            // 1) upload image:
            string mediaId = await _twitterService.UploadMedia(quoteMedia);

            // 2) tweet it:
            return await _twitterService.SendTweet("", mediaId);
        }

    }
}
