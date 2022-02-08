namespace AudibleIngestService
{
    public class Audiobook
    {
        public Audiobook(string inputPath, string fileName)
        {
            InputPath = inputPath;
            InputFileName = fileName;
        }

        public string InputPath { get; set; }

        public string InputFileName { get; set; }

        public string OutputPath { get; set; }

        public string NewFileName { get; set; }

        public string Title { get; set; }

        public string Author { get; set; }
    }
}