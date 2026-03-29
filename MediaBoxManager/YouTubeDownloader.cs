using System.Diagnostics;
using Logger;

namespace MediaBoxManager;

public class YouTubeDownloader
{
	private readonly string _outputPath;
	private readonly string _archivePath;
	private readonly string _nodeRuntime;
	private readonly int _retries;
	private readonly SqliteLogger _logger;

	public YouTubeDownloader(string outputPath, string archivePath, string nodeRuntime = "node", int retries = 5)
	{
		_outputPath = outputPath;
		_archivePath = archivePath;
		_nodeRuntime = nodeRuntime;
		_retries = retries;
		_logger = new SqliteLogger();
	}

	public void RunAll(List<YtDlpDownloadJob> jobs)
	{
		foreach (var job in jobs)
		{
			try
			{
				_logger.Info($"Starting yt-dlp download: {job.Url}");
				RunJob(job);
				_logger.Info($"Completed yt-dlp download: {job.Url}");
			}
			catch (Exception ex)
			{
				_logger.Error($"yt-dlp failed for {job.Url}: {ex.Message}");
			}
		}
	}

	private void RunJob(YtDlpDownloadJob job)
	{
		var arguments = BuildArguments(job);

		var startInfo = new ProcessStartInfo
		{
			FileName = "yt-dlp",
			Arguments = arguments,
			UseShellExecute = false,
		};

		using var process = Process.Start(startInfo);
		if (process is null)
		{
			throw new InvalidOperationException("Failed to start yt-dlp process.");
		}

		process.WaitForExit();

		if (process.ExitCode != 0)
			_logger.Error($"yt-dlp exited with code {process.ExitCode}");
	}

	private string BuildArguments(YtDlpDownloadJob job)
	{
		var args = new List<string>
		{
			$"--js-runtimes {_nodeRuntime}",
			$"--download-archive \"{_archivePath}\"",
			"--no-overwrites",
		};

		if (job.IgnoreErrors)
			args.Add("--ignore-errors");

		if (job.DateAfter is not null)
			args.Add($"--dateafter {job.DateAfter}");

		if (job.PlaylistEnd is not null)
			args.Add($"--playlist-end {job.PlaylistEnd}");

		if (job.PlaylistEnd is not null || job.DateAfter is not null)
		{
			args.Add($"--retries {_retries}");
			args.Add($"--fragment-retries {_retries}");
		}

		if (job.MatchTitle is not null)
			args.Add($"--match-title \"{job.MatchTitle}\"");

		args.Add($"-o \"{_outputPath}/%(uploader)s/%(upload_date)s - %(title)s.%(ext)s\"");
		args.Add($"-f \"{job.Format}\"");
		args.Add(job.Url);

		return string.Join(" ", args);
	}
}
