using System;
using Xunit;

namespace System.IO.Compression
{
   public class HttpStreamZipTest
   {
      string httpUrl = "https://nbksiq.sn.files.1drv.com/y4mMf9YEnMLi4aw8MXlkr424G5_5GXPI60vKgFC_XYM2e26Md1R4j4msK1kb1I7wGGS6i_t6SQA0xrAvqPNLAIxCRWqUf7DL-XOTWcq25XaLjFHZMZgWRdENqFR48bM50SIe1wppMhZmf6NyFHsQYH_8Us72c-0in6mVfRIL9jJSGisJCHNaOb6rRE43CalkNRXPJSyGWRIOsypps9pgIVRrw/Blue%20Beetle%20%5B1967%5D%20%2301.cbz?download&psid=1";


      [Fact]
      public async void ExampleStream_ContentLength_MustBe_9702kbytes()
      {
         using (var streamZip = new System.IO.Compression.HttpStreamZip(httpUrl))
         {
            var contentLength = await streamZip.GetContentLengthAsync();
            Assert.Equal(9935427, contentLength);
         }
      }


      [Fact]
      public async void TempTest()
      { 
         using (var streamZip = new System.IO.Compression.HttpStreamZip(httpUrl))
         {
            var contentLength = await streamZip.GetContentLengthAsync();
            var entryList = await streamZip.GetEntries();
            Assert.NotNull(entryList);
         }
      }


   }
}