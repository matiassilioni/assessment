using System;
using System.Threading.Tasks;
using Xunit;

namespace TestRestApi.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void ValidateNoDuplicated()
        {
            var downloadRequest = new DownloadRequest()
            {
                Threads = 2,
                Links = new System.Collections.Generic.List<LinkSave>
                {
                    new LinkSave{ Filename ="file1", Link =""},
                    new LinkSave{ Filename ="file2", Link =""},
                    new LinkSave{ Filename ="file3", Link =""},
                }
            };
            var downloadService = new DownloaderService(new FileDonwladStatus()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<DownloaderService>>());
            var duplicated = downloadService.GetDuplicatedFiles(downloadRequest);
            Assert.Empty(duplicated);
        }

        [Fact]
        public void ValidateTwoDuplicated()
        {
            var downloadRequest = new DownloadRequest()
            {
                Threads = 2,
                Links = new System.Collections.Generic.List<LinkSave>
                {
                    new LinkSave{ Filename ="file1", Link =""},
                    new LinkSave{ Filename ="file1", Link =""},
                    new LinkSave{ Filename ="file2", Link =""},
                    new LinkSave{ Filename ="file2", Link =""},
                    new LinkSave{ Filename ="file3", Link =""},
                }
            };
            var downloadService = new DownloaderService(new FileDonwladStatus()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<DownloaderService>>());
            var duplicated = downloadService.GetDuplicatedFiles(downloadRequest);
            Assert.Equal(2, duplicated.Count);
            Assert.Equal("file1", duplicated[0]);
            Assert.Equal("file2", duplicated[1]);
        }
        [Fact]
        public void ValidateEmptyLinks()
        {
            var downloadRequest = new DownloadRequest()
            {
                Threads = 2,
                Links = null
            };
            var downloadService = new DownloaderService(new FileDonwladStatus()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<DownloaderService>>());
            var duplicated = downloadService.GetDuplicatedFiles(downloadRequest);
            Assert.Empty(duplicated);
        }


        [Fact]
        public void GroupDifferentLinks()
        {
            var downloadRequest = new DownloadRequest()
            {
                Threads = 2,
                Links = new System.Collections.Generic.List<LinkSave>
                {
                    new LinkSave{ Filename ="file1", Link ="http://file1"},
                    new LinkSave{ Filename ="file2", Link ="http://file2"},
                    new LinkSave{ Filename ="file3", Link ="http://file3"},
                }
            };
            var downloadService = new DownloaderService(new FileDonwladStatus()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<DownloaderService>>());
            var grouped = downloadService.GetGroupedLinks(downloadRequest);
            Assert.Equal(3, grouped.Count);
        }

        [Fact]
        public void GroupDuplicatedLinks()
        {
            var downloadRequest = new DownloadRequest()
            {
                Threads = 2,
                Links = new System.Collections.Generic.List<LinkSave>
                {
                    new LinkSave{ Filename ="file1", Link ="http://file1"},
                    new LinkSave{ Filename ="file2", Link ="http://file1"},
                    new LinkSave{ Filename ="file3", Link ="http://file3"},
                }
            };
            var downloadService = new DownloaderService(new FileDonwladStatus()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<DownloaderService>>());
            var grouped = downloadService.GetGroupedLinks(downloadRequest);
            Assert.Equal(2, grouped.Count);
            Assert.Equal(2, grouped[0].FileDefinitions.Count);
        }

        [Fact]
        public void GroupEmptyLinks()
        {
            var downloadRequest = new DownloadRequest()
            {
                Threads = 2,
                Links = null
            };
            var downloadService = new DownloaderService(new FileDonwladStatus()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<DownloaderService>>());
            var grouped = downloadService.GetGroupedLinks(downloadRequest);
            Assert.Empty(grouped);
        }
        [Fact]
        public async Task EmptyLinksDontStartDownload()
        {
            var downloadRequest = new DownloadRequest()
            {
                Threads = 2,
                Links = null
            };
            var fileDownloadStatus = new FileDonwladStatus();
            var downloadService = new DownloaderService(fileDownloadStatus
                , NSubstitute.Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>()
                , NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<DownloaderService>>());
            await downloadService.Download(downloadRequest);
            Assert.Empty(fileDownloadStatus.Definitions);
        }
    }
}
