namespace System.IO.Compression
{
   internal class DirectoryData
   {
      public int Offset { get; set; }
      public int Size { get; set; }
      public short Entries { get; set; }
   }
}