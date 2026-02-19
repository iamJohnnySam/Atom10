using Logger;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NexusDatabaseManager;

public class ManagerLogin
{
    private readonly string dbFileName = "NexusDB.sqlite";
    public readonly string dbPath;
    private readonly string _connectionString;

    // DataAccess
    public EmployeeDataAccess EmployeeDB { get; }
    public ProjectDataAccess ProjectDB { get; }

    public ManagerLogin()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            dbPath = Path.Combine(homeDir, dbFileName);
        }
        else
        {
            dbPath = dbFileName;
        }
        _connectionString = $"Data Source={dbPath};";

        // Employee
        EmployeeDB = new(_connectionString);
        ProjectDB = new(_connectionString);
        new SqliteLogger().Info($"Manager Lite Created");
    }
}
