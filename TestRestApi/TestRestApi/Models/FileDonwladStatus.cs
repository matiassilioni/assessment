using System.Collections.Generic;
using TestRestApi.Models;

namespace TestRestApi
{
    public class FileDonwladStatus
    {
        public List<FileDownloadDefinition> Definitions { get; set; } = new List<FileDownloadDefinition>();
    }

}