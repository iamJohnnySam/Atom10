using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities;

class ScheduledTask
{
	public TimeSpan RunTime { get; set; }
	public TimeSpan Interval { get; set; }
	public required Action Action { get; set; }
	public bool IntervalMode { get; set; }
}

public class Scheduler
{
	private readonly List<ScheduledTask> _tasks = [];

	public void AddDailyTask(TimeSpan runTime, Action action)
	{
		_tasks.Add(new ScheduledTask
		{
			RunTime = runTime,
			Action = action,
			IntervalMode = false
		});
	}

	public void AddIntervalTask(TimeSpan interval, Action action)
	{
		_tasks.Add(new ScheduledTask
		{
			Interval = interval,
			Action = action,
			IntervalMode = true
		});
	}

	public void Start()
	{
		foreach (var task in _tasks)
		{
			if (task.IntervalMode)
			{
				Task.Run(() => RunIntervalTask(task));
			}
			else
			{
				Task.Run(() => RunDailyTask(task));
			}
		}
	}

	private static void RunDailyTask(ScheduledTask task)
	{
		while (true)
		{
			var now = DateTime.Now;
			var nextRun = new DateTime(now.Year, now.Month, now.Day, task.RunTime.Hours, task.RunTime.Minutes, task.RunTime.Seconds);

			if (nextRun <= now)
				nextRun = nextRun.AddDays(1);

			var delay = nextRun - now;
			Task.Delay(delay).Wait();

			task.Action();
		}
	}

	private static void RunIntervalTask(ScheduledTask task)
	{
		while (true)
		{
			Task.Delay(task.Interval).Wait();
			task.Action();
		}
	}
}
