using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace System.IO.Compression
{
   public class HttpStreamZip
   {

      string httpUrl { get; set; }
      HttpClient httpClient { get; set; }
      public HttpStreamZip(string httpUrl) : this(httpUrl, new HttpClient()) { }
      public HttpStreamZip(string httpUrl, HttpClient httpClient)
      {
         this.httpUrl = httpUrl;
         this.httpClient = httpClient;
         this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
      }

      long ContentLength { get; set; }
      private async Task<bool> RefreshContentLengthAsync()
      {
         try
         {
            var httpMessage = await this.httpClient.GetAsync(this.httpUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!httpMessage.IsSuccessStatusCode) { return false; }
            this.ContentLength = httpMessage.Content.Headers
               .GetValues("Content-Length")
               .Select(x => long.Parse(x))
               .FirstOrDefault();
            return true;
         }
         catch (Exception) { throw; }
      }

      DirectoryData directoryData { get; set; }
      private async Task<bool> LocateDirectoryAsync()
      {
         try
         {

            // INITIALIZE
            this.directoryData = new DirectoryData { Offset = -1 };
            var secureMargin = 22;
            var chunkSize = 256;
            var rangeStart = this.ContentLength - secureMargin;
            var rangeFinish = this.ContentLength;

            // TRY TO FOUND THE CENTRAL DIRECTORY FOUR TIMES SLOWLY INCREASING THE CHUNK SIZE
            short tries = 1;
            while (this.directoryData.Offset == -1 && tries <= 4)
            {

               // MAKE A HTTP CALL USING THE RANGE HEADER
               rangeStart -= (chunkSize * tries);
               this.httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(rangeStart, rangeFinish);
               var byteArray = await httpClient.GetByteArrayAsync(this.httpUrl);

               // TRY TO LOCATE THE END OF CENTRAL DIRECTORY DEFINED BY
               // 50 4B 05 06
               // https://en.wikipedia.org/wiki/Zip_(file_format)#End_of_central_directory_record_(EOCD)
               int pos = (byteArray.Length - secureMargin);
               while (pos >= 0) { 

                  // FOUND CENTRAL DIRECTORY 
                  if (byteArray[pos + 0] == 0x50 &&
                      byteArray[pos + 1] == 0x4b &&
                      byteArray[pos + 2] == 0x05 &&
                      byteArray[pos + 3] == 0x06)
                  {
                     // this.directoryData.Size = ByteArrayToInt(byteArray, pos + 12);
                     // this.directoryData.Offset = ByteArrayToInt(byteArray, pos + 16);
                     // this.directoryData.Entries = ByteArrayToShort(byteArray, pos + 10);
                     return true;
                  }
                  else { pos--; }

               }

               tries++;
            }

            return false;
         }
         catch (Exception) { throw; }
      }

   }
}