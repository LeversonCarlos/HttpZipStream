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

      internal short FileNameLength { get; set; }
      internal short ExtraFieldLength { get; set; }
      internal short FileCommentLength { get; set; }

      internal short DiskNumberWhereFileStarts { get; set; }
      internal short InternalFileAttributes { get; set; }
      internal short ExternalFileAttributes { get; set; }

      internal int FileOffset { get; set; }
      public string FileName { get; internal set; }
      // public string ExtraField { get; set; }
      // public string FileComment { get; set; }
   }
}