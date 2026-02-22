using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class OemItemDataAccess : DataAccess<OemItem>
{
    public OemItemDataAccess(string connectionString) : base(connectionString, OemItem.Metadata)
    {

    }
}
