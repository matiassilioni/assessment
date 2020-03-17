using System;
using System.Collections.Generic;

namespace TestRestApi.Models

{
    public class FileDownloadDefinition
    {
        public FileDownloadDefinition() { }
        public FileDownloadDefinition(LinkSave definition)
        {
            this.FileDefinitions.Add(definition);
        }
        public FileDownloadDefinition(IEnumerable<LinkSave> definitions)
        {
            this.FileDefinitions.AddRange(definitions);
        }
        public List<LinkSave> FileDefinitions { get; set; } = new List<LinkSave>();
        //public long FileLength { get; set; }
        //public int Threads { get; set; }
        public long BytesDownloaded { get; set; }
        public DateTime? Started { get; set; }
        public DateTime? DownloadEnd { get; set; }
        public State Status { get; set; } = State.Waiting;
        public String StatusString => this.Status.ToString();

        public string CurrentSpeed { get; internal set; }
    }

}