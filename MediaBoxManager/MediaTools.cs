using Logger;
using MediaBoxManager.Enum;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaBoxManager;

public static class MediaTools
{
	public static string[] VideoExtensions { get; set; } = ["mp4", "flv", "mkv", "avi", "MP4", "FLV", "MKV", "AVI"];


	public static List<string> GetAllVideoFiles(string location)
	{
		List<string> fileList = [];
		foreach (string extension in VideoExtensions)
		{
			fileList.AddRange([.. Directory.GetFiles(location, $"*.{extension}*", System.IO.SearchOption.AllDirectories)]);
		}
		return fileList;
	}

	private static (FileType FileType, string Extension) GetFileType(string fileName)
	{
		string extension = fileName.Split('.').Last();

		if (extension.Equals("srt", StringComparison.OrdinalIgnoreCase))
		{
			return (FileType.SUBTITLE, extension);
		}
		else if (VideoExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
		{
			return (FileType.VIDEO, extension);
		}
		else
		{
			return (FileType.OTHER, extension);
		}
	}
	private static (bool Success, int Season, int Episode, int MatchStart, int MatchEnd) MatchShowEpisode(string fileName)
	{
		int season = 0;
		int episode = 0;
		bool success = false;
		int matchStart = 0;
		int matchEnd = 0;

		Match match1 = Regex.Match(fileName, "S([0-9][0-9])E([0-9][0-9])", RegexOptions.IgnoreCase);
		Match match2 = Regex.Match(fileName, "Ep\\. (\\d+)-\\d+", RegexOptions.IgnoreCase);
		Match match3 = Regex.Match(fileName, "S([0-9][0-9]) E([0-9][0-9])", RegexOptions.IgnoreCase);
		Match match4 = Regex.Match(fileName, "S([0-9][0-9])x([0-9][0-9])", RegexOptions.IgnoreCase);
		Match match5 = Regex.Match(fileName, "([0-9][0-9])x([0-9][0-9])", RegexOptions.IgnoreCase);
		Match match6 = Regex.Match(fileName, "([0-9])x([0-9][0-9])", RegexOptions.IgnoreCase);

		if (match1.Success)
		{
			success = true;
			season = int.Parse(match1.Groups[1].Value);
			episode = int.Parse(match1.Groups[2].Value);
			matchStart = match1.Index;
			matchEnd = match1.Index + match1.Length;
		}
		else if (match2.Success)
		{
			success = true;
			season = 0;
			episode = int.Parse(match2.Groups[1].Value);
			matchStart = match2.Index;
			matchEnd = match2.Index + match2.Length;
		}
		else if (match3.Success)
		{
			success = true;
			season = int.Parse(match3.Groups[1].Value);
			episode = int.Parse(match3.Groups[2].Value);
			matchStart = match3.Index;
			matchEnd = match3.Index + match3.Length;
		}
		else if (match4.Success)
		{
			success = true;
			season = int.Parse(match4.Groups[1].Value);
			episode = int.Parse(match4.Groups[2].Value);
			matchStart = match4.Index;
			matchEnd = match4.Index + match4.Length;
		}
		else if (match5.Success)
		{
			success = true;
			season = int.Parse(match5.Groups[1].Value);
			episode = int.Parse(match5.Groups[2].Value);
			matchStart = match5.Index;
			matchEnd = match5.Index + match5.Length;
		}
		else if (match6.Success)
		{
			success = true;
			season = int.Parse(match6.Groups[1].Value);
			episode = int.Parse(match6.Groups[2].Value);
			matchStart = match6.Index;
			matchEnd = match6.Index + match6.Length;
		}
		else
		{
			new SqliteLogger().Info($"{fileName}: Not a TV show");
		}
		return (success, season, episode, matchStart, matchEnd);
	}
	internal static Torrent BreakdownTorrentFileName(string fileName)
	{
		fileName = Path.GetFileName(fileName);
		VideoType videoType = VideoType.OTHER;
		(FileType fileType, _) = GetFileType(fileName);
		string baseName = string.Empty;
		int season = 0;
		int episode = 0;
		string quality = string.Empty;

		if (fileType == FileType.VIDEO || fileType == FileType.SUBTITLE)
		{
			(videoType, baseName, (season, episode), quality) = BreakdownVideoTitle(fileName);
		}
		return new Torrent()
		{ 
			TorrentName = fileName,
			BaseName = baseName,
			Season = season,
			Episode = episode,
			Quality = quality,
			FileType = fileType,
			VideoType = videoType
		};
	}
	public static (VideoType videoType, string baseName, (int season, int episode), string quality) BreakdownVideoTitle(string fileName)
	{
		VideoType videoType;
		string baseName;

		bool isShow;
		string[] noWords = ["EXTENDED", "REMASTERED", "REPACK", "BLURAY", "Dir Cut", "IMAX", "EDITION", "BRRIP", "DVDRip", "(", ")", "-", "Ep.", "www.torrenting.com - "];

		(isShow, int season, int episode, int matchStart, _) = MatchShowEpisode(fileName);
		Match matchQuality = Regex.Match(fileName, "[0-9][0-9][0-9]p", RegexOptions.IgnoreCase);
		string quality = matchQuality.Value;

		if (isShow)
		{
			videoType = VideoType.SHOW;
			baseName = fileName[..matchStart];
		}
		else
		{
			videoType = VideoType.MOVIE;
			if (quality == "080p" || quality == "080P")
				baseName = fileName[..(matchQuality.Index - 1)];
			else
				baseName = fileName[..matchQuality.Index];
		}

		baseName = RemoveWords(baseName, noWords).Replace("&", " and ");

		baseName = new CultureInfo("en-US", false).TextInfo.ToTitleCase(baseName
			.Replace(".", " ").Replace("+", " ").Replace("!", "")
			.Replace("(", "").Replace(")", "").Replace("'", "")
			.Replace("-", " ").Replace("\"", "").Replace("?", "")
			.TrimEnd()).Trim();
		baseName = Regex.Replace(baseName, @"\s+", " ");

		new SqliteLogger().Info($"{fileName} -> {baseName}");
		return (videoType, baseName, (season, episode), quality);
	}

	private static string RemoveWords(string input, string[] wordsToRemove)
	{
		foreach (string word in wordsToRemove)
		{
			input = Regex.Replace(input, $"\\b{Regex.Escape(word)}\\b", string.Empty, RegexOptions.IgnoreCase).Trim();
		}
		return input;
	}

	internal static int QualityScore(string quality)
	{
		if (quality == "240p")
			return 0;
		else if (quality == "360p")
			return 1;
		else if (quality == "480p")
			return 2;
		else if (quality == "720p")
			return 3;
		else if (quality == "080p")
			return 4;
		else
			return 10;
	}
}
