using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class FunctionalKpiDataAccess(string connectionString) : DataAccess<FunctionalKpi>(connectionString, FunctionalKpi.Metadata)
{
}