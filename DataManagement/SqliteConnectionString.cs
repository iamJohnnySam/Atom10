using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DataManagement;

public class SqliteConnectionString
{
	private readonly string dbFileName = "NexusDB.sqlite";

	public string DbPath { get; private set; }
	public string ConnectionString { get; set; }

	public SqliteConnectionString(string dbName)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			DbPath = Path.Combine(homeDir, dbFileName);
		}
		else
		{
			DbPath = dbFileName;
		}
		ConnectionString = $"Data Source={DbPath};";
	}
}
