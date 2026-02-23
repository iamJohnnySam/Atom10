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
	private readonly Configuration _config;

	public Manager()
	{
		_connectionString = new SqliteConnectionString("MediBoxDB").ConnectionString;
		_config = new(typeof(Manager));
		_scheduler = new ();

		new SqliteLogger().Info($"Manager Created");
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
		ShowScanner showScanner = new(_connectionString);

		TransmissionManager tm = new(_connectionString, _config.GetField("TRANSMISSION_CONNECT"));

		try
		{
			Dictionary<string, Show> tv_shows = showScanner.CheckShows();
			foreach (string show in tv_shows.Keys)
			{
				try
				{
					if (tm.CheckIfExists($"{tv_shows[show].BaseName} {tv_shows[show].Season}x{tv_shows[show].Episode}", tv_shows[show].Magnet).Result)
					{
						new SqliteLogger().Info($"The torrent {tv_shows[show].BaseName} was previously added. Now skipped.");
						continue;
					}
					(int tmID, string tmName) = tm.AddTorrent(tv_shows[show].Magnet, $"{tv_shows[show].BaseName} {tv_shows[show].Season}x{tv_shows[show].Episode}");
					new SqliteLogger().Info($"{tv_shows[show].BaseName} {tv_shows[show].Season}x{tv_shows[show].Episode} added: {tmID}.");
				}
				catch (Exception ex)
				{
					new SqliteLogger().Info(ex.Message);
				}
			}
		}
		catch (Exception ex)
		{
			new SqliteLogger().Info(ex.Message);
		}
	}

	void CleanTorrents()
	{
		TransmissionManager tm = new(_connectionString, _config.GetField("TRANSMISSION_CONNECT"));


		MediaHandler mediaHandler = new(_config.GetField("MEDIA_DOWNLOADS"), _config.GetField("MEDIA_MOVIES"), _config.GetField("MEDIA_SHOWS"), _config.GetField("MEDIA_UNKNOWN"));

		tm.DeleteTorrentsIfDone();
		mediaHandler.Relocate();
		UpdateLibraryWithTorrents(tm.GetTorrents());
	}

	void UpdateLibrary()
	{
		Librarian libraryUpdater = new(_connectionString, _config.GetField("MEDIA_MOVIES"), _config.GetField("MEDIA_SHOWS"));

		libraryUpdater.UpdateLibrary();
	}
	void UpdateLibraryWithTorrents(List<TorrentInfo> torrents)
	{
		Librarian libraryUpdater = new(_connectionString, _config.GetField("MEDIA_MOVIES"), _config.GetField("MEDIA_SHOWS"));

		libraryUpdater.UpdateLibrary();
		libraryUpdater.UpdateTorrents(torrents);
	}
}
