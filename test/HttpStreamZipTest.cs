using System;
using Xunit;

namespace System.IO.Compression
{
   public class HttpStreamZipTest
   {
      string httpUrl = "https://nbksiq.sn.files.1drv.com/y4mS-DyZXkpHW8UpWwT7BoWkgKCVirwqxCPg-5ejrB1xbLYp38WtJr5cgs35OxfeAscDxpC5EmZcfq0lBOcyxKO_Cpmq92ywqgnltBvYD5MlCxsiklatljizQJ3sS-76aGvbqRBTZWJMjKrBYe6K2UdzfbDRjmuKqOvE_TbhbfHBD_4GmFJfGJwmdrlBsxK7BEKg9gHRDpNKJlt6Eo3sPNPKA/Blue%20Beetle%20%5B1967%5D%20%2301.cbz?download&psid=1";

      [Fact]
      public async void ExampleStream_ContentLength_MustBe_9702kbytes()
      {
         var streamZip = new System.IO.Compression.HttpStreamZip(httpUrl);
         var contentLength = await streamZip.GetContentLengthAsync();
         Assert.Equal(9935427, contentLength);
      }

   }
}