using DataManagement;
using DataManagement.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MediaBoxDatabaseModels;

public class Magnet
{
	[Key]
	public int MagnetId { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.Now;
	public string MagnetName { get; set; } = "UnNamed Show";
	public string MagnetLink { get; set; } = string.Empty;
	public int Count { get; set; } = 1;


	public static TableMetadata Metadata => new(
		typeof(Movie).Name,
		new Dictionary<string, EDataType>
		{
			{ nameof(MagnetId), EDataType.Key },
			{ nameof(Timestamp), EDataType.Date },
			{ nameof(MagnetName), EDataType.Text },
			{ nameof(MagnetLink), EDataType.Text },
			{ nameof(Count), EDataType.Integer }
		},
		nameof(MagnetName)
	);
}
