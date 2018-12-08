# HttpZipStream 
A simple library to extract specific entries from a remote http zip archive without the need to download the entire file. 

## Install instructions
You can add the library to your project using the nuget package: 
```
dotnet add package HttpZipStream
```

## Samples of how to use the library
How to extract just the first entry from a remote zip archive: 
```csharp 
   var httpUrl = "http://MyRemoteFile.zip";
   using (var streamZip = new HttpZipStream(httpUrl))
   {      
      var entryList = await streamZip.GetEntries(); 
      var entry = entryList.FirstOrDefault();
      await streamZip.Extract(entry, (MemoryStream entryStream) => { 
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
GNU General Public License - see the [LICENSE.md](LICENSE.md) file for details
