using DataManagement;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MediaBoxDatabaseModels;

public class TvShowDataAccess (string connectionString) : DataAccess<TvShow>(connectionString, TvShow.Metadata)
{
	public async Task<List<TvShow>> CustomSelectAsync(string condition = "1=1", string? orderBy = null, int? limit = null)
	{
		var query = $"SELECT * FROM TvShow WHERE {condition}";

		if (!string.IsNullOrEmpty(orderBy))
		{
			query += $" ORDER BY {orderBy}";
		}

		if (limit.HasValue)
		{
			query += $" LIMIT {limit.Value}";
		}

		return await QueryAsync(query);
	}

	public async Task<bool> CombinationExistsAsync(Dictionary<string, object> columnValuePairs)
	{
		if (columnValuePairs == null || columnValuePairs.Count == 0)
		{
			throw new ArgumentException("Column-value pairs cannot be null or empty.");
		}

		// Dynamically construct the WHERE clause
		var conditions = string.Join(" AND ", columnValuePairs.Keys.Select(key => $"{key} = @{key}"));
		var query = $"SELECT 1 FROM TvShow WHERE {conditions} LIMIT 1;";

		// Execute the query and check the result
		var result = await QueryFirstOrDefaultAsync(query, columnValuePairs);

		// Return true if the combination exists, otherwise false
		return result != null;
	}
}
