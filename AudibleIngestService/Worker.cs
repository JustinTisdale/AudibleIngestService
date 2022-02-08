using FFMpegCore;
using System.Text.RegularExpressions;

namespace AudibleIngestService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ServiceConfiguration _configuration;


        public Worker(ILogger<Worker> logger, ServiceConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!string.IsNullOrWhiteSpace(_configuration.FFMpegBinPath))
            {
                GlobalFFOptions.Configure(new FFOptions() { BinaryFolder = _configuration.FFMpegBinPath });
            }

            // TODO: cancellation token on child methods
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking for new files.");
                DirectoryInfo directory = new DirectoryInfo(_configuration.InputDirectory);

                var archivedFilenames = await GetArchivedFileNames();
                var matchingFiles = directory.GetFiles("*.aax");
                var filesForConversion = matchingFiles.Where(a => archivedFilenames.All(b => b != a.Name));

                // If any files, convert the first one
                if (!filesForConversion.Any())
                {
                    _logger.LogInformation("Didn't find any new files. Sleeping for 60 seconds before checking again.");
                    await Task.Delay(60000, stoppingToken);
                    continue;
                }

                //TODO: With the way this is currently written, if a file errors out repeatedly the process is going to get stuck
                //      because it'll just attempt the same file in an infinite loop. Add some error tracking so the file causing
                //      errors is either skipped or moved to the archive folder.
                _logger.LogInformation("There are {fileCount} unprocessed .aax files in the input directory.", filesForConversion.Count());
                var audiobook = await GetInfo(new Audiobook(filesForConversion.FirstOrDefault().FullName, filesForConversion.FirstOrDefault().Name));
                _logger.LogInformation("Converting file: {fileName}", audiobook.InputPath);
                _logger.LogInformation("{title} by {author}", audiobook.Title, audiobook.Author);
                bool success = await ConvertAudiobook(audiobook);

                if (success)
                {
                    _logger.LogInformation("Successfully converted file {fileName}.", audiobook.InputPath);
                    _logger.LogInformation("New file path is {newFilePath}", Path.Combine(_configuration.OutputDirectory, audiobook.NewFileName));
                    // After conversion, do archive logic based on appsettings
                    await FinishFile(audiobook);
                }

                if (_configuration.PauseInSecondsBetweenConversions.HasValue)
                {
                    _logger.LogInformation($"Sleeping for {_configuration.PauseInSecondsBetweenConversions} seconds.");
                    await Task.Delay(_configuration.PauseInSecondsBetweenConversions.Value * 1000, stoppingToken);
                }
                
            }
        }

        public async Task FinishFile(Audiobook audiobook)
        {
            if (!string.IsNullOrWhiteSpace(_configuration.ArchiveFileName))
            {
                _logger.LogInformation("Added {fileName} to archive text file.", audiobook.InputFileName);
                await File.AppendAllTextAsync(GetArchiveFilePath(), audiobook.InputFileName + Environment.NewLine);
            }

            if (!string.IsNullOrWhiteSpace(_configuration.ArchiveDirectory))
            {
                string newPath = Path.Combine(_configuration.ArchiveDirectory, audiobook.InputFileName);
                File.Move(audiobook.InputPath, newPath);
                _logger.LogInformation("Moved file to {newFilePath}", newPath);
                return; // If you archived the file, do not attempt to delete it.
            }

            if (_configuration.DeleteInputFilesAfterIngest)
            {
                File.Delete(audiobook.InputPath); 
                _logger.LogInformation("Deleted file {oldFilePath}", audiobook.InputPath);
            }
        }

        public async Task<bool> ConvertAudiobook(Audiobook audiobook)
        {
            string outputToFile = Path.Join(_configuration.OutputDirectory, audiobook.NewFileName);
            
            if (File.Exists(outputToFile))
            {
                // This file would overwrite one that already exists. Add some random characters and let the human figure it out later.
                outputToFile = Path.Join(_configuration.OutputDirectory, (GenerateRandomString(6) + "_" + audiobook.NewFileName));
            }

            return await FFMpegArguments
                .FromFileInput(audiobook.InputPath, verifyExists: false, options =>
                {
                    options.WithCustomArgument($"-activation_bytes {_configuration.AudibleActivationBytes}");
                })
                .OutputToFile(outputToFile, overwrite: true, options =>
                {
                    options.WithCustomArgument("-c copy");
                })
                .ProcessAsynchronously(throwOnError: true);
        }

        /// <summary>
        /// Generate a small randomized string. Please don't use this for anything to do with security.
        /// </summary>
        /// <returns>A string of 6 pseudorandom characters</returns>
        private string GenerateRandomString(int length)
        {
            // Stolen mostly from https://stackoverflow.com/a/1344258
            string pool = "abcdefghijklmnopqrstuvwxyz";

            var chars = new char[length];
            var rng = new Random();
            for(int i = 0; i < chars.Length; i++)
            {
                chars[i] = pool[rng.Next(pool.Length)];
            }

            return new string(chars);
        }

        public async Task<Audiobook> GetInfo(Audiobook audiobook)
        {
            var info = await FFProbe.AnalyseAsync(audiobook.InputPath);

            if(info.Format == null)
            {
                throw new InvalidOperationException("Title and Author are not present as expected in the Format metadata.");
            }

            audiobook.Title = info.Format.Tags["title"];
            audiobook.Author = info.Format.Tags["artist"];

            Regex validFileCharacters = new Regex(@"[^\w\-. ]+");
            string fileName = validFileCharacters.Replace(audiobook.Title, "") + " - " + validFileCharacters.Replace(audiobook.Author, "") + ".m4b";
            audiobook.NewFileName = fileName;
            return audiobook;
        }

        public string GetArchiveFilePath()
        {
            if (string.IsNullOrWhiteSpace(_configuration.ArchiveFileName))
            {
                return null;
            }

            return Path.Combine(_configuration.InputDirectory, _configuration.ArchiveFileName);
        }

        public async Task<List<string>> GetArchivedFileNames()
        {
            if (!File.Exists(GetArchiveFilePath()))
                return new List<string>();

            string fileContents = await File.ReadAllTextAsync(GetArchiveFilePath());
            return fileContents.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToList();
        }
    }
}