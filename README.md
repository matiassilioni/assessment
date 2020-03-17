# Test Task

to run use
dotnet run 
if you want to specify download folder run as:

dotnet run downloadFolder=myDownloadFolder

or you can modify appsettings.json and set DownloadFolder.

Application uses SwaggerUI to expose human friendly api. You can test it by accessing root ( http://localhost:5000 by default)


There is two methods
Post /Download which receives json and starts downloads, validates duplicated filenames to save, otherwise returns 200 OK and donwloads starts in sepparete tasks.

Get /Download which returns current downloads statuses, links, save files and statisctics (start time, downloaded bytes, current speed and status ).


Download threads are controlled by a semaphore
Each download thread is contained in a Task. If link is provided twice to be downloaded to multiple files, it opens a file stream for each file but only one download is started.
There's a validation for multiple downloads to the same file name, returns bad request.

Further information let me know

Pending tasks:
Parallel download requests don't validate the same filename from other request. This would throw errors.
Configure better server things like CORS, etc.
Improve download speed metric
Improve log