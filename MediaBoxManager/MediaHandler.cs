using Logger;
using MediaBoxManager.Enum;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace MediaBoxManager;

public class MediaHandler
{
	public string PathDownload { get; set; }
	public string PathMovies { get; set; }
	public string PathShows { get; set; }
	public string PathUnknown { get; set; }
	private readonly SqliteLogger logger;


	readonly FileTools FileManager;

	public MediaHandler(string pathDownload, string pathMovies, string pathShows, string pathUnknown)
	{
		PathDownload = pathDownload;
		PathMovies = pathMovies.Split(',')[0];
		PathShows = pathShows.Split(',')[0];
		PathUnknown = pathUnknown;

		FileManager = new FileTools();
		logger = new SqliteLogger();
	}

	public bool CheckDownloadsAvaialble()
	{
		string[] files = Directory.GetFiles(PathDownload);
		string[] directories = Directory.GetDirectories(PathDownload);

		if (files.Length == 0 && directories.Length == 0)
		{
			logger.Info("Nothing to refactor");
			return false;
		}
		logger.Debug($"Found {files.Length} Files and {directories.Length} Directories in {PathDownload}");
		logger.Debug($"Files -> {String.Join(",", files)}");
		logger.Debug($"Directories -> {String.Join(",", directories)}");
		return true;
	}

	public void Relocate()
	{
		if (!CheckDownloadsAvaialble())
		{
			logger.Info("No New Downloads");
		}
		string sendString = "New Additions:";

		logger.Debug("Sorting external files.");
		string[] files = Directory.GetFiles(PathDownload);
		string[] directories = Directory.GetDirectories(PathDownload);

		sendString += SortTorrentFiles(files, directories);

		logger.Debug("Sorting Folders");
		foreach (string directory in directories)
		{
			string[] files1 = Directory.GetFiles(directory);
			string[] directories1 = Directory.GetDirectories(directory);
			logger.Debug($"Found {files1.Length} files and {directories1.Length} directories in {directory}");
			sendString += SortTorrentFiles(files1, directories1);
			DeleteSubDirectoriesIfEmpty(directory);
		}
		logger.Debug(sendString);
	}

	private void DeleteSubDirectoriesIfEmpty(string startLocation)
	{
		foreach (var directory in Directory.GetDirectories(startLocation))
		{
			DeleteSubDirectoriesIfEmpty(directory);
			if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
			{
				try
				{
					Directory.Delete(directory, false);
				}
				catch (DirectoryNotFoundException)
				{
					logger.Error($"Delete failed: {directory}");
				}
			}
			if (Directory.GetFiles(startLocation).Length == 0 && Directory.GetDirectories(startLocation).Length == 0)
			{
				Directory.Delete(startLocation, false);
			}
		}
	}

	private (bool isSameBaseName, VideoType videoType, string baseName) CheckIfSameBaseName(IEnumerable<string> files)
	{
		List<string> availableBaseNames = [];

		VideoType lastValidVideoType = VideoType.OTHER;
		string lastBaseName = string.Empty;

		foreach (string fileName in files)
		{
			Torrent torrent = MediaTools.BreakdownTorrentFileName(fileName);
			string checkFile = $"{torrent.VideoType}-{torrent.BaseName}";
			if (torrent.FileType == FileType.VIDEO && !availableBaseNames.Contains(checkFile))
			{
				availableBaseNames.Add(checkFile);
				lastValidVideoType = torrent.VideoType;
				lastBaseName = torrent.BaseName;
			}
		}

		if (availableBaseNames.Count == 1)
		{
			logger.Debug($"Group of videos in directory belongs to {lastBaseName}.");
			return (true, lastValidVideoType, lastBaseName);
		}
		else if (availableBaseNames.Count == 0)
		{
			// todo:
		}
		logger.Debug($"Group of videos in directory are mixed. -> {String.Join(",", availableBaseNames)}");
		return (false, VideoType.OTHER, lastBaseName);
	}

