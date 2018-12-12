# HttpZipStream 
A simple library to extract specific entries from a remote http zip archive without the need to download the entire file. 

## Sample of how to use the library
Extracting just the first entry from a remote zip archive: 
```csharp 
   var httpUrl = "http://MyRemoteFile.zip"; 
   using (var zipStream = new System.IO.Compression.HttpZipStream(httpUrl)) 
   { 
      var entryList = await zipStream.GetEntriesAsync(); 
      var entry = entryList.FirstOrDefault(); 
      await zipStream.ExtractAsync(entry, (entryStream) => { 
         /* store the entry stream where you like */
      }); 
   }
``` 

## Install instructions
You can add the library to your project using the [nuget](https://www.nuget.org/packages/HttpZipStream) package: 
```
dotnet add package HttpZipStream
```

## Understanding the magic
When opening a zip arquive using a remote url, the zip library will need to download the entire file to be able to read its contents. So if you had a 90 mega zipfile and wanted only a 100 kbyte file from within it, you will end doing the entire 90 mega download anyway.

## Build using
* [DotNET Core](https://dotnet.github.io)
* [xUnit](https://xunit.github.io)
* [vsCode](https://github.com/Microsoft/vscode) 

## Changelog
### v0.1.9
* Some minor documentation adjust

### v0.1.8
* Proper name convention for async methods

## Authors
* [Leverson Carlos](https://github.com/LeversonCarlos). 

## License
GNU General Public License - see the [LICENSE](LICENSE) file for details
