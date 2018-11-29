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
            int directoryOffset = -1;
            int directorySize = -1;
            short directoryEntries = -1;
            short locateTries = 1;
            long locateStart = contentLength - 22;
            long locateFinish = contentLength;
            var locateChunkSize = 256;
            while (directoryOffset == -1 && locateTries <= 4)
            {
               Log($"Trying to found central directory. {locateTries} try");

               // MAKE A HTTP CALL USING THE RANGE HEADER
               locateStart -= (locateChunkSize * locateTries);
               httpClient = new HttpClient();
               httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(locateStart, locateFinish);
               var byteArray = await httpClient.GetByteArrayAsync(httpUrl);

               // TRY TO LOCATE THE CENTRAL DIRECTORY STARTING DEFINED BY
               // 50 4B 05 06
               int pos = (byteArray.Length - 22);
               while (pos >= 0)
               {

                  if (byteArray[pos + 0] == 0x50 &&
                      byteArray[pos + 1] == 0x4b &&
                      byteArray[pos + 2] == 0x05 &&
                      byteArray[pos + 3] == 0x06)
                  {

                     // CENTRAL DIRECTORY DATA
                     directorySize = ByteArrayToInt(byteArray, pos + 12);
                     directoryOffset = ByteArrayToInt(byteArray, pos + 16);
                     directoryEntries = ByteArrayToShort(byteArray, pos + 10);
                     break; 

                  }
                  else { pos--; }
               }

               locateTries++;
            }
            if (directoryOffset == -1) { Log($"Hasnt found the central directory."); return; }


            // RETRIEVE CENTRAL DIRECTORY
            Log($"Found central directory with {directoryEntries} entries starting at {directoryOffset} with an size of {directorySize}.");
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(directoryOffset, (directoryOffset+directorySize));
            var dirByteArray = await httpClient.GetByteArrayAsync(httpUrl);


            // LOOP THROUGH ENTRIES
            var entriesOffset = 0;
            var entries = new string[directoryEntries];
            for (int entryIndex = 0; entryIndex < directoryEntries; entryIndex++)
            {
               Log($"Entry: {entryIndex}");

               // 0x02014b50 // ZipConstants.CENSIG
               var entrySignature = ByteArrayToInt(dirByteArray, entriesOffset + 0);
               var versionMadeBy = ByteArrayToShort(dirByteArray, entriesOffset + 4);
               var minimumVersionNeededToExtract = ByteArrayToShort(dirByteArray, entriesOffset + 6);
               var generalPurposeBitFlag = ByteArrayToShort(dirByteArray, entriesOffset + 8);

               var compressionMethod = ByteArrayToShort(dirByteArray, entriesOffset + 10);
               var fileLastModification = ByteArrayToInt(dirByteArray, entriesOffset + 12);
               // var fileLastModificationTime = ByteArrayToShort(dirByteArray, entriesOffset + 12);
               // var fileLastModificationDate = ByteArrayToShort(dirByteArray, entriesOffset + 14);
               var crc32 = ByteArrayToInt(dirByteArray, entriesOffset + 16);

               var compressedSize = ByteArrayToInt(dirByteArray, entriesOffset + 20);
               var uncompressedSize = ByteArrayToInt(dirByteArray, entriesOffset + 24);

               var fileNameLength = ByteArrayToShort(dirByteArray, entriesOffset + 28); // (n)
               var extraFieldLength = ByteArrayToShort(dirByteArray, entriesOffset + 30); // (m)
               var fileCommentLength = ByteArrayToShort(dirByteArray, entriesOffset + 32); // (k)

               var diskNumberWhereFileStarts = ByteArrayToShort(dirByteArray, entriesOffset + 34);
               var internalFileAttributes = ByteArrayToShort(dirByteArray, entriesOffset + 36);
               var externalFileAttributes = ByteArrayToShort(dirByteArray, entriesOffset + 38);

               var fileOffset = ByteArrayToInt(dirByteArray, entriesOffset + 42);

               var fileNameStart = entriesOffset + 46;
               var fileNameBuffer = new byte[fileNameLength];
               Array.Copy(dirByteArray, fileNameStart, fileNameBuffer, 0, fileNameLength);
               var fileName = System.Text.Encoding.Default.GetString(fileNameBuffer);

               var extraFieldStart = fileNameStart + fileNameLength;
               var extraFieldBuffer = new byte[extraFieldLength];
               Array.Copy(dirByteArray, extraFieldStart, extraFieldBuffer, 0, extraFieldLength);
               var extraField = System.Text.Encoding.Default.GetString(extraFieldBuffer);

               var fileCommentStart = extraFieldStart + extraFieldLength;
               var fileCommentBuffer = new byte[fileCommentLength];
               Array.Copy(dirByteArray, fileCommentStart, fileCommentBuffer, 0, fileCommentLength);
               var fileComment = System.Text.Encoding.Default.GetString(fileCommentBuffer);

               Log($" FileName: {fileName}");
               Log($" FileOffset: {fileOffset}");
               Log($" CompressedSize: {compressedSize}");
               Log($" UncompressedSize: {uncompressedSize}");

               entriesOffset = fileCommentStart + fileCommentLength;
            }

            Log($"Result");
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

		private static int ByteArrayToInt(byte [] byteArray, int pos)
		{
         return byteArray[pos + 0] | (byteArray[pos + 1] << 8) | (byteArray[pos + 2] << 16) | (byteArray[pos + 3] << 24);
      }

		private static short ByteArrayToShort(byte [] byteArray, int pos)
		{
         return (short)(byteArray[pos + 0] | (byteArray[pos + 1] << 8));
      }

      private static void Log(string value)
      { Console.WriteLine($"{DateTime.Now.ToString("mm:ss.fff")} - {value}"); }

   }
}