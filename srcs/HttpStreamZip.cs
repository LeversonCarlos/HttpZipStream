using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace System.IO.Compression
{
   public class HttpStreamZip
   {

      HttpClient httpClient { get; set; }
      public HttpStreamZip() : this(new HttpClient()) { }
      public HttpStreamZip(HttpClient httpClient)
      {
         this.httpClient = httpClient;
         this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
      }

   }
}