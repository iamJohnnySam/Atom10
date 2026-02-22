using Logger;
using MediaBoxDatabaseModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace MediaBoxManager;

public class ShowScanner
{
	public TvShowDataAccess TvShowDB { get; }

	private readonly Dictionary<string, Dictionary<string, string>> _sources;

	public ShowScanner(string connectionString)
	{
		TvShowDB = new(connectionString);

		string jsonContent = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sources.json"));
		_sources = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonContent) ?? throw new Exception("Error getting RSS feed sources");
	}

	internal Dictionary<string, Show> CheckShows()
	{
		Dictionary<string, Show> results = [];
		foreach (var source in _sources)
		{
			new SqliteLogger().Info($"Check Shows Started for {source.Key}");
			try
			{
				results = results.Concat(RssFeedManager.GetShows(TvShowDB, source.Value))
				.GroupBy(pair => pair.Key)
				.Where(group => group.Count() == 1)
				.ToDictionary(group => group.Key, group => group.First().Value);
			}
			catch (Exception e)
			{
				new SqliteLogger().Info($"Connection failed to {source.Key}: {e}");
			}
			new SqliteLogger().Info($"Check Shows Ended for {source.Key}");
		}
		new SqliteLogger().Info("TV Show Scan Completed");
		return results;
	}
}