	private string SortTorrentFiles(IEnumerable<string> files, IEnumerable<string> directories)
	{
		string refactoredFiles = string.Empty;

		(bool isSameBaseName, VideoType sameVideoType, string SameBaseName) = CheckIfSameBaseName(files);

		if (isSameBaseName)
		{
			foreach (string fileName in files)
			{
				logger.Debug($"Sorting file {fileName} in base {SameBaseName}.");
				Torrent torrent = MediaTools.BreakdownTorrentFileName(fileName);
				refactoredFiles += MoveFile(torrent.FileType, sameVideoType, SameBaseName, fileName);
			}
			foreach (string directory in directories)
			{
				logger.Debug($"Sorting folder {directory}");
				string[] files1 = Directory.GetFiles(directory);
				string[] directories1 = Directory.GetDirectories(directory);

				if (directory.Contains("Subs"))
				{
					logger.Debug($"Moving subs folder of {SameBaseName}.");
					MoveSubsFolder(directory, sameVideoType, SameBaseName);
				}

				logger.Debug($"Found {files1.Length} files and {directories1.Length} directories in {directory}");

				refactoredFiles += SortTorrentFiles(files1, directories1);
			}
		}
		else
		{
			foreach (string fileName in files)
			{
				logger.Debug($"Sorting file {fileName}.");
				Torrent torrent = MediaTools.BreakdownTorrentFileName(fileName);
				refactoredFiles += MoveFile(torrent.FileType, torrent.VideoType, torrent.BaseName, fileName);
			}
		}
		return refactoredFiles;
	}

	private string MoveFile(FileType fileType, VideoType destinationFolder, string baseName, string fileName)
	{
		logger.Debug($"Preparing to move File, {fileName}.");

		string refactoredFiles = string.Empty;
		string newLocation;

		if (fileType == FileType.VIDEO || fileType == FileType.SUBTITLE)
		{
			if (destinationFolder == VideoType.MOVIE)
			{
				newLocation = Path.Combine(PathMovies, baseName);
				refactoredFiles += $"\n{fileName}";
			}
			else if (destinationFolder == VideoType.SHOW)
			{
				newLocation = Path.Combine(PathShows, baseName);
				refactoredFiles += $"\n{fileName}";
			}
			else
			{
				newLocation = Path.Combine(PathUnknown, baseName);
			}
		}
		else
		{
			newLocation = Path.Combine(PathUnknown, baseName);
		}

		FileManager.CreateFolderIfNotExist(newLocation);
		string newDesitnation = Path.Combine(newLocation, Path.GetFileName(fileName));
		try
		{
			File.Move(fileName, newDesitnation);
		}
		catch (System.IO.IOException)
		{
			if (File.Exists(newDesitnation))
			{
				File.Delete(newDesitnation);
			}
			File.Move(fileName, newDesitnation);
		}
		logger.Info($"Moved '{fileName}' -> '{newDesitnation}'");
		return refactoredFiles;
	}

	private void MoveSubsFolder(string subsFolder, VideoType destinationFolder, string baseName)
	{
		string[] subFiles = Directory.GetFiles(subsFolder);
		logger.Debug($"Found {subFiles.Length} Subtitle Files for {baseName}.");

		string? destination = null;

		if (destinationFolder == VideoType.MOVIE)
		{
			destination = Path.Combine(PathMovies, baseName, "Subs");
			FileManager.CreateFolderIfNotExist(destination);
		}
		else if (destinationFolder == VideoType.SHOW)
		{
			destination = Path.Combine(PathShows, baseName, "Subs");
			FileManager.CreateFolderIfNotExist(destination);
		}

		if (destination != null)
		{
			foreach (var file in subFiles)
			{
				logger.Debug($"Found Sub File {file}");
				var filePath = Path.Combine(subsFolder, file);
				var destinationPath = Path.Combine(destination, file);

				File.Move(filePath, destinationPath);
				logger.Info($"Moved {filePath} -> {destinationPath}");
			}
		}
		DeleteSubDirectoriesIfEmpty(subsFolder);
	}
}
