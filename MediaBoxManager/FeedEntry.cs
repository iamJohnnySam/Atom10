using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBoxManager;

internal class FeedEntry
{
	public required string Title { get; set; }
	public required string Link { get; set; }
	public string? TvShowName { get; set; }
	public string? TvEpisodeId { get; set; }
}
