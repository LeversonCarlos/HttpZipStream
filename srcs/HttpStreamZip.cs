using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace System.IO.Compression
{
   public class HttpStreamZip: IDisposable
   {


      string httpUrl { get; set; }
      HttpClient httpClient { get; set; }
      bool LeaveHttpClientOpen { get; set; }
      public HttpStreamZip(string httpUrl) : this(httpUrl, new HttpClient()) { this.LeaveHttpClientOpen = true; }
      public HttpStreamZip(string httpUrl, HttpClient httpClient)
      {
         this.httpUrl = httpUrl;
         this.httpClient = httpClient;
         this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
      }


      public long ContentLength { get; private set; } = -1;
      // public void SetContentLength(long value) { this.ContentLength = value; }
      public async Task<long> GetContentLengthAsync()
      {
         try
         {
            if (this.ContentLength != -1) { return this.ContentLength; }
            using (var httpMessage = await this.httpClient.GetAsync(this.httpUrl, HttpCompletionOption.ResponseHeadersRead))
            { 
               if (!httpMessage.IsSuccessStatusCode) { return -1; }
               this.ContentLength = httpMessage.Content.Headers
                  .GetValues("Content-Length")
                  .Select(x => long.Parse(x))
                  .FirstOrDefault();
               return this.ContentLength;
            }
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
                     this.directoryData.Size = ByteArrayToInt(byteArray, pos + 12);
                     this.directoryData.Offset = ByteArrayToInt(byteArray, pos + 16);
                     this.directoryData.Entries = ByteArrayToShort(byteArray, pos + 10);
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


      List<HttpStreamZipEntry> EntryList { get; set; }
      public async Task<List<HttpStreamZipEntry>> GetEntries()
      {
         try
         {
            // INITIALIZE
            this.EntryList = new List<HttpStreamZipEntry>();
            if (await this.GetContentLengthAsync() == -1) { return null; }
            if (await this.LocateDirectoryAsync() == false) { return null; }

            // MAKE A HTTP CALL USING THE RANGE HEADER
            var rangeStart = this.directoryData.Offset;
            var rangeFinish = this.directoryData.Offset + this.directoryData.Size;
            this.httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(rangeStart, rangeFinish);
            var byteArray = await httpClient.GetByteArrayAsync(this.httpUrl);

            // LOOP THROUGH ENTRIES
            var entriesOffset = 0;
            for (int entryIndex = 0; entryIndex < this.directoryData.Entries; entryIndex++)
            {
               var entry = new HttpStreamZipEntry(entryIndex);
               // https://en.wikipedia.org/wiki/Zip_(file_format)#Local_file_header
               
               entry.Signature = ByteArrayToInt(byteArray, entriesOffset + 0); // 0x04034b50
               entry.VersionMadeBy = ByteArrayToShort(byteArray, entriesOffset + 4);
               entry.MinimumVersionNeededToExtract = ByteArrayToShort(byteArray, entriesOffset + 6);
               entry.GeneralPurposeBitFlag = ByteArrayToShort(byteArray, entriesOffset + 8);

               entry.CompressionMethod = ByteArrayToShort(byteArray, entriesOffset + 10);
               entry.FileLastModification = ByteArrayToInt(byteArray, entriesOffset + 12);
               entry.CRC32 = ByteArrayToInt(byteArray, entriesOffset + 16);
               entry.CompressedSize = ByteArrayToInt(byteArray, entriesOffset + 20);
               entry.UncompressedSize = ByteArrayToInt(byteArray, entriesOffset + 24);

               this.EntryList.Add(entry);
            }

            // RESULT
            return this.EntryList;

         }
         catch (Exception) { throw; }
      }


      private static int ByteArrayToInt(byte[] byteArray, int pos)
      {
         return byteArray[pos + 0] | (byteArray[pos + 1] << 8) | (byteArray[pos + 2] << 16) | (byteArray[pos + 3] << 24);
      }

      private static short ByteArrayToShort(byte[] byteArray, int pos)
      {
         return (short)(byteArray[pos + 0] | (byteArray[pos + 1] << 8));
      }


      public void Dispose()
      {
         if (!this.LeaveHttpClientOpen) { this.httpClient.Dispose(); this.httpClient = null; }
         this.directoryData = null;
         this.ContentLength = 0;
      }
      

   }
}