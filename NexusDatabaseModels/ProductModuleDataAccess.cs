using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class ProductModuleDataAccess(string connectionString) : DataAccess<ProductModule>(connectionString, ProductModule.Metadata)
{
}