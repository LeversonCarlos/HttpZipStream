using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace System.IO.Compression
{
   public class HttpZipStream : IDisposable
   {


      string httpUrl { get; set; }
      HttpClient httpClient { get; set; }
      bool LeaveHttpClientOpen { get; set; }
      public HttpZipStream(string httpUrl) : this(httpUrl, new HttpClient()) { this.LeaveHttpClientOpen = true; }
      public HttpZipStream(string httpUrl, HttpClient httpClient)
      {
         this.httpUrl = httpUrl;
         this.httpClient = httpClient;
         this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
      }


      public long ContentLength { get; private set; } = -1;

      /// <summary>
      /// Manually setting the content length is only recommended if you truly know what your doing. This may increase loading time but could also invalidate the requests.
      /// </summary>
      public void SetContentLength(long value) { this.ContentLength = value; }

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


      HttpZipDirectory directoryData { get; set; }
      private async Task<bool> LocateDirectoryAsync()
      {
         try
         {

            // INITIALIZE
            this.directoryData = new HttpZipDirectory { Offset = -1 };
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
               while (pos >= 0)
               {

                  // FOUND CENTRAL DIRECTORY 
                  if (byteArray[pos + 0] == 0x50 &&
                      byteArray[pos + 1] == 0x4b &&
                      byteArray[pos + 2] == 0x05 &&
                      byteArray[pos + 3] == 0x06)
                  {
                     this.directoryData.Size = BitConverter.ToInt32(byteArray, pos + 12);
                     this.directoryData.Offset = BitConverter.ToInt32(byteArray, pos + 16);
                     this.directoryData.Entries = BitConverter.ToInt16(byteArray, pos + 10);
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


      public async Task<List<HttpZipEntry>> GetEntriesAsync()
      {
         try
         {
            // INITIALIZE
            var entryList = new List<HttpZipEntry>();
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
               var entry = new HttpZipEntry(entryIndex);
               // https://en.wikipedia.org/wiki/Zip_(file_format)#Local_file_header

               entry.Signature = BitConverter.ToInt32(byteArray, entriesOffset + 0); // 0x04034b50
               entry.VersionMadeBy = BitConverter.ToInt16(byteArray, entriesOffset + 4);
               entry.MinimumVersionNeededToExtract = BitConverter.ToInt16(byteArray, entriesOffset + 6);
               entry.GeneralPurposeBitFlag = BitConverter.ToInt16(byteArray, entriesOffset + 8);

               entry.CompressionMethod = BitConverter.ToInt16(byteArray, entriesOffset + 10);
               entry.FileLastModification = BitConverter.ToInt32(byteArray, entriesOffset + 12);
               entry.CRC32 = BitConverter.ToInt32(byteArray, entriesOffset + 16);
               entry.CompressedSize = BitConverter.ToInt32(byteArray, entriesOffset + 20);
               entry.UncompressedSize = BitConverter.ToInt32(byteArray, entriesOffset + 24);

               entry.FileNameLength = BitConverter.ToInt16(byteArray, entriesOffset + 28); // (n)
               entry.ExtraFieldLength = BitConverter.ToInt16(byteArray, entriesOffset + 30); // (m)
               entry.FileCommentLength = BitConverter.ToInt16(byteArray, entriesOffset + 32); // (k)

               entry.DiskNumberWhereFileStarts = BitConverter.ToInt16(byteArray, entriesOffset + 34);
               entry.InternalFileAttributes = BitConverter.ToInt16(byteArray, entriesOffset + 36);
               entry.ExternalFileAttributes = BitConverter.ToInt32(byteArray, entriesOffset + 38);
               entry.FileOffset = BitConverter.ToInt32(byteArray, entriesOffset + 42);               

               var fileNameStart = entriesOffset + 46;
               var fileNameBuffer = new byte[entry.FileNameLength];
               Array.Copy(byteArray, fileNameStart, fileNameBuffer, 0, entry.FileNameLength);
               entry.FileName = System.Text.Encoding.Default.GetString(fileNameBuffer);

               var extraFieldStart = fileNameStart + entry.FileNameLength;
               var extraFieldBuffer = new byte[entry.ExtraFieldLength];
               Array.Copy(byteArray, extraFieldStart, extraFieldBuffer, 0, entry.ExtraFieldLength);
               entry.ExtraField = System.Text.Encoding.Default.GetString(extraFieldBuffer);

               var fileCommentStart = extraFieldStart + entry.ExtraFieldLength;
               var fileCommentBuffer = new byte[entry.FileCommentLength];
               Array.Copy(byteArray, fileCommentStart, fileCommentBuffer, 0, entry.FileCommentLength);
               entry.FileComment = System.Text.Encoding.Default.GetString(fileCommentBuffer);
               
               entryList.Add(entry);
               entriesOffset = fileCommentStart + entry.FileCommentLength;
            }

            // RESULT
            return entryList;

         }
         catch (Exception) { throw; }
      }


      [Obsolete]
      public async Task ExtractAsync(List<HttpZipEntry> entryList, Action<MemoryStream> resultCallback)
      {
         try
         {
            foreach (var entry in entryList)
            { await this.ExtractAsync(entry, resultCallback); }
         }
         catch (Exception) { throw; }
      }

      public async Task ExtractAsync(HttpZipEntry entry, Action<MemoryStream> resultCallback)
      {
         try
         {
            var fileDataBuffer = await this.ExtractAsync(entry);
            var resultStream = new MemoryStream(fileDataBuffer);
            resultStream.Position = 0;
            resultCallback.Invoke(resultStream);
            return;
         }
         catch (Exception) { throw; }
      }

      public async Task<byte[]> ExtractAsync(HttpZipEntry entry)
      {
         try
         {

            // MAKE A HTTP CALL USING THE RANGE HEADER
            var fileHeaderLength = 30 + entry.FileNameLength + entry.ExtraFieldLength;
            var rangeStart = entry.FileOffset;
            var rangeFinish = entry.FileOffset + fileHeaderLength + entry.CompressedSize;
            this.httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(rangeStart, rangeFinish);
            var byteArray = await httpClient.GetByteArrayAsync(this.httpUrl);

            // LOCATE DATA BOUNDS
            // https://en.wikipedia.org/wiki/Zip_(file_format)#Local_file_header
            var fileSignature = BitConverter.ToInt32(byteArray, 0);
            var bitFlag = BitConverter.ToInt16(byteArray, 6);
            var compressionMethod = BitConverter.ToInt16(byteArray, 8);
            var crc = BitConverter.ToInt32(byteArray, 14);
            var compressedSize = BitConverter.ToInt32(byteArray, 18);
            var uncompressedSize = BitConverter.ToInt32(byteArray, 22);
            var fileNameLength = BitConverter.ToInt16(byteArray, 26); // (n)
            var extraFieldLength = BitConverter.ToInt16(byteArray, 28); // (m)
            var fileDataOffset = 30 + fileNameLength + extraFieldLength;
            var fileDataSize = entry.CompressedSize;

            // EXTRACT DATA BUFFER
            var fileDataBuffer = new byte[fileDataSize];
            Array.Copy(byteArray, fileDataOffset, fileDataBuffer, 0, fileDataSize);            
            Array.Clear(byteArray, 0, byteArray.Length);
            byteArray = null;

            /* STORED */
            if (entry.CompressionMethod == 0)
            { return fileDataBuffer; }

            /* DEFLATED */
            if (entry.CompressionMethod == 8)
            {           
               var deflatedArray = new byte[entry.UncompressedSize];
               using (var compressedStream = new MemoryStream(fileDataBuffer))
               {

                  using (var deflateStream = new System.IO.Compression.DeflateStream(compressedStream, CompressionMode.Decompress))
                  {
                     await deflateStream.ReadAsync(deflatedArray, 0, deflatedArray.Length);
                  }

                  /*
                  using (var deflatedStream = new MemoryStream())
                  {
                     var deflater = new System.IO.Compression.DeflateStream(compressedStream, CompressionMode.Decompress, true);                     

                     byte[] buffer = new byte[1024];
                     var bytesPending = entry.UncompressedSize;
                     while (bytesPending > 0)
                     {
                        var bytesRead = deflater.Read(buffer, 0, (int)Math.Min(bytesPending, buffer.Length));
                        deflatedStream.Write(buffer, 0, bytesRead);
                        bytesPending -= (uint)bytesRead;
                        if (bytesRead == 0) { break; }
                     }                    

                     deflatedArray = deflatedStream.ToArray();
                  }
                   */

               }
               return deflatedArray;
            }

            // NOT SUPPORTED COMPRESSION METHOD
            throw new NotSupportedException($"The compression method [{entry.CompressionMethod}] is not supported");
         }
         catch (Exception) { throw; }
      }


      public void Dispose()
      {
         if (!this.LeaveHttpClientOpen) { this.httpClient.Dispose(); this.httpClient = null; }
         this.directoryData = null;
         this.ContentLength = -1;
      }


   }
}