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


        public DownloaderService(FileDonwladStatus fileDonwladStatus, IConfiguration configuration, ILogger<DownloaderService> logger)
        {
            this._definitions = fileDonwladStatus.Definitions;
            _downloadFolder = configuration["DownloadFolder"];
            this._logger = logger;
        }

        public List<string> GetDuplicatedFiles(DownloadRequest files)
        {
            List<string> duplicatedFiles = files.Links.GroupBy(x => x.Filename).Where(w => w.Count() > 1).Select(s => s.Key).ToList();
            return duplicatedFiles;
        }

        public async Task Download(DownloadRequest files)
        {
            var tasks = new List<Task>();
            if (files.Threads < 1)
                files.Threads = 1;

            var semahpore = new SemaphoreSlim(files.Threads, files.Threads);
            //ReadDuplicated
            var currentDownloadDefinitions = GetGroupedLinks(files);
            
            _definitions.AddRange(currentDownloadDefinitions);

            var downloadTasks = new List<Task>();
            foreach (var link in currentDownloadDefinitions)
            {
                var downloadTask = Task.Run(async () =>
                {
                    var freshLink = link;
                    var fileStreams = new List<FileStream>();
                    await semahpore.WaitAsync();
                    freshLink.Started = DateTime.Now;
                    freshLink.BytesDownloaded = 0;
                    try
                    {
                        _logger.LogInformation("{link}, Started", freshLink.FileDefinitions[0].Link);
                        freshLink.Status = State.Downloading;
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(freshLink.FileDefinitions[0].Link);
                        var response = await req.GetResponseAsync();

                        using (var reponseStream = response.GetResponseStream())
                        {
                            
                            foreach (var fileDefinition in freshLink.FileDefinitions)
                            {
                                if (!Directory.Exists(_downloadFolder))
                                    Directory.CreateDirectory(_downloadFolder);
                                fileStreams.Add(new FileStream(Path.Combine(_downloadFolder,fileDefinition.Filename), FileMode.OpenOrCreate));
                            }
                            const int bufferSize = 1024 * 1024;
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
                                freshLink.CurrentSpeed = $"{Math.Round(speed,2)} kb/s";
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
                        semahpore.Release();
                    }
                });
                downloadTasks.Add(downloadTask);
            }
            await Task.WhenAll(downloadTasks);
            _logger.LogInformation("all requested downloads finished");
        }

        private List<FileDownloadDefinition> GetGroupedLinks(DownloadRequest files)
        {
            var currentDownloadDefinitions = new List<FileDownloadDefinition>();
            foreach (var link in files.Links)
            {
                var alreadyRegisteredToDownload = currentDownloadDefinitions.FirstOrDefault(x => x.FileDefinitions.FirstOrDefault(y => y.Link == link.Link) != null);
                if (alreadyRegisteredToDownload != null)
                {
                    alreadyRegisteredToDownload.FileDefinitions.Add(link);
                    continue;
                }
                var freshLink = new FileDownloadDefinition(link);
                currentDownloadDefinitions.Add(freshLink);
            }
            return currentDownloadDefinitions;
        }

        //public async Task Download(DownloadRequest files)
        //{
        //    var tasks = new List<Task>();

        //    foreach (var link in files.Links)
        //    {
        //        var alreadyRegisteredToDoownload = _definitions.FirstOrDefault(x => x.FileDefinitions.FirstOrDefault(y => y.Link == link.Link) != null);
        //        if (alreadyRegisteredToDoownload != null)
        //        {
        //            alreadyRegisteredToDoownload.FileDefinitions.Add(link);
        //            await this.CopyIfFinished(alreadyRegisteredToDoownload);
        //            continue;
        //        }
        //        var freshLink = new FileDownloadDefinition(link);

        //        //TODO: VALIDATE LINK
        //        var req = WebRequest.Create(link.Link);
        //        //var response = await req.GetResponseAsync();
        //        req.Method = "HEAD";
        //        var resp = await req.GetResponseAsync();

        //        int responseLength = int.Parse(resp.Headers.Get("Content-Length"));
        //        var parts = files.Threads;
        //        var eachSize = responseLength / parts;
        //        var lastPartSize = eachSize + (responseLength % parts);


        //        _definitions.Add(freshLink);
        //        int i = 0;
        //        for (; i < parts - 1; i++)
        //        {
        //            tasks.Add(DoDownload(new FilePartDownloadRequest(link.Link, i * eachSize, eachSize, i)));
        //        }
        //        tasks.Add(DoDownload(new FilePartDownloadRequest(link.Link, (parts - 1) * eachSize, lastPartSize, i)));
        //    }

        //    //string url = "http://somefile.mp3";
        //    //List<FileDownloader> filewonloadersList = new List<FileDownloader>();
        //    //System.Net.WebRequest req = System.Net.HttpWebRequest.Create(url);
        //    //var response = req.GetResponse();
        //    //req.Method = "HEAD";
        //    //System.Net.WebResponse resp = req.GetResponse();
        //    //int responseLength = int.Parse(resp.Headers.Get("Content-Length"));
        //    //int parts = 6;
        //    //var eachSize = responseLength / parts;
        //    //var lastPartSize = eachSize + (responseLength % parts);
        //    //for (int i = 0; i < parts - 1; i++)
        //    //{
        //    //    filewonloadersList.Add(new FileDownloader(url, i * eachSize, eachSize));
        //    //}
        //    //filewonloadersList.Add(new FileDownloader(url, (parts - 1) * eachSize, lastPartSize));

        //    //var threads = new List<Thread>();
        //    //foreach (var item in filewonloadersList)
        //    //{
        //    //    var newThread = new Thread(DoDownload);
        //    //    threads.Add(newThread);
        //    //    newThread.Start(item);
        //    //}
        //    await Task.WhenAll(tasks);
        //}

        private async Task CopyIfFinished(FileDownloadDefinition definition)
        {
            if (definition.BytesDownloaded == 100)
            {
                await Task.CompletedTask;
                //TODO: COPY FILE
            }
            return;
        }

        //public async Task DoDownload(FilePartDownloadRequest filePartDownload)
        //{
        //    var finished = false;
        //    while (!finished)
        //    {
        //        try
        //        {
        //            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(filePartDownload.Url);
        //            req.AddRange(filePartDownload.Start, filePartDownload.Start + filePartDownload.Count - 1);
        //            var response = await req.GetResponseAsync();
        //            using (var reponseStream = response.GetResponseStream())
        //            {
        //                using (var fs = new FileStream($"temp_{filePartDownload.Part}.sth", FileMode.OpenOrCreate))
        //                {
        //                    fs.Seek(0, SeekOrigin.Begin);
        //                    var buffer = new byte[1024];
        //                    int bytesRead = 0;
        //                    do
        //                    {
        //                        //reponseStream.Seek(downloader.Start, SeekOrigin.Current);
        //                        bytesRead = await reponseStream.ReadAsync(buffer, 0, 1024);
        //                        await fs.WriteAsync(buffer, 0, bytesRead);
        //                        //await fs.FlushAsync();
        //                    } while (bytesRead > 0);
        //                    fs.Close();
        //                }
        //            }
        //            finished = true;
        //        }
        //        catch (WebException e)
        //        {
        //            if (e.Status == WebExceptionStatus.Timeout || e.Status == WebExceptionStatus.KeepAliveFailure)
        //            {
        //                //retry
        //            }
        //            throw e;
        //        }
        //    }
        //}
    }

}