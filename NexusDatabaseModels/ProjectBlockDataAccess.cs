using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class ProjectBlockDataAccess(string connectionString) : DataAccess<ProjectBlock>(connectionString, ProjectBlock.Metadata)
{
    public async Task<ProjectBlock?> GetProjectBlockByProjectId(int id, int year, int week)
    {
        var sql = "SELECT * FROM ProjectBlock WHERE ProjectId = @id AND Year = @year AND Week = @week";
        return await QueryFirstOrDefaultAsync(sql, new { id, year, week });
    }
}
