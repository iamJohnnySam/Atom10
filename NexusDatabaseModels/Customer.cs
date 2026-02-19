using NexusDatabaseManager.DataManagement;
using NexusDatabaseManager.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NexusDatabaseModels;

public class Customer
{
    [Key]
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "New Customer";

    public static TableMetadata Metadata => new(
        typeof(Customer).Name,
        new Dictionary<string, EDataType>
        {
                { nameof(CustomerId), EDataType.Key },
                { nameof(CustomerName), EDataType.Text }
        },
        nameof(CustomerName)
    );
}