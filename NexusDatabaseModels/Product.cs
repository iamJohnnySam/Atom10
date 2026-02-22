using DataManagement;
using DataManagement.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NexusDatabaseModels;

public class Product
{
    [Key]
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "Untitled Product";

    public static TableMetadata Metadata => new(
        typeof(Product).Name,
        new Dictionary<string, EDataType>
        {
            { nameof(ProductId), EDataType.Key },
            { nameof(ProductName), EDataType.Text }
        },
        nameof(ProductName)
    );
}