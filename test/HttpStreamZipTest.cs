using System;
using System.Linq;
using Xunit;

namespace System.IO.Compression
{
   public class HttpStreamZipTest
   {
      string httpUrl = "https://nbksiq.sn.files.1drv.com/y4mxC666kaizJK8ia95nl3gUuB8WKqXMBcnU7tGmXYhnTe0y6-PZDRAnQ0vmEXYG4RBvNypa-J_l-M5WM028Z3OxIXbs5epdmvrRcB7WtSFmqSvyU7kxqnAZWTGvm7F0F635-xH1LxbeAhqvrJjSvTovT4fhSSnlf69I6Nf5fMYO0IBGhLEiSGaNQVIfyiZkBnRKxZijGtIyznOUY3BLv6yTw/Blue%20Beetle%20%5B1967%5D%20%2301.cbz?download&psid=1";


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
      public async void ExampleStream_Entries_MustHave_36items()
      { 
         using (var streamZip = new System.IO.Compression.HttpStreamZip(httpUrl))
         {
            var contentLength = await streamZip.GetContentLengthAsync();
            var entryList = await streamZip.GetEntries();
            Assert.Equal(36, entryList.Count);
         }
      }


      [Fact]
      public async void ExampleStream_LargerEntry_MustBe_0001_With_347kbytes()
      { 
         using (var streamZip = new System.IO.Compression.HttpStreamZip(httpUrl))
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
      public async void ExampleStream_SmallerEntry_MustBe_0005_With_227kbytes()
      { 
         using (var streamZip = new System.IO.Compression.HttpStreamZip(httpUrl))
         {
            var contentLength = await streamZip.GetContentLengthAsync();
            var entryList = await streamZip.GetEntries();
            var smallerEntry = entryList
               .OrderBy(x => x.CompressedSize)
               .Take(1)
               .FirstOrDefault();
            Assert.Equal("Blue Beetle [1967] #01 - 0035.jpg", smallerEntry.FileName);
            Assert.Equal(232723, smallerEntry.CompressedSize);
         }
      }

   }
}