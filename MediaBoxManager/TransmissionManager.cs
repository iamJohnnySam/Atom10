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
	private readonly string url;
	private readonly SqliteLogger _logger;

	public TransmissionManager(MagnetDataAccess magnetDataAccess, string url, string username = "", string password = "")
	{
		MagnetDB = magnetDataAccess;
		_client = new(url, username, password);
		this.url = url;
		_logger = new SqliteLogger();
	}

	public async Task<bool> CheckIfExists(string name, string magnetLink)
	{
		string showName = name.Replace("'", "''");
		List<Magnet> results = await (MagnetDB.GetByColumnAsync(nameof(Magnet.MagnetName), showName));
		if (results.Count == 0)
		{
			return false;
		}
		{
			Magnet magnet = results[0];
			magnet.Count += 1;
			_logger.Info($"{name} download attempt count now at {magnet.Count}");
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
			_logger.Error($"Error adding torrent {torrentName}: {torrentPathOrMagnet}");
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

			_logger.Debug($"Added torrent {result.ID}: {result.Name}");
			_logger.Info($"{result.Name} started downloading");
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
			_logger.Error($"Transmission Connect Failure: {url}");
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
			_logger.Info($"ID: {torrent.ID}, Name: {torrent.Name}, Status: {torrent.Status}, Progress: {torrent.PercentDone:P}");
		}
	}

	public void DeleteTorrentsIfDone()
	{
		foreach (TorrentInfo torrent in GetTorrents())
		{
			_logger.Info($"{torrent.Name} -> {torrent.Status} \t(% done = {torrent.PercentDone})");
			if (torrent.PercentDone == 1)
			{
				DeleteTorrent(torrent.ID);
				_logger.Debug($"{torrent.Name} Deleted");
			}
			Thread.Sleep(1000);
		}
	}
}
