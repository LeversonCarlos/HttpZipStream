# HttpZipStream 
A simple library to extract specific entries from a remote http zip archive without the need to download the entire file.  
![Release](https://github.com/LeversonCarlos/HttpZipStream/workflows/Release/badge.svg)

## Understanding the magic
When opening a zip archive using a remote url, the zip library will need to download the entire file to be able to read its contents. So if you had a 90 mega zipfile and wanted only a 100 kbyte file from within it, you will end doing the entire 90 mega download anyway.  
The [zip format](https://en.wikipedia.org/wiki/Zip_(file_format)) defines a directory pointing to all it's inner entries. Containing properties like names, starting offset, size, and other stuff. And this directory is pretty small, just a few bytes placed on the very end of the archive. So, if we could just read this directory, we could know where, on the entire zip archive, is stored the file we want.  
And if we could just request from the remote url, just that part of the content, we could get a smaller download, with just what we want and need.  
Turns out that the http protocol supports a technique called [byte serving](https://en.wikipedia.org/wiki/Byte_serving). That states that we could define some header parameters on the http request specifying the byte ranges we want for that request.  
With that in mind, what we do it's pretty simple. We make a first http request asking just for the http headers (not its content) and from that we know the content size. Then we make a small range requests at the end of the file, extracting all the directory info. Then, for the entries we want, we make requests for just that ranges. Apply the deflate algoritm and it's done.  
With this approach, we end doing more http requests, so its only good to use if the desired content represents a small part of the entire zip archive.  
More on this, can be found on my [medium](https://medium.com/@lcjohnny/httpzipstream-extracting-single-entry-from-remote-zip-without-downloading-the-entire-file-7a0f3d24a6fc) article.

## Install instructions
You can add the library to your project using the [nuget](https://www.nuget.org/packages/HttpZipStream) package: 
```
dotnet add package HttpZipStream
```

## Sample of how to use the library
Extracting just the first entry from a remote zip archive: 
```csharp 
   var httpUrl = "http://MyRemoteFile.zip"; 
   using (var zipStream = new System.IO.Compression.HttpZipStream(httpUrl)) 
   { 
      var entryList = await zipStream.GetEntriesAsync(); 
      var entry = entryList.FirstOrDefault(); 
      byte[] entryContent = await zipStream.ExtractAsync(entry);
      /* do what you want with the entry content */
   }
``` 

## Build using
* [DotNET Core](https://dotnet.github.io)
* [xUnit](https://xunit.github.io)
* [vsCode](https://github.com/Microsoft/vscode) 
* [ZipFormat](https://en.wikipedia.org/wiki/Zip_(file_format))

## Changelog
### v0.1.*
- Some minor documentation adjust.  
- Proper name convention for async methods.  
- Preparing projects to be build, packed and deploy by the server.  
### v0.2.*
- Implementing a ExtractAsync overload that results just the entry content byte array.  
- BUG #13: Some entries are not deflate correctly.  
### v0.3.*
- Upgrading dotnet version to 3.1


## Authors
* [Leverson Carlos](https://github.com/LeversonCarlos) 

## License
MIT License - see the [LICENSE](LICENSE) file for details
