namespace AudibleIngestService
{
    public class ServiceConfiguration
    {
        public string AudibleActivationBytes { get; set; }

        public string InputDirectory { get; set; }

        public string OutputDirectory { get; set; }

        public bool DeleteInputFilesAfterIngest { get; set; } = false;

        public string ArchiveDirectory { get; set; }

        public string ArchiveFileName { get; set; }

        public string FFMpegBinPath { get; set; }

        public int? PauseInSecondsBetweenConversions { get; set; }

        public string LogFileName { get; set; }

        public string DuplicateDetectionBehavior { get; set; }
    }
}