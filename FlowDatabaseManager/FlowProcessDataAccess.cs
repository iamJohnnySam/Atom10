using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowDatabaseManager;

public class FlowProcessDataAccess(string connectionString) : DataAccess<FlowProcess>(connectionString, FlowProcess.Metadata)
{
}
