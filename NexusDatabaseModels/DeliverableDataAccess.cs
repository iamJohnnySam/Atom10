using NexusDatabaseManager.DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class DeliverableDataAccess(string connectionString) : DataAccess<Deliverable>(connectionString, Deliverable.Metadata)
{
}
