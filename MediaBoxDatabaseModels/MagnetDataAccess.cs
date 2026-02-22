using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBoxDatabaseModels;

public class MagnetDataAccess (string connectionString) : DataAccess<Magnet>(connectionString, Magnet.Metadata)
{
	public async Task<bool> CombinationExistsAsync(Dictionary<string, object> columnValuePairs)
	{
		if (columnValuePairs == null || columnValuePairs.Count == 0)
		{
			throw new ArgumentException("Column-value pairs cannot be null or empty.");
		}

		// Dynamically construct the WHERE clause
		var conditions = string.Join(" AND ", columnValuePairs.Keys.Select(key => $"{key} = @{key}"));
		var query = $"SELECT 1 FROM Magnet WHERE {conditions} LIMIT 1;";

		// Execute the query and check the result
		var result = await QueryFirstOrDefaultAsync(query, columnValuePairs);

		// Return true if the combination exists, otherwise false
		return result != null;
	}
}
