using MediaBoxManager.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBoxManager;

internal class Torrent
{
	public required string TorrentName { get; set; }
	public required string BaseName { get; set; }
	public int Season { get; set; }
	public int Episode { get; set; }
	public required string Quality { get; set; }
	public FileType FileType { get; set; }
	public VideoType VideoType { get; set; }
	public string? Magnet { get; set; }

	public bool IsMovie
	{
		get 
		{ 
			return (FileType == FileType.VIDEO && VideoType == VideoType.MOVIE); 
		}
	}

	public bool IsTvShow
	{
		get
		{
			return (FileType == FileType.VIDEO && VideoType == VideoType.SHOW);
		}
	}

	public string GetLogDetails()
	{
		if (IsMovie)
			return $"{VideoType}\t{BaseName}\t{Quality}";
		else
			return $"{VideoType}\t{BaseName}\tS{Season}E{Episode}\t{Quality}";
	}

}
