using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TestRestApi.Models;

namespace TestRestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DownloadController : ControllerBase
    {
        private readonly ILogger<DownloadController> _logger;
        private readonly DownloaderService _downloaderService;
        private readonly List<FileDownloadDefinition> _downloadDefinitions;

        public DownloadController(ILogger<DownloadController> logger, DownloaderService downloaderService, FileDonwladStatus status)
        {
            _logger = logger;
            _downloaderService = downloaderService;
            _downloadDefinitions = status.Definitions;
        }

        [HttpPost]
        public IActionResult Download(DownloadRequest request)
        {
            if(request.Links == null || request.Links.Count ==0)
            {
                return new BadRequestObjectResult("no links provided");
            }
            if(!_downloaderService.ValidateHttpLinks(request))
                return new BadRequestObjectResult("http links only");

            var duplicated = _downloaderService.GetDuplicatedFiles(request);
            if(duplicated.Count != 0)
            {
                return new BadRequestObjectResult($"duplicated filenames to save: {duplicated.Aggregate((i, j) => i + ", " + j)}");
            }
            _downloaderService.Download(request);
            return Ok();
        }

        [HttpGet]
        public List<FileDownloadDefinition> DownloadStatus()
        {
            return _downloadDefinitions;
        }
    }
}
