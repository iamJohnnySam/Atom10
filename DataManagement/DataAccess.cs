using System;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Logger;
using Utilities;
using System.Linq;

namespace DataManagement;

public class DataAccess<T> : INotifyPropertyChanged where T : class
{
    public event PropertyChangedEventHandler? PropertyChanged;
    SqliteLogger logger = new SqliteLogger();

    private readonly object _cacheLock = new object();
    private List<T> allItemsCache = new List<T>();
    private bool AllItemsInitialLoad = false;
    private Task<List<T>>? _loadTask = null;

    public List<T> AllItems
    {
        get
        {
            lock (_cacheLock)
            {
                if (allItemsCache.Count == 0 && !AllItemsInitialLoad)
                {
                    AllItemsInitialLoad = true;
                    logger.Debug("'AllItems' list accessed but is currently empty. Loading Data...");
                    // Return empty list immediately; trigger background load
                    // Avoid blocking in getter - dangerous for Blazor/UI contexts
                    _ = LoadAllItemsAsync();
                }
                // Return a copy to prevent external modification
                return new List<T>(allItemsCache);
            }
        }
        private set
        {
            lock (_cacheLock)
            {
                allItemsCache = value ?? new List<T>();
                logger.Debug($"'AllItems' list updated. New count: {allItemsCache.Count}");
            }
            OnPropertyChanged();
        }
    }

    public TableMetadata Metadata { get; }
    internal readonly string connectionString;

    public DataAccess(string connectionString, TableMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        
        this.connectionString = connectionString;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

        // Ensure table exists at startup
        CreateTableAsync().GetAwaiter().GetResult();
    }

    public void UpdateAllItems(List<T> allItems)
    {
        AllItems = allItems ?? new List<T>();
    }

    private async Task LoadAllItemsAsync()
    {
        lock (_cacheLock)
        {
            // Prevent duplicate loads
            if (_loadTask != null && !_loadTask.IsCompleted)
                return;

            _loadTask = GetAllAsync().ContinueWith(t => allItemsCache, TaskScheduler.Default);
        }

        try
        {
            await _loadTask;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load AllItems: {ex.Message}");
            lock (_cacheLock)
            {
                AllItemsInitialLoad = false;
                _loadTask = null;
            }
        }
    }

