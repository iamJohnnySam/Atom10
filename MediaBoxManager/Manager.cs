using DataManagement;
using Logger;
using MediaBoxDatabaseModels;
using Utilities;
using Transmission.API.RPC.Entity;

namespace MediaBoxManager;

public class Manager
{
	private readonly string _connectionString;
	private readonly Scheduler _scheduler;
	private readonly AtomConfiguration _config;
	private readonly SqliteLogger logger;

	public Manager()
	{
		_connectionString = new SqliteConnectionString("MediBoxDB").ConnectionString;
		_config = new(typeof(Manager));
		_scheduler = new ();

		SetBasicSchedule();
		
		logger = new SqliteLogger();
		logger.Debug($"Manager Created");

		CleanTorrents();
		logger.Debug($"Cleaning Completed...");
		ScanNewShows();
	}

	void SetBasicSchedule()
	{
		_scheduler.AddDailyTask(new TimeSpan(6, 30, 0), () =>
		{
			CleanTorrents();
			ScanNewShows();
		});
		_scheduler.AddDailyTask(new TimeSpan(8, 0, 0), () =>
		{
			CleanTorrents();
		});
		_scheduler.AddDailyTask(new TimeSpan(4, 0, 0), () =>
		{
			CleanTorrents();
		});
		_scheduler.Start();
	}


	void ScanNewShows()
	{
		TvShowDataAccess tvShowDataAccess = new(_connectionString);
		MagnetDataAccess magnetDataAccess = new(_connectionString);

		ShowScanner showScanner = new(tvShowDataAccess);
		TransmissionManager tm = new(magnetDataAccess, _config.GetField("TRANSMISSION_CONNECT"));

		try
		{
			Dictionary<string, Show> tv_shows = showScanner.CheckShows();
			foreach (string show in tv_shows.Keys)
			{
				try
				{
					if (tm.CheckIfExists($"{tv_shows[show].BaseName} {tv_shows[show].Season}x{tv_shows[show].Episode}", tv_shows[show].Magnet).Result)
					{
						logger.Info($"The torrent {tv_shows[show].BaseName} was previously added. Now skipped.");
						continue;
					}
					(int tmID, string tmName) = tm.AddTorrent(tv_shows[show].Magnet, $"{tv_shows[show].BaseName} {tv_shows[show].Season}x{tv_shows[show].Episode}");
					logger.Info($"{tv_shows[show].BaseName} {tv_shows[show].Season}x{tv_shows[show].Episode} added: {tmID}.");
				}
				catch (Exception ex)
				{
					logger.Error(ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex.Message);
		}
	}

	void CleanTorrents()
	{
		MagnetDataAccess magnetDataAccess = new(_connectionString);
		TransmissionManager tm = new(magnetDataAccess, _config.GetField("TRANSMISSION_CONNECT"));


		MediaHandler mediaHandler = new(_config.GetField("MEDIA_DOWNLOADS"), _config.GetField("MEDIA_MOVIES"), _config.GetField("MEDIA_SHOWS"), _config.GetField("MEDIA_UNKNOWN"));

		tm.DeleteTorrentsIfDone();
		mediaHandler.Relocate();
		UpdateLibraryWithTorrents(tm.GetTorrents());
	}

	void UpdateLibrary()
	{
		TvShowDataAccess tvShowDataAccess = new(_connectionString);
		MovieDataAccess movieDataAccess = new(_connectionString);
		Librarian libraryUpdater = new(movieDataAccess, tvShowDataAccess, _config.GetField("MEDIA_MOVIES"), _config.GetField("MEDIA_SHOWS"));

		libraryUpdater.UpdateLibrary();
	}
	void UpdateLibraryWithTorrents(List<TorrentInfo> torrents)
	{
		TvShowDataAccess tvShowDataAccess = new(_connectionString);
		MovieDataAccess movieDataAccess = new(_connectionString);
		Librarian libraryUpdater = new(movieDataAccess, tvShowDataAccess, _config.GetField("MEDIA_MOVIES"), _config.GetField("MEDIA_SHOWS"));

		libraryUpdater.UpdateLibrary();
		libraryUpdater.UpdateTorrents(torrents);
	}
}
