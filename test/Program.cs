using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
            Log("Make initial call to the url");
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
            httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(directoryOffset, (directoryOffset + directorySize));
            var dirByteArray = await httpClient.GetByteArrayAsync(httpUrl);


            // LOOP THROUGH ENTRIES
            var entriesOffset = 0;
            var entries = new List<Entry>();
            for (int entryIndex = 0; entryIndex < directoryEntries; entryIndex++)
            {
               Log($"Entry: {entryIndex}");
               var entry = new Entry();
               entry.Index = entryIndex;

               // 0x02014b50 // ZipConstants.CENSIG
               entry.Signature = ByteArrayToInt(dirByteArray, entriesOffset + 0);
               entry.VersionMadeBy = ByteArrayToShort(dirByteArray, entriesOffset + 4);
               entry.MinimumVersionNeededToExtract = ByteArrayToShort(dirByteArray, entriesOffset + 6);
               entry.GeneralPurposeBitFlag = ByteArrayToShort(dirByteArray, entriesOffset + 8);

               entry.CompressionMethod = ByteArrayToShort(dirByteArray, entriesOffset + 10);
               entry.FileLastModification = ByteArrayToInt(dirByteArray, entriesOffset + 12);
               // var fileLastModificationTime = ByteArrayToShort(dirByteArray, entriesOffset + 12);
               // var fileLastModificationDate = ByteArrayToShort(dirByteArray, entriesOffset + 14);
               entry.CRC32 = ByteArrayToInt(dirByteArray, entriesOffset + 16);

               entry.CompressedSize = ByteArrayToInt(dirByteArray, entriesOffset + 20);
               entry.UncompressedSize = ByteArrayToInt(dirByteArray, entriesOffset + 24);

               entry.FileNameLength = ByteArrayToShort(dirByteArray, entriesOffset + 28); // (n)
               entry.ExtraFieldLength = ByteArrayToShort(dirByteArray, entriesOffset + 30); // (m)
               entry.FileCommentLength = ByteArrayToShort(dirByteArray, entriesOffset + 32); // (k)

               entry.DiskNumberWhereFileStarts = ByteArrayToShort(dirByteArray, entriesOffset + 34);
               entry.InternalFileAttributes = ByteArrayToShort(dirByteArray, entriesOffset + 36);
               entry.ExternalFileAttributes = ByteArrayToShort(dirByteArray, entriesOffset + 38);

               entry.FileOffset = ByteArrayToInt(dirByteArray, entriesOffset + 42);

               var fileNameStart = entriesOffset + 46;
               var fileNameBuffer = new byte[entry.FileNameLength];
               Array.Copy(dirByteArray, fileNameStart, fileNameBuffer, 0, entry.FileNameLength);
               entry.FileName = System.Text.Encoding.Default.GetString(fileNameBuffer);

               var extraFieldStart = fileNameStart + entry.FileNameLength;
               var extraFieldBuffer = new byte[entry.ExtraFieldLength];
               Array.Copy(dirByteArray, extraFieldStart, extraFieldBuffer, 0, entry.ExtraFieldLength);
               entry.ExtraField = System.Text.Encoding.Default.GetString(extraFieldBuffer);

               var fileCommentStart = extraFieldStart + entry.ExtraFieldLength;
               var fileCommentBuffer = new byte[entry.FileCommentLength];
               Array.Copy(dirByteArray, fileCommentStart, fileCommentBuffer, 0, entry.FileCommentLength);
               entry.FileComment = System.Text.Encoding.Default.GetString(fileCommentBuffer);

               entries.Add(entry);
               Log($" {entry.FileName}");
               entriesOffset = fileCommentStart + entry.FileCommentLength;
            }


            // ENTRIES FOUND
            Log($"Found {entries.Count} entries");
            var random = new Random(DateTime.Now.Second);
            var pageNumber = random.Next(0, entries.Count);
            Log($"pageNumber{pageNumber}");
            var page = entries[pageNumber];
            Log($"Page is {page.FileName} with {page.CompressedSize} bytes");

            // EXTRACT THE LARGER ONE
            Log($"ExtractingFile");
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(page.FileOffset, (page.FileOffset + page.CompressedSize));
            var fileByteArray = await httpClient.GetByteArrayAsync(httpUrl);
            Log($"ExtractedFile");

            var fileSignature = ByteArrayToInt(fileByteArray, 0);
            var fileNameLength = ByteArrayToShort(fileByteArray, 26); // (n)
            var extraFieldLength = ByteArrayToShort(fileByteArray, 28); // (m)

            var fileDataOffset = 30 + fileNameLength + extraFieldLength;
            var fileDataSize = page.CompressedSize - fileDataOffset;

            var fileDataBuffer = new byte[fileDataSize];
            Array.Copy(fileByteArray, fileDataOffset, fileDataBuffer, 0, fileDataSize);

            var filePathHandle = System.IO.Path.GetTempFileName();
            var fileHandle = $"{filePathHandle}.jpg";
            System.IO.File.Move(filePathHandle, fileHandle);
            Log($"fileHandle:{fileHandle}");

            using (var fileMemoryStream = new MemoryStream(fileDataBuffer))
            {
               fileMemoryStream.Position = 0;

               using (var fileStream = new FileStream(fileHandle, FileMode.Create))
               {
                  Log($"CompressionMethod:{page.CompressionMethod}");

                  /* STORED */
                  if (page.CompressionMethod == 0)
                  {
                     await fileMemoryStream.CopyToAsync(fileStream);
                  }

                  /* DEFLATED */
                  if (page.CompressionMethod == 8)
                  {
                     using (var deflateStream = new System.IO.Compression.DeflateStream(fileMemoryStream, CompressionMode.Decompress))
                     {
                        await deflateStream.CopyToAsync(fileStream);
                     }
                  }

               }

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
   public class Entry
   {
      public int Index { get; set; }
      public int Signature { get; set; }
      public short VersionMadeBy { get; set; }
      public short MinimumVersionNeededToExtract { get; set; }
      public short GeneralPurposeBitFlag { get; set; }
      public short CompressionMethod { get; set; }
      public int FileLastModification { get; set; }
      public int CRC32 { get; set; }
      public int CompressedSize { get; set; }
      public int UncompressedSize { get; set; }
      public short FileNameLength { get; set; }
      public short ExtraFieldLength { get; set; }
      public short FileCommentLength { get; set; }
      public short DiskNumberWhereFileStarts { get; set; }
      public short InternalFileAttributes { get; set; }
      public short ExternalFileAttributes { get; set; }
      public int FileOffset { get; set; }
      public string FileName { get; set; }
      public string ExtraField { get; set; }
      public string FileComment { get; set; }
   }
}