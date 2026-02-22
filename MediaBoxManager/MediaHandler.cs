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


	readonly FileTools FileManager;

	public MediaHandler(string pathDownload, string pathMovies, string pathShows, string pathUnknown)
	{
		PathDownload = pathDownload;
		PathMovies = pathMovies.Split(',')[0];
		PathShows = pathShows.Split(',')[0];
		PathUnknown = pathUnknown;

		FileManager = new FileTools();
	}

	public bool CheckDownloadsAvaialble()
	{
		string[] files = Directory.GetFiles(PathDownload);
		string[] directories = Directory.GetDirectories(PathDownload);

		if (files.Length == 0 && directories.Length == 0)
		{
			Debug(job, "Nothing to refactor");
			return false;
		}
		job.BackgroundTask = false;
		Debug(job, $"Found {files.Length} Files and {directories.Length} Directories in {PathDownload}");
		Debug(job, $"Files -> {String.Join(",", files)}");
		Debug(job, $"Directories -> {String.Join(",", directories)}");
		return true;
	}

	public void Relocate(Job job)
	{
		if (!CheckDownloadsAvaialble(job))
		{
			Inform(job, "No New Downloads");
		}
		string sendString = "New Additions:";

		Debug(job, $"Sorting external files.");
		string[] files = Directory.GetFiles(PathDownload);
		string[] directories = Directory.GetDirectories(PathDownload);

		sendString += SortTorrentFiles(job, files, directories);

		Debug(job, $"Sorting Folders");
		foreach (string directory in directories)
		{
			string[] files1 = Directory.GetFiles(directory);
			string[] directories1 = Directory.GetDirectories(directory);
			Debug(job, $"Found {files1.Length} files and {directories1.Length} directories in {directory}");
			sendString += SortTorrentFiles(job, files1, directories1);
			DeleteSubDirectoriesIfEmpty(job, directory);
		}
		Inform(job, sendString);
	}

	private void DeleteSubDirectoriesIfEmpty(Job job, string startLocation)
	{
		foreach (var directory in Directory.GetDirectories(startLocation))
		{
			DeleteSubDirectoriesIfEmpty(job, directory);
			if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
			{
				try
				{
					Directory.Delete(directory, false);
				}
				catch (DirectoryNotFoundException)
				{
					Log(job, $"Delete failed: {directory}");
				}
			}
			if (Directory.GetFiles(startLocation).Length == 0 && Directory.GetDirectories(startLocation).Length == 0)
			{
				Directory.Delete(startLocation, false);
			}
		}
	}

	private (bool isSameBaseName, VideoType videoType, string baseName) CheckIfSameBaseName(Job job, IEnumerable<string> files)
	{
		List<string> availableBaseNames = [];

		VideoType lastValidVideoType = VideoType.OTHER;
		string lastBaseName = string.Empty;

		foreach (string fileName in files)
		{
			(FileType fileType, VideoType videoType, string baseName, _, _) = MediaTools.BreakdownTorrentFileName(job, fileName);
			string checkFile = $"{videoType}-{baseName}";
			if (fileType == FileType.VIDEO && !availableBaseNames.Contains(checkFile))
			{
				availableBaseNames.Add(checkFile);
				lastValidVideoType = videoType;
				lastBaseName = baseName;
			}
		}

		if (availableBaseNames.Count == 1)
		{
			Debug(job, $"Group of videos in directory belongs to {lastBaseName}.");
			return (true, lastValidVideoType, lastBaseName);
		}
		else if (availableBaseNames.Count == 0)
		{
			// todo:
		}
		Debug(job, $"Group of videos in directory are mixed. -> {String.Join(",", availableBaseNames)}");
		return (false, VideoType.OTHER, lastBaseName);
	}

	private string SortTorrentFiles(Job job, IEnumerable<string> files, IEnumerable<string> directories)
	{
		string refactoredFiles = string.Empty;

		(bool isSameBaseName, VideoType sameVideoType, string SameBaseName) = CheckIfSameBaseName(job, files);

		if (isSameBaseName)
		{
			foreach (string fileName in files)
			{
				Debug(job, $"Sorting file {fileName} in base {SameBaseName}.");
				(FileType fileType, _, _, _, _) = MediaTools.BreakdownTorrentFileName(job, fileName);
				refactoredFiles += MoveFile(job, fileType, sameVideoType, SameBaseName, fileName);
			}
			foreach (string directory in directories)
			{
				Debug(job, $"Sorting folder {directory}");
				string[] files1 = Directory.GetFiles(directory);
				string[] directories1 = Directory.GetDirectories(directory);

				if (directory.Contains("Subs"))
				{
					Debug(job, $"Moving subs folder of {SameBaseName}.");
					MoveSubsFolder(job, directory, sameVideoType, SameBaseName);
				}

				Debug(job, $"Found {files1.Length} files and {directories1.Length} directories in {directory}");

				refactoredFiles += SortTorrentFiles(job, files1, directories1);
			}
		}
		else
		{
			foreach (string fileName in files)
			{
				Debug(job, $"Sorting file {fileName}.");
				(FileType fileType, VideoType videoType, string baseName, _, _) = MediaTools.BreakdownTorrentFileName(job, fileName);
				refactoredFiles += MoveFile(job, fileType, videoType, baseName, fileName);
			}
		}
		return refactoredFiles;
	}

	private string MoveFile(Job job, FileType fileType, VideoType destinationFolder, string baseName, string fileName)
	{
		Debug(job, $"Preparing to move File, {fileName}.");

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

		FileManager.CreateFolderIfNotExist(job, newLocation);
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
		Log(job, $"Moved '{fileName}' -> '{newDesitnation}'");
		return refactoredFiles;
	}

	private void MoveSubsFolder(Job job, string subsFolder, VideoType destinationFolder, string baseName)
	{
		string[] subFiles = Directory.GetFiles(subsFolder);
		Debug(job, $"Found {subFiles.Length} Subtitle Files for {baseName}.");

		string? destination = null;

		if (destinationFolder == VideoType.MOVIE)
		{
			destination = Path.Combine(PathMovies, baseName, "Subs");
			FileManager.CreateFolderIfNotExist(job, destination);
		}
		else if (destinationFolder == VideoType.SHOW)
		{
			destination = Path.Combine(PathShows, baseName, "Subs");
			FileManager.CreateFolderIfNotExist(job, destination);
		}

		if (destination != null)
		{
			foreach (var file in subFiles)
			{
				Debug(job, $"Found Sub File {file}");
				var filePath = Path.Combine(subsFolder, file);
				var destinationPath = Path.Combine(destination, file);

				File.Move(filePath, destinationPath);
				Log(job, $"Moved {filePath} -> {destinationPath}");
			}
		}
		DeleteSubDirectoriesIfEmpty(job, subsFolder);
	}
}
