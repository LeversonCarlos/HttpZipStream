using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace StreamZIP
{
   class Program
   {
      static void Main(string[] args)
      { Execute().GetAwaiter().GetResult(); }

      private static async Task Execute()
      {
         try
         {
            Console.WriteLine("Initialized");
            var httpUrl = "https://onedrive.live.com/download.aspx?cid=E6710A9CD086B950&authKey=%21AJeGdFhgxPdH8M4&resid=E6710A9CD086B950%2157128&ithint=%2Ecbz";

            // MAKE A HTTP CALL REQUIRING ONLY THE HEADERS SO IT SHOULD BE A FAST CALL
            var httpClient = GetHttpClient();
            var httpMessage = await httpClient.GetAsync(httpUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!httpMessage.IsSuccessStatusCode) { Console.WriteLine($"Http Message Status Code: {httpMessage.StatusCode}"); return; }

            // READ THE CONTENT SIZE FROM THE RETURNED HEADERS
            var contentLength = httpMessage.Content.Headers
               .GetValues("Content-Length")
               .Select(x => long.Parse(x))
               .FirstOrDefault();
            Console.WriteLine($"ContentLength: {contentLength}");

            return;
         }
         catch (Exception ex) { Console.WriteLine($"Exception: {ex.ToString()}"); }
         finally { Console.WriteLine("Finalized"); }
      }

      private static HttpClient GetHttpClient()
      {
         var httpClient = new HttpClient();
         httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
         return httpClient;
      }

   }
}