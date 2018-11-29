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
            Log("Initialized");
            var httpUrl = "https://onedrive.live.com/download.aspx?cid=E6710A9CD086B950&authKey=%21AJeGdFhgxPdH8M4&resid=E6710A9CD086B950%2157128&ithint=%2Ecbz";


            // MAKE A HTTP CALL REQUIRING ONLY THE HEADERS SO IT SHOULD BE A FAST CALL
            var httpClient = GetHttpClient();
            var httpMessage = await httpClient.GetAsync(httpUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!httpMessage.IsSuccessStatusCode) { Log($"Received Http Status Code {httpMessage.StatusCode}."); return; }


            // READ THE CONTENT SIZE FROM THE RETURNED HEADERS
            var contentLength = httpMessage.Content.Headers
               .GetValues("Content-Length")
               .Select(x => long.Parse(x))
               .FirstOrDefault();
            Log($"The full content will have {contentLength} bytes");


            // TRY TO LOCATE THE CENTRAL DIRECTORY STARTING FROM THE END OF THE FILE AND GOING BACK BY 256 BYTES EACH TIME
            long directoryStart = -1;
            short locateTries = 1;
            long locateStart = contentLength - 22;
            long locateFinish = contentLength;
            var locateChunkSize = 256;
            while (directoryStart == -1 && locateTries <= 4)
            {
               Log($"Trying to found central directory. {locateTries} try");

               // MAKE A HTTP CALL USING THE RANGE HEADER
               locateStart -= (locateChunkSize * locateTries);
               httpClient = new HttpClient();
               httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(locateStart, locateFinish);
               var byteArray = await httpClient.GetByteArrayAsync(httpUrl);

               // TRY TO LOCATE THE CENTRAL DIRECTORY STARTING DEFINED BY
               // 50 4B 05 06
               long pos = (byteArray.Length - 22);
               while (pos >= 0)
               {

                  if (byteArray[pos + 0] == 0x50 &&
                      byteArray[pos + 1] == 0x4b &&
                      byteArray[pos + 2] == 0x05 &&
                      byteArray[pos + 3] == 0x06)
                  {
                     directoryStart = pos;
                     break; // FOUND IT
                  }
                  else { pos--; }
               }

               locateTries++;
            }

            if (directoryStart == -1) { Log($"Hasnt found the central directory."); return; }
            Log($"Found directory starting at {directoryStart}");



            return;
         }
         catch (Exception ex) { Log($"Exception: {ex.ToString()}"); }
         finally { Log("Finalized"); }
      }

      private static HttpClient GetHttpClient()
      {
         var httpClient = new HttpClient();
         httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
         return httpClient;
      }

      private static void Log(string value)
      { Console.WriteLine($"{DateTime.Now.ToString("mm:ss.fff")} - {value}"); }

   }
}