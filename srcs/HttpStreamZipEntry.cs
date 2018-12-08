namespace System.IO.Compression
{
   public class HttpStreamZipEntry
   { 

      internal HttpStreamZipEntry(int index)
      {
         this.Index = index;
      }

      public int Index { get; }

      internal int Signature { get; set; }
      internal short VersionMadeBy { get; set; }
      internal short MinimumVersionNeededToExtract { get; set; }
      internal short GeneralPurposeBitFlag { get; set; }

      public short CompressionMethod { get; internal set; }
      public int FileLastModification { get; internal set; }
      public int CRC32 { get; internal set; }
      public int CompressedSize { get; internal set; }
      public int UncompressedSize { get; internal set; }
      // public short FileNameLength { get; set; }
      // public short ExtraFieldLength { get; set; }
      // public short FileCommentLength { get; set; }
      // public short DiskNumberWhereFileStarts { get; set; }
      // public short InternalFileAttributes { get; set; }
      // public short ExternalFileAttributes { get; set; }
      // public int FileOffset { get; set; }
      // public string FileName { get; set; }
      // public string ExtraField { get; set; }
      // public string FileComment { get; set; }
   }
}