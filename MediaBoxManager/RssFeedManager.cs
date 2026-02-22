using Logger;
using MediaBoxDatabaseModels;
using MediaBoxManager.Enum;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace MediaBoxManager;

public static class RssFeedManager
{
	internal static Dictionary<string, Show> GetShows(TvShowDataAccess TvShowDB, Dictionary<string, string> source)
	{
		Dictionary<string, Show> results = [];

		IEnumerable<FeedEntry> feed = ParseFeed(source["feed"]);
		foreach (var entry in feed)
		{
			(VideoType type, string baseName, (int season, int episode), string quality) = MediaTools.BreakdownVideoTitle(entry.Title);

			if (type != VideoType.SHOW)
			{
				new SqliteLogger().Info($"{entry.Title} is not a TV show and is ignored.");
				continue;
			}

			Dictionary<string, object> dataToCheck = new(){
					{ "tv_show", baseName },
					{ "season", season },
					{ "episode", episode }
				};

			var taskCheckExist = TvShowDB.CombinationExistsAsync(dataToCheck);
			bool showExists = taskCheckExist.Result;

			new SqliteLogger().Info($"Found show {baseName} S{season:00}E{episode:00} -> Exists = {showExists}");

			if (!showExists)
			{
				List<TvShow> lastSeasonTable = TvShowDB.CustomSelectAsync($"tv_show = '{baseName}'", "season DESC", 1).Result;
				int lastDownloadedSeason = 0;
				if (lastSeasonTable.Count > 0)
				{
					lastDownloadedSeason = Convert.ToInt32(lastSeasonTable.FirstOrDefault()!.Season);
				}
				if (season != 0 && season < lastDownloadedSeason)
				{
					new SqliteLogger().Info($"Skipping {baseName} S{season:00}E{episode:00} as a higher season ({lastDownloadedSeason}) exists.");
					continue;
				}

				string searchParam = $"{baseName}_{season}_{episode}";
				if (results.ContainsKey(searchParam))
				{
					if (MediaTools.QualityScore(results[searchParam].Quality) > MediaTools.QualityScore(quality))
					{
						results[searchParam] = new Show()
						{
							BaseName = baseName,
							Season = season,
							Episode = episode,
							Magnet = entry.Link,
							Quality = quality
						};
						new SqliteLogger().Info($"Updated Download: {baseName} S{season:00}E{episode:00}");
					}
				}
				else
				{
					results.Add(searchParam, new Show()
					{
						BaseName = baseName,
						Season = season,
						Episode = episode,
						Magnet = entry.Link,
						Quality = quality
					});
					new SqliteLogger().Info($"Added Download: {baseName} S{season:00}E{episode:00}");
				}
			}
		}
		return results;
	}

	private static List<FeedEntry> ParseFeed(string feedUrl)
	{
		using var client = new HttpClient();
		XDocument doc;

		try
		{
			var response = client.GetStringAsync(feedUrl).Result;
			doc = XDocument.Parse(response);
		}
		catch
		{
			throw new MissingFieldException($"Error reading {feedUrl}");
		}

		new SqliteLogger().Info($"Successfully extracted feed {feedUrl}");

		return doc.Descendants("item").Select(item => new FeedEntry
		{
			Title = item.Element("title")?.Value ?? string.Empty,
			Link = item.Element("link")?.Value ?? string.Empty,
			TvShowName = item.Element("tv_show_name")?.Value,
			TvEpisodeId = item.Element("tv_episode_id")?.Value
		}).ToList();
	}
}
