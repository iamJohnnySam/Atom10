using DataManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MediaBoxDatabaseModels;

public class MovieDataAccess(string connectionString) : DataAccess<Movie>(connectionString, Movie.Metadata)
{
	public async Task UpdateColumnValueAsync(string columnName, object newValue)
	{
		var query = $"UPDATE Movie SET {columnName} = @NewValue;";
		var parameters = new Dictionary<string, object>
			{
				{ "@NewValue", newValue }
			};
		await ExecuteAsync(query, parameters);
	}

	public async Task<bool> CombinationExistsAsync(Dictionary<string, object> columnValuePairs)
	{
		if (columnValuePairs == null || columnValuePairs.Count == 0)
		{
			throw new ArgumentException("Column-value pairs cannot be null or empty.");
		}

		var conditions = string.Join(" AND ", columnValuePairs.Keys.Select(key => $"{key} = @{key}"));
		var query = $"SELECT 1 FROM Movie WHERE {conditions} LIMIT 1;";

		var result = await QueryFirstOrDefaultAsync(query, columnValuePairs);
		return result != null;
	}
}
