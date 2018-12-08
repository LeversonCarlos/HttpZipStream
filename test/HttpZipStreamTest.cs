using System;
using System.Linq;
using Xunit;

namespace System.IO.Compression
{
   public class HttpZipStreamTest
   {
      string httpUrl = "https://nbksiq.sn.files.1drv.com/y4mb3XRulz2OcLKckOtAzTMseqZchltP7kuzfkMPee93wNSXAdKgqP5_c0U-4rqZnsMcTjpbN5Ojnw31N3GDYNKkj8A6P7VV3JaFuacLSZ5LLknkfX5GC1iOrf3YpkHwo2EsaSVwE7jcxs7Id8-BI97tZJ-V2vf5qb3hRPif0MG_mMdY0GjPu1kAt7xxfnxzlOWeBfzlEaBwKGFL-QL3NyFXw/Blue%20Beetle%20%5B1967%5D%20%2301.cbz?download&psid=1";


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
            var entryList = await streamZip.GetEntries();
            Assert.Equal(36, entryList.Count);
         }
      }


      [Fact]
      public async void ExampleStream_LargerEntry_MustBe_0001_With_347kbytes()
      { 
         using (var streamZip = new HttpZipStream(httpUrl))
         {
            var contentLength = await streamZip.GetContentLengthAsync();
            var entryList = await streamZip.GetEntries();
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
            var entryList = await streamZip.GetEntries();
            var smallerEntry = entryList
               .OrderBy(x => x.CompressedSize)
               .Take(1)
               .FirstOrDefault();
            long memoryStreamLength = 0;
            await streamZip.ExtractEntries(smallerEntry, (MemoryStream memoryStream) =>
            {
               memoryStreamLength = memoryStream.Length;
            });
            Assert.Equal(232660, memoryStreamLength);
         }
      }

   }
}