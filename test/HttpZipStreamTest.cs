using System;
using System.Linq;
using Xunit;

namespace System.IO.Compression
{
   public class HttpZipStreamTest
   {
      string httpUrl = "https://onedrive.live.com/download.aspx?cid=ADED24162E9A6538&authKey=%21ABqNji2NaV0MT58&resid=ADED24162E9A6538%21107&ithint=%2Ecbz";


      [Fact]
      public async void ExampleStream_ContentLength_MustBe_9702kbytes()
      {
         using (var streamZip = new HttpZipStream(httpUrl))
         {
            var contentLength = await streamZip.GetContentLengthAsync();
            Assert.Equal(9935427, contentLength);
         }
      }


      [Fact]
      public async void ExampleStream_Entries_MustHave_36items()
      { 
         using (var streamZip = new HttpZipStream(httpUrl))
         {
            var contentLength = await streamZip.GetContentLengthAsync();
            var entryList = await streamZip.GetEntriesAsync();
            Assert.Equal(36, entryList.Count);
         }
      }


      [Fact]
      public async void ExampleStream_LargerEntry_MustBe_0001_With_347kbytes()
      { 
         using (var streamZip = new HttpZipStream(httpUrl))
         {
            var contentLength = await streamZip.GetContentLengthAsync();
            var entryList = await streamZip.GetEntriesAsync();
            var largerEntry = entryList
               .OrderByDescending(x => x.CompressedSize)
               .Take(1)
               .FirstOrDefault();
            Assert.Equal("Blue Beetle [1967] #01 - 0001.jpg", largerEntry.FileName);
            Assert.Equal(355736, largerEntry.CompressedSize);
         }
      }


      [Fact]
      public async void ExampleStream_SmallerEntryExtraction_MustResult_MemoryStream_With_227kbytes()
      {
         using (var streamZip = new HttpZipStream(httpUrl))
         {
            var contentLength = await streamZip.GetContentLengthAsync();
            var entryList = await streamZip.GetEntriesAsync();
            var smallerEntry = entryList
               .OrderBy(x => x.CompressedSize)
               .Take(1)
               .FirstOrDefault();
            long memoryStreamLength = 0;
            await streamZip.ExtractAsync(smallerEntry, (MemoryStream memoryStream) =>
            {
               memoryStreamLength = memoryStream.Length;
            });
            Assert.Equal(232660, memoryStreamLength);
         }
      }


   }
}