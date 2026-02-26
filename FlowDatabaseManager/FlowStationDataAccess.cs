using System;
using System.Collections.Generic;
using System.Text;

namespace FlowDatabaseManager;

public class FlowStationDataAccess(string connectionString) : DataManagement.DataAccess<FlowStation>(connectionString, FlowStation.Metadata)
{
}
