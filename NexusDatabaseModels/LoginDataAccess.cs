using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class LoginDataAccess(string connectionString) : DataAccess<Login>(connectionString, Login.Metadata)
{
    public async Task<List<Login>> GetAllActive()
    {
        return await GetByColumnAsync(nameof(Login.IsActive), true);
    }
    public async Task<Login?> GetByEmployeeIdAsync(int employeeId)
    {
        return await GetOneByColumnAsync(nameof(Login.EmployeeId), employeeId);
    }
}
