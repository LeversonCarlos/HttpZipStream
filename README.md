# HttpZipStream 
A simple library to extract specific entries from a remote http zip archive without the need to download the entire file. 

## Install instructions
You can add the library to your project using the [nuget](https://www.nuget.org/packages/HttpZipStream) package: 
```
dotnet add package HttpZipStream
```

## Sample of how to use the library
Extracting just the first entry from a remote zip archive: 
```csharp 
   var httpUrl = "http://MyRemoteFile.zip"; 
   using (var zipStream = new HttpZipStream(httpUrl)) 
   { 
      var entryList = await zipStream.GetEntries(); 
      var entry = entryList.FirstOrDefault(); 
      await zipStream.Extract(entry, (MemoryStream entryStream) => { 
         /* store the entry stream where you like */
      }); 
   }
``` 

## Build using
* [DotNET Core](https://dotnet.github.io)
* [xUnit](https://xunit.github.io)
* [vsCode](https://github.com/Microsoft/vscode) 

## Authors
* [Leverson Carlos](https://github.com/LeversonCarlos). 

## License
GNU General Public License - see the [LICENSE](LICENSE) file for details
