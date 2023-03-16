using System;

namespace PlayListGenerator.DomainModel
{
   public class SongInfo
   {
      public string Title { get; set; }
      public string Artists { get; set; }
      public string Album { get; set; }
      public TimeSpan Duration { get; set; }
      public int TrackNumber { get; set; }
      public string Path { get; set; } //relative path
   }
}
