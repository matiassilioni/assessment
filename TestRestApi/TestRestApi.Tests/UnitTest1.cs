using System;
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
    }
}