    public async Task CreateTableAsync()
    {
        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync(SqliteFactory.BuildCreateTable(Metadata));
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to create table: {ex.Message}");
            throw;
        }
    }

    public virtual async Task ReloadCachedData()
    {
        bool shouldReload;
        lock (_cacheLock)
        {
            shouldReload = allItemsCache.Count > 0 || AllItemsInitialLoad;
        }

        if (shouldReload)
        {
            logger.Debug($"Reloading cached data for 'AllItems' of {typeof(T).Name}...");
            await GetAllAsync();
        }
        else
        {
            logger.Debug($"Cache {typeof(T).Name} reload ignored since cache was never accessed.");
        }
    }

    private void InvalidateCache()
    {
        lock (_cacheLock)
        {
            allItemsCache = new List<T>();
        }
        OnPropertyChanged(nameof(AllItems));
    }

    public virtual async Task InsertAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // ExecuteScalarAsync<long> returns the new row id
            var newId = await connection.ExecuteScalarAsync<long>(
                SqliteFactory.BuildInsert(Metadata), entity);

            // Set the primary key property on the entity
            var pkProp = typeof(T).GetProperty(SqliteFactory.GetKeyColumn(Metadata));
            if (pkProp != null && pkProp.CanWrite)
            {
                if (pkProp.PropertyType == typeof(int))
                    pkProp.SetValue(entity, (int)newId);
                else if (pkProp.PropertyType == typeof(long))
                    pkProp.SetValue(entity, newId);
            }

            InvalidateCache();
            logger.Debug(message: $"Inserted new {typeof(T).Name} with ID {newId}.", interaction: "SQLite");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to insert {typeof(T).Name}: {ex.Message}");
            throw;
        }
    }

    public virtual async Task GetAllAsync()
    {
        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            var sql = SqliteFactory.BuildSelect(Metadata, orderBy: Metadata.SortColumn, descending: Metadata.SortDescending);
            var items = await connection.QueryAsync<T>(sql);
            AllItems = items.ToList();
            logger.Debug(message: $"Loaded {AllItems.Count} items of type {typeof(T).Name}.", interaction: "SQLite");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to get all {typeof(T).Name}: {ex.Message}");
            throw;
        }
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        try
        {
            string primaryKey = SqliteFactory.GetKeyColumn(Metadata);
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            var result = await connection.QuerySingleOrDefaultAsync<T>(
                SqliteFactory.BuildSelect(Metadata, $"{primaryKey} = @{primaryKey}"),
                new Dictionary<string, object> { { primaryKey, id } });
            logger.Debug(message: $"Retrieved {typeof(T).Name} with ID {id}.", interaction: "SQLite");
            return result;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to get {typeof(T).Name} by ID {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<T>> GetByColumnAsync<TValue>(string columnName, TValue value)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        ValidateColumnName(columnName);

        try
        {
            var tableName = typeof(T).Name;
            var sql = $"SELECT * FROM {tableName} WHERE {columnName} = @Value " +
                      $"ORDER BY {Metadata.SortColumn} {(Metadata.SortDescending ? "DESC" : "ASC")}";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            var items = await connection.QueryAsync<T>(sql, new { Value = value });
            List<T> result = items.ToList();
            logger.Debug(message: $"Retrieved {result.Count} items of type {typeof(T).Name} where {columnName} = {value}.", interaction: "SQLite");
            return result;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to get {typeof(T).Name} by column {columnName}: {ex.Message}");
            throw;
        }
    }

    public async Task<T?> GetOneByColumnAsync<TValue>(string columnName, TValue value)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        ValidateColumnName(columnName);

        try
        {
            var tableName = typeof(T).Name;
            var sql = $"SELECT * FROM {tableName} WHERE {columnName} = @Value " +
                      $"ORDER BY {Metadata.SortColumn} {(Metadata.SortDescending ? "DESC" : "ASC")} LIMIT 1";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            var result = await connection.QueryFirstOrDefaultAsync<T>(sql, new { Value = value });
            logger.Debug(message: $"Retrieved item of type {typeof(T).Name} where {columnName} = {value}.", interaction: "SQLite");
            return result;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to get one {typeof(T).Name} by column {columnName}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<T>> GetByColumnsAsync(Dictionary<string, object> columnValues)
    {
        if (columnValues == null || columnValues.Count == 0)
            throw new ArgumentException("At least one column/value pair must be provided.", nameof(columnValues));

        foreach (var key in columnValues.Keys)
        {
            ValidateColumnName(key);
        }

        try
        {
            var tableName = typeof(T).Name;
            var whereBuilder = new StringBuilder();
            var parameters = new DynamicParameters();

            int index = 0;
            foreach (var kvp in columnValues)
            {
                var paramName = $"p{index++}";

                if (whereBuilder.Length > 0)
                    whereBuilder.Append(" AND ");

                if (kvp.Value == null)
                {
                    whereBuilder.Append($"{kvp.Key} IS NULL");
                }
                else
                {
                    whereBuilder.Append($"{kvp.Key} = @{paramName}");
                    parameters.Add(paramName, kvp.Value);
                }
            }

            var sql = $"SELECT * FROM {tableName} WHERE {whereBuilder} " +
                      $"ORDER BY {Metadata.SortColumn} {(Metadata.SortDescending ? "DESC" : "ASC")}";


            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var items = await connection.QueryAsync<T>(sql, parameters);
            List<T> result = items.ToList();

            logger.Debug(
                message: $"Retrieved {result.Count} items of type {typeof(T).Name} with multiple column filters.",
                interaction: "SQLite");

            return result;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to get {typeof(T).Name} by multiple columns: {ex.Message}");
            throw;
        }
    }

    public async Task<T?> GetOneByColumnsAsync(Dictionary<string, object> columnValues)
    {
        if (columnValues == null || columnValues.Count == 0)
            throw new ArgumentException("At least one column/value pair must be provided.", nameof(columnValues));

        foreach (var key in columnValues.Keys)
        {
            ValidateColumnName(key);
        }

        try
        {
            var tableName = typeof(T).Name;
            var whereBuilder = new StringBuilder();
            var parameters = new DynamicParameters();

            int index = 0;
            foreach (var kvp in columnValues)
            {
                var paramName = $"p{index++}";

                if (whereBuilder.Length > 0)
                    whereBuilder.Append(" AND ");

                if (kvp.Value == null)
                {
                    whereBuilder.Append($"{kvp.Key} IS NULL");
                }
                else
                {
                    whereBuilder.Append($"{kvp.Key} = @{paramName}");
                    parameters.Add(paramName, kvp.Value);
                }
            }

            var sql = $"SELECT * FROM {tableName} WHERE {whereBuilder} " +
                      $"ORDER BY {Metadata.SortColumn} {(Metadata.SortDescending ? "DESC" : "ASC")} LIMIT 1";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);

            logger.Debug(
                message: $"Retrieved single {typeof(T).Name} with multiple column filters.",
                interaction: "SQLite");

            return result;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to get one {typeof(T).Name} by multiple columns: {ex.Message}");
            throw;
        }
    }

    public virtual async Task UpdateAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync(SqliteFactory.BuildUpdate(Metadata), entity);

            InvalidateCache();
            logger.Debug(message: $"Updated {typeof(T).Name} entity.", interaction: "SQLite");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to update {typeof(T).Name}: {ex.Message}");
            throw;
        }
    }

    public virtual async Task DeleteAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync(SqliteFactory.BuildDelete(Metadata), entity);

            InvalidateCache();
            logger.Debug(message: $"Deleted {typeof(T).Name} entity.", interaction: "SQLite");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to delete {typeof(T).Name}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<T>> QueryAsync(string sql, object? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be null or empty.", nameof(sql));

        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            var items = await connection.QueryAsync<T>(sql, parameters);
            List<T> result = items.ToList();
            logger.Debug(message: $"Executed QueryAsync with SQL: {sql}, returned {result.Count} results", interaction: "SQLite");
            return result;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to execute query: {ex.Message}");
            throw;
        }
    }

    public async Task<T?> QueryFirstOrDefaultAsync(string sql, object? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be null or empty.", nameof(sql));

        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            var result = await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
            logger.Debug(message: $"Executed QueryFirstOrDefaultAsync with SQL: {sql}", interaction: "SQLite");
            return result;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to execute query first or default: {ex.Message}");
            throw;
        }
    }

    public virtual async Task ExecuteAsync(string sql, object? parameters = null, bool updateCache = true)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be null or empty.", nameof(sql));

        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync(sql, parameters);
            
            if (updateCache)
                InvalidateCache();
            
            logger.Debug(message: $"Executed ExecuteAsync with SQL: {sql}", interaction: "SQLite");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to execute command: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Validates that column name contains only alphanumeric characters and underscores to prevent SQL injection
    /// </summary>
    private void ValidateColumnName(string columnName)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(columnName, @"^[a-zA-Z0-9_]+$"))
        {
            throw new ArgumentException($"Invalid column name: {columnName}. Column names must contain only alphanumeric characters and underscores.", nameof(columnName));
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}