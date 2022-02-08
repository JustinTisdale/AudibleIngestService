# Audible Ingest Service

## Purpose

A little Windows Service to watch a directory for newly-added .aax files and convert them to .m4b files.

This is a convenient method to archive your legally-purchased Audible library. You will need your personal Audible activation bytes. The means for acquiring these are beyond the scope of this service, but there are various means here on Github.

## Disclaimer

This little service isn't very smart. Error handling is minimal and its log entries may or may not be helpful. I wrote it as a convenience for myself and am sharing it in hopes others might find it useful. Use at your own risk.

## Requirements

1. ffmpeg and ffprobe binaries for your platform
2. Your Audible activation bytes
3. .Net 6 Runtime

## Building

```bash
dotnet publish -r win-x64 --self-contained false
```

## Installation

Run cmd or Powershell as Administrator and execute:

```bash
sc create AudibleIngestService start= auto binPath= "I:\Services\AudibleIngestService\AudibleIngestService.exe" DisplayName= "Audible Ingest Service"
sc start AudibleIngestService
```

## Configuration

Edit the appsettings.json file and apply settings that suit your needs.

```json
    "AppSettings": {
    // Required - Your Audible activation bytes. 
    "AudibleActivationBytes": "abcd1234",
    // Required - The directory to watch for new .aax files to be converted.
    "InputDirectory": "",
    // Required - The directory to move new .m4b files to after conversion.
    "OutputDirectory": "",
    // Required - Should files be deleted after conversion? If true, ArchiveDirectory is ignored.
    "DeleteInputFilesAfterIngest": false,
    // Optional - If present, ingested files are moved to this directory after conversion.
    "ArchiveDirectory": "",
    // Optional - If present, converted file names are added to this text file. If a file name is present in this file, it will be ignored.
    "ArchiveFileName": "archive_audible_ingest.txt",
    // Optional - If present, sets the path to the binaries for ffmpeg/ffprobe
    "FFMpegBinPath": "C:/Tools",
    // Optional - If present, sets the number of seconds to pause between file conversions. If not present, defaults to 60 seconds.
    "PauseInSecondsBetweenConversions":  60,
    // Optional - If present, sets the file path to be used when writing a detailed log file
    "LogFileName": "ConversionLog.txt"
  }
```