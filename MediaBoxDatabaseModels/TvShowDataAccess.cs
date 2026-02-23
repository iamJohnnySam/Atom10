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

		var conditions = string.Join(" AND ", columnValuePairs.Keys.Select(key => $"{key} = @{key}"));
		var query = $"SELECT 1 FROM TvShow WHERE {conditions} LIMIT 1;";

		var result = await QueryFirstOrDefaultAsync(query, columnValuePairs);
		return result != null;
	}

	public async Task UpdateColumnValueAsync(string columnName, object newValue)
	{
		var query = $"UPDATE TvShow SET {columnName} = @NewValue;";
		var parameters = new Dictionary<string, object>
			{
				{ "@NewValue", newValue }
			};
		await ExecuteAsync(query, parameters);
	}
}
