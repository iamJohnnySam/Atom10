using Logger;
using MediaBoxDatabaseModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Transmission.API.RPC;
using Transmission.API.RPC.Entity;

namespace MediaBoxManager;

public class TransmissionManager
{
	private readonly MagnetDataAccess MagnetDB;
	private readonly Client _client;

	public TransmissionManager(string connectionString, string url, string username = "", string password = "")
	{
		MagnetDB = new(connectionString);
		_client = new(url, username, password);
	}

	public async Task<bool> CheckIfExists(string name, string magnetLink)
	{
		string showName = name.Replace("'", "''");
		List<Magnet> results = await (MagnetDB.GetByColumnAsync("name", showName));
		if (results.Count == 0)
		{
			return false;
		}
		{
			Magnet magnet = results[0];
			magnet.Count += 1;
			new SqliteLogger().Info($"{name} download attempt count now at {magnet.Count}");
			await MagnetDB.UpdateAsync(magnet);
			return true;
		}
	}

	public (int id, string name) AddTorrent(string torrentPathOrMagnet, string? torrentName)
	{
		NewTorrentInfo result;
		try
		{
			result = _client.TorrentAdd(new NewTorrent { Filename = torrentPathOrMagnet });
		}
		catch (UriFormatException e)
		{
			throw new Exception($"Error adding torrent {torrentName}: {torrentPathOrMagnet}", e);
		}

		if (result.ID != 0)
		{
			MagnetDB.InsertAsync(new Magnet
			{
				MagnetName = torrentName,
				MagnetLink = torrentPathOrMagnet,
				Count = 1
			}).Wait();

			new SqliteLogger().Info($"Added torrent {result.ID}: {result.Name}");
			new SqliteLogger().Info($"{result.Name} started downloading");
			return (result.ID, result.Name);
		}
		throw new Exception($"Error adding torrent {torrentName}: {torrentPathOrMagnet}");
	}

	public List<TorrentInfo> GetTorrents()
	{
		var fields = new string[] { "id", "name", "status", "percentDone" };
		TransmissionTorrents torrents;
		try
		{
			torrents = _client.TorrentGet(fields);
		}
		catch (Exception e)
		{
			throw new Exception($"Transmission Connect Failure", e);
		}
		if (torrents != null && torrents.Torrents.Length > 0)
		{

			return [.. torrents.Torrents];
		}
		return [];
	}

	public void StartTorrent(int torrentId)
	{
		_client.TorrentStart([torrentId]);
	}
	public void StopTorrent(int torrentId)
	{
		_client.TorrentStop([torrentId]);
	}

	public void DeleteTorrent(int torrentId, bool deleteData = false)
	{
		_client.TorrentRemove([torrentId], deleteData);
	}

	public void PrintTorrents()
	{
		var torrents = GetTorrents();

		foreach (var torrent in torrents)
		{
			new SqliteLogger().Info($"ID: {torrent.ID}, Name: {torrent.Name}, Status: {torrent.Status}, Progress: {torrent.PercentDone:P}");
		}
	}

	public void DeleteTorrentsIfDone()
	{
		foreach (TorrentInfo torrent in GetTorrents())
		{
			new SqliteLogger().Info($"{torrent.Name} -> {torrent.Status} (% done = {torrent.PercentDone})");
			if (torrent.PercentDone == 1)
			{
				DeleteTorrent(torrent.ID);
				new SqliteLogger().Info($"{torrent.Name} Deleted");
			}
			Thread.Sleep(1000);
		}
	}
}
