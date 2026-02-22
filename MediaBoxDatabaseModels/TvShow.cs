using DataManagement;
using DataManagement.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;

namespace MediaBoxDatabaseModels;

public class TvShow
{
	[Key]
	public int TvShowId { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.Now;
	public string ShowName { get; set; } = "UnNamed Show";
	public string ProperName { get; set; } = string.Empty;
	public int Season { get; set; }
	public int Episode { get; set; }
	public string Magnet { get; set; } = string.Empty;
	public string Quality { get; set; } = string.Empty;
	public bool Exists { get; set; } = true;
	public string Path { get; set; } = string.Empty;


	public static TableMetadata Metadata => new(
		typeof(TvShow).Name,
		new Dictionary<string, EDataType>
		{
			{ nameof(TvShowId), EDataType.Key },
			{ nameof(Timestamp), EDataType.Date },
			{ nameof(ShowName), EDataType.Text },
			{ nameof(ProperName), EDataType.Text },
			{ nameof(Season), EDataType.Integer },
			{ nameof(Episode), EDataType.Integer },
			{ nameof(Magnet), EDataType.Text },
			{ nameof(Quality), EDataType.Text },
			{ nameof(Exists), EDataType.Boolean },
			{ nameof(Path), EDataType.Text }
		},
		nameof(ShowName)
	);
}
