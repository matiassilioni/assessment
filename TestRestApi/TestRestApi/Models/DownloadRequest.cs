using System.Collections.Generic;

namespace TestRestApi
{
    public class DownloadRequest
    {
        public int Threads { get; set; }
        public List<LinkSave> Links { get; set; }
    }

}