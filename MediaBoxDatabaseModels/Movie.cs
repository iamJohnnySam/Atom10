using DataManagement;
using DataManagement.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MediaBoxDatabaseModels;

public class Movie
{
	[Key]
	public int MovieId { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.Now;
	public string MovieName { get; set; } = "UnNamed Show";
	public string Magnet { get; set; } = string.Empty;
	public string Quality { get; set; } = string.Empty;
	public bool Exists { get; set; } = true;
	public string Path { get; set; } = string.Empty;


	public static TableMetadata Metadata => new(
		typeof(Movie).Name,
		new Dictionary<string, EDataType>
		{
			{ nameof(MovieId), EDataType.Key },
			{ nameof(Timestamp), EDataType.Date },
			{ nameof(MovieName), EDataType.Text },
			{ nameof(Magnet), EDataType.Text },
			{ nameof(Quality), EDataType.Text },
			{ nameof(Exists), EDataType.Boolean },
			{ nameof(Path), EDataType.Text }
		},
		nameof(MovieName)
	);
}
