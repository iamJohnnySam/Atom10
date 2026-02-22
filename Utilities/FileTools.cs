using Logger;
using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities;

public class FileTools
{
	public void CreateFolderIfNotExist(string location)
	{
		if (!Directory.Exists(location))
		{
			Directory.CreateDirectory(location);
			new SqliteLogger().Info($"Created folder at {location}");
		}
	}
}
