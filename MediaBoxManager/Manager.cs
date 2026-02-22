using DataManagement;
using Logger;
using MediaBoxDatabaseModels;
using Utilities;

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


		MediaHandler mediaHandler = new(fd.GetField("MEDIA_DOWNLOADS"), fd.GetField("MEDIA_MOVIES"), fd.GetField("MEDIA_SHOWS"), fd.GetField("MEDIA_UNKNOWN"));
		mediaHandler.OnLogEvent += fd.LogEvent;

		tm.DeleteTorrentsIfDone(job);
		mediaHandler.Relocate(job);
		UpdateLibraryWithTorrents(job, tm.GetTorrents(job));
	}

	void UpdateLibrary()
	{
		Librarian libraryUpdater = new(fd.db, movieTable, fd.GetField("MEDIA_MOVIES"), tvShowTable, fd.GetField("MEDIA_SHOWS"));

		libraryUpdater.UpdateLibrary(job);
	}
	void UpdateLibraryWithTorrents(List<TorrentInfo> torrents)
	{
		Librarian libraryUpdater = new(fd.db, movieTable, fd.GetField("MEDIA_MOVIES"), tvShowTable, fd.GetField("MEDIA_SHOWS"));

		libraryUpdater.UpdateLibrary(job);
		libraryUpdater.UpdateTorrents(job, torrents);
	}
}
