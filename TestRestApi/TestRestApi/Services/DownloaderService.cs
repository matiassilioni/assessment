using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TestRestApi.Models;

namespace TestRestApi
{
    public class DownloaderService
    {
        private readonly List<FileDownloadDefinition> _definitions;
        private readonly string _downloadFolder;
        private readonly ILogger<DownloaderService> _logger;


        public DownloaderService(FileDonwladStatus fileDonwloadStatus, IConfiguration configuration, ILogger<DownloaderService> logger)
        {
            this._definitions = fileDonwloadStatus.Definitions;
            _downloadFolder = configuration["DownloadFolder"];
            this._logger = logger;
        }

        public List<string> GetDuplicatedFiles(DownloadRequest files)
        {
            if (files.Links == null)
                return new List<string>();
            List<string> duplicatedFiles = files.Links.GroupBy(x => x.Filename).Where(w => w.Count() > 1).Select(s => s.Key).ToList();
            return duplicatedFiles;
        }

        public async Task Download(DownloadRequest files)
        {

            if (files.Links == null || files.Links.Count == 0)
                return;
            var tasks = new List<Task>();
            if (files.Threads < 1)
                files.Threads = 1;

            var duplicated = GetDuplicatedFiles(files);
            if (duplicated.Count != 0)
                return;

            //Semaphore to control max concurrent parallel downloads.
            var semaphore = new SemaphoreSlim(files.Threads, files.Threads);
            
            //Fix Duplicated links to different file names
            var currentDownloadDefinitions = GetGroupedLinks(files);

            _definitions.AddRange(currentDownloadDefinitions);

            var downloadTasks = new List<Task>();
            foreach (var link in currentDownloadDefinitions)
            {
                var downloadTask = Task.Run(async () =>
                {
                    var freshLink = link;
                    var fileStreams = new List<FileStream>();
                    await semaphore.WaitAsync();
                    _logger.LogInformation("{link}, Started", freshLink.FileDefinitions[0].Link);
                    freshLink.Status = State.Downloading;
                    freshLink.Started = DateTime.Now;
                    freshLink.BytesDownloaded = 0;
                    try
                    {
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(freshLink.FileDefinitions[0].Link);
                        var response = await req.GetResponseAsync();

                        using (var reponseStream = response.GetResponseStream())
                        {

                            foreach (var fileDefinition in freshLink.FileDefinitions)
                            {
                                if (!Directory.Exists(_downloadFolder))
                                    Directory.CreateDirectory(_downloadFolder);
                                fileStreams.Add(new FileStream(Path.Combine(_downloadFolder, fileDefinition.Filename), FileMode.OpenOrCreate));
                            }
                            const int bufferSize = 1024 * 500;  //500kb
                            var buffer = new byte[bufferSize];
                            int bytesRead = 0;
                            var stopwatch = new Stopwatch();
                            do
                            {
                                stopwatch.Restart();
                                //reponseStream.Seek(downloader.Start, SeekOrigin.Current);
                                bytesRead = await reponseStream.ReadAsync(buffer, 0, bufferSize);
                                stopwatch.Stop();
                                freshLink.BytesDownloaded += bytesRead;
                                foreach (var fs in fileStreams)
                                {
                                    await fs.WriteAsync(buffer, 0, bytesRead);
                                }
                                decimal speed = bytesRead / (decimal)1024;
                                speed = speed / (decimal)stopwatch.Elapsed.TotalSeconds;
                                freshLink.CurrentSpeed = $"{Math.Round(speed, 2)} kb/s";
                            } while (bytesRead > 0);
                            freshLink.DownloadEnd = DateTime.Now;
                            freshLink.Status = State.Finished;
                        }
                    }
                    catch (Exception e)
                    {
                        freshLink.Status = State.Error;
                        _logger.LogError(e, $"Error downloading [{freshLink.FileDefinitions[0].Link}].");
                    }
                    finally
                    {
                        foreach (var fs in fileStreams)
                        {
                            fs.Close();
                        }
                        semaphore.Release();
                    }
                });
                downloadTasks.Add(downloadTask);
            }
            await Task.WhenAll(downloadTasks);
            _logger.LogInformation("all requested downloads finished");
        }

        public List<FileDownloadDefinition> GetGroupedLinks(DownloadRequest files)
        {
            if (files.Links == null)
                return new List<FileDownloadDefinition>();
            var items = files.Links.GroupBy(x => x.Link).Select(x => new FileDownloadDefinition(x)).ToList();
            return items;
            
        }
    }
}