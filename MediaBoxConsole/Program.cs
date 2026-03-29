using MediaBoxManager;
using MediaBoxManager.Enum;

if (args.Length > 0 && args[0] == "1")
{
	_ = new Manager(setSchedule: false, runType: RunProgram.TorrentCleaner);
}
else if (args.Length > 0 && args[0] == "2")
{
	_ = new Manager(setSchedule: false, runType: RunProgram.ShowScanner);
}
else if (args.Length > 0 && args[0] == "3")
{
	string? url = args.Length > 1 ? args[1] : null;
	_ = new Manager(setSchedule: false, runType: RunProgram.YouTubeDownloader, youtubeUrl: url);
}
else
{
	_ = new Manager(setSchedule: true);
}
