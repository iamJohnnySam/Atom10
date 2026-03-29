namespace MediaBoxManager;

public class YtDlpDownloadJob
{
	public required string Url { get; init; }
	public string Format { get; init; } = "best[height<=720]";
	public string? MatchTitle { get; init; }
	public int? PlaylistEnd { get; init; }
	public string? DateAfter { get; init; }
	public bool IgnoreErrors { get; init; } = true;
}
