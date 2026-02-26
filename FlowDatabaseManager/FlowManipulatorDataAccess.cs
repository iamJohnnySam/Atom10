using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowDatabaseManager;

public class FlowManipulatorDataAccess(string connectionString) : DataAccess<FlowManipulator>(connectionString, FlowManipulator.Metadata)
{
}
