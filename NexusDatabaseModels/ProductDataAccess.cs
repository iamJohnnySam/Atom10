using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class ProductDataAccess(string connectionString) : DataAccess<Product>(connectionString, Product.Metadata)
{
}
