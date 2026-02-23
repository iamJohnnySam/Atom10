using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class SupplierDataAccess(string connectionString) : DataAccess<Supplier>(connectionString, Supplier.Metadata)
{
}
