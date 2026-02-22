using MediaBoxManager.Enum;
using System;
using System.Collections.Generic;
using System.Text;
using Transmission.API.RPC.Entity;

namespace MediaBoxManager;

public class Librarian
{
	private readonly DBManager db;
	private readonly string movieTable;
	private readonly List<string> movieLocations;
	private readonly string tvShowTable;
	private readonly List<string> tvShowLocations;
	readonly MediaTools mediaTools;

	public Librarian(DBManager database, string movieTable, string movieLocations, string tvShowTable, string tvShowLocations)
	{
		this.db = database;
		this.movieTable = movieTable;
		this.movieLocations = movieLocations.Split(',').ToList();
		this.tvShowTable = tvShowTable;
		this.tvShowLocations = tvShowLocations.Split(',').ToList();

		mediaTools = new MediaTools();
		mediaTools.OnLogEvent += DebugEvent;
	}

	public void UpdateLibrary()
	{
		db.UpdateColumnValueAsync(movieTable, "exist", 0).Wait();
		foreach (string movieLocation in movieLocations)
		{
			Log(job, $"Scanning Location: {movieLocation}");
			UpdateMovieLibrary(job, movieLocation);
		}

		db.UpdateColumnValueAsync(tvShowTable, "exist", 0).Wait();
		foreach (string tvShowLocation in tvShowLocations)
		{
			Log(job, $"Scanning Location: {tvShowLocation}");
			UpdateTVShowLibrary(job, tvShowLocation);
		}
	}

	public void UpdateTorrents(Job job, List<TorrentInfo> torrents)
	{
		foreach (TorrentInfo torrent in torrents)
		{
			(FileType fileType, VideoType videoType, string baseName, (int season, int episode), string quality) = mediaTools.BreakdownTorrentFileName(job, torrent.Name);

			if (fileType == FileType.VIDEO && videoType == VideoType.MOVIE)
			{
				Dictionary<string, object> dataToCheck = new() { { "movie", baseName.Replace("'", "''") } };
				var taskCheckExist = db.CombinationExistsAsync(movieTable, dataToCheck);
				bool movieExists = taskCheckExist.Result;

				if (movieExists)
				{
					Dictionary<string, object> existsDict = new() { { "exist", 1 } };
					db.UpdateAsync(movieTable, existsDict, $"movie = '{baseName.Replace("'", "''")}'").Wait();
				}
				else
				{
					db.InsertAsync(movieTable, new Dictionary<string, object>
						{
							{ "movie", baseName },
							{ "magnet", torrent.MagnetLink },
							{ "quality", quality }
						}).Wait();
					Log(job, $"Show Added to DB: {videoType}\t{baseName}\t{quality}");
				}
			}

			else if (fileType == FileType.VIDEO && videoType == VideoType.SHOW)
			{
				Dictionary<string, object> dataToCheck = new(){
						{ "tv_show", baseName.Replace("'", "''") },
						{ "season", season },
						{ "episode", episode }
					};
				var taskCheckExist = db.CombinationExistsAsync(tvShowTable, dataToCheck);
				bool showExists = taskCheckExist.Result;

				if (showExists)
				{
					Dictionary<string, object> existsDict = new() { { "exist", 1 } };
					db.UpdateAsync(tvShowTable, existsDict, $"tv_show = '{baseName.Replace("'", "''")}' AND season = '{season}' AND episode = '{episode}'").Wait();
				}
				else
				{
					db.InsertAsync(tvShowTable, new Dictionary<string, object>
						{
							{ "tv_show", baseName },
							{ "season", season },
							{ "episode", episode },
							{ "magnet", torrent.MagnetLink },
							{ "quality", quality }
						}).Wait();
					Log(job, $"Show Added to DB: {videoType}\t{baseName}\t{season}x{episode}\t{quality}");
				}
			}
		}
	}

	private void UpdateMovieLibrary(Job job, string movieLoc)
	{
		List<string> movieList = mediaTools.GetAllVideoFiles(movieLoc);
		foreach (string movie in movieList)
		{
			(FileType fileType, VideoType videoType, string baseName, (_, _), string quality) = mediaTools.BreakdownTorrentFileName(job, movie.Split("/").Last());

			if (fileType == FileType.VIDEO)
			{
				Dictionary<string, object> dataToCheck = new() { { "movie", baseName.Replace("'", "''") } };
				var taskCheckExist = db.CombinationExistsAsync(movieTable, dataToCheck);
				bool movieExists = taskCheckExist.Result;

				if (movieExists)
				{
					Dictionary<string, object> existsDict = new() { { "exist", 1 }, { "path", movie.Replace(movieLoc, "") } };
					db.UpdateAsync(movieTable, existsDict, $"movie = '{baseName.Replace("'", "''")}'").Wait();
				}
				else
				{
					db.InsertAsync(movieTable, new Dictionary<string, object>
						{
							{ "movie", baseName },
							{ "path", movie.Replace(movieLoc, "") },
							{ "quality", quality }
						}).Wait();
					Log(job, $"Show Added: {videoType}\t{baseName}\t{quality}\t{movie.Replace(movieLoc, "")}");
				}
			}
		}
	}

	private void UpdateTVShowLibrary(Job job, string tvShowLoc)
	{
		List<string> tvShowList = mediaTools.GetAllVideoFiles(tvShowLoc);
		foreach (string tvShow in tvShowList)
		{
			(FileType fileType, VideoType videoType, string baseName, (int season, int episode), string quality) = mediaTools.BreakdownTorrentFileName(job, tvShow.Split("/").Last());

			if (fileType == FileType.VIDEO)
			{
				Dictionary<string, object> dataToCheck = new(){
						{ "tv_show", baseName.Replace("'", "''") },
						{ "season", season },
						{ "episode", episode }
					};
				var taskCheckExist = db.CombinationExistsAsync(tvShowTable, dataToCheck);
				bool showExists = taskCheckExist.Result;

				if (showExists)
				{
					Dictionary<string, object> existsDict = new() { { "exist", 1 }, { "path", tvShow.Replace(tvShowLoc, "") } };
					db.UpdateAsync(tvShowTable, existsDict, $"tv_show = '{baseName.Replace("'", "''")}' AND season = '{season}' AND episode = '{episode}'").Wait();
				}
				else
				{
					db.InsertAsync(tvShowTable, new Dictionary<string, object>
						{
							{ "tv_show", baseName },
							{ "season", season },
							{ "episode", episode },
							{ "path", tvShow.Replace(tvShowLoc, "") },
							{ "quality", quality }
						}).Wait();
					Log(job, $"Show Added: {videoType}\t{baseName}\t{season}x{episode}\t{quality}\t{tvShow.Replace(tvShowLoc, "")}");
				}
			}

		}
	}
}
