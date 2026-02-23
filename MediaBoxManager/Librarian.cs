using Logger;
using MediaBoxDatabaseModels;
using MediaBoxManager.Enum;
using System;
using System.Collections.Generic;
using System.Text;
using Transmission.API.RPC.Entity;

namespace MediaBoxManager;

public class Librarian
{
	MovieDataAccess MovieDB;
	TvShowDataAccess TvShowDB;

	private readonly List<string> movieLocations;
	private readonly List<string> tvShowLocations;

	public Librarian(string connectionString, string movieLocations, string tvShowLocations)
	{
		MovieDB = new(connectionString);
		TvShowDB = new(connectionString);
		this.movieLocations = movieLocations.Split(',').ToList();
		this.tvShowLocations = tvShowLocations.Split(',').ToList();
	}

	public void UpdateLibrary()
	{
		MovieDB.UpdateColumnValueAsync("exist", 0).Wait();
		foreach (string movieLocation in movieLocations)
		{
			new SqliteLogger().Info($"Scanning Location: {movieLocation}");
			UpdateMovieLibrary(movieLocation);
		}

		TvShowDB.UpdateColumnValueAsync("exist", 0).Wait();
		foreach (string tvShowLocation in tvShowLocations)
		{
			new SqliteLogger().Info($"Scanning Location: {tvShowLocation}");
			UpdateTVShowLibrary(tvShowLocation);
		}
	}

	private void MarkMovie(Torrent torrent, string path = "Downloading...")
	{
		List<Movie> movies = MovieDB.GetByColumnAsync("movie", torrent.BaseName.Replace("'", "''")).Result;
		if (movies.Count > 0)
		{
			Movie movie = movies[0];
			movie.Exists = true;
			movie.Path = path;
			MovieDB.UpdateAsync(movie).Wait();
		}
		else
		{
			MovieDB.InsertAsync(new Movie
			{
				MovieName = torrent.BaseName,
				Magnet = torrent.Magnet!,
				Quality = torrent.Quality,
				Path = path,
				Exists = true
			}).Wait();
			new SqliteLogger().Info($"Show Added to DB: {torrent.GetLogDetails}\t{path}");
		}
	}

	private void MarkTVShow(Torrent torrent, string path = "Downloading...")
	{
		List<TvShow> tvShows = TvShowDB.GetByColumnsAsync(new(){
						{ "tv_show", torrent.BaseName.Replace("'", "''") },
						{ "season", torrent.Season },
						{ "episode", torrent.Episode }
					}).Result;
		if (tvShows.Count > 0)
		{
			TvShow tvShow = tvShows[0];
			tvShow.Exists = true;
			tvShow.Path = path;
			TvShowDB.UpdateAsync(tvShow).Wait();
		}
		else
		{
			TvShowDB.InsertAsync(new TvShow
			{
				ShowName = torrent.BaseName,
				Season = torrent.Season,
				Episode = torrent.Episode,
				Magnet = torrent.Magnet!,
				Quality = torrent.Quality,
				Path = path,
				Exists = true
			}).Wait();
			new SqliteLogger().Info($"Show Added to DB: {torrent.GetLogDetails}\t{path}");
		}
	}

	public void UpdateTorrents(List<TorrentInfo> torrents)
	{
		foreach (TorrentInfo torrent in torrents)
		{
			Torrent torrentItem = MediaTools.BreakdownTorrentFileName(torrent.Name);
			torrentItem.Magnet = torrent.MagnetLink;

			if (torrentItem.IsMovie)
			{
				MarkMovie(torrentItem);
			}

			else if (torrentItem.IsTvShow)
			{
				MarkTVShow(torrentItem);
			}
		}
	}

	private void UpdateMovieLibrary(string movieLoc)
	{
		List<string> movieList = MediaTools.GetAllVideoFiles(movieLoc);
		foreach (string movie in movieList)
		{
			Torrent torrent = MediaTools.BreakdownTorrentFileName(movie.Split("/").Last());

			if (torrent.FileType == FileType.VIDEO)
			{
				MarkMovie(torrent, movie.Replace(movieLoc, ""));
			}
		}
	}

	private void UpdateTVShowLibrary(string tvShowLoc)
	{
		List<string> tvShowList = MediaTools.GetAllVideoFiles(tvShowLoc);
		foreach (string tvShow in tvShowList)
		{
			Torrent torrent = MediaTools.BreakdownTorrentFileName(tvShow.Split("/").Last());

			if (torrent.FileType == FileType.VIDEO)
			{
				MarkTVShow(torrent, tvShow.Replace(tvShowLoc, ""));
			}

		}
	}
}
