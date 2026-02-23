using DataManagement;
using DataManagement.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NexusDatabaseModels;

public class Supplier
{
    [Key]
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = "Untitled Supplier";
    public string SupplierWebsite { get; set; } = string.Empty;
    public string SupplierContact { get; set; } = string.Empty;
    public string ProductTypesRaw { get; set; } = string.Empty;
    public List<string> ProductTypes { get; set; } = [];

    public static TableMetadata Metadata => new(
        typeof(Supplier).Name,
        new Dictionary<string, EDataType>
        {
                { nameof(SupplierId), EDataType.Key },
                { nameof(SupplierName), EDataType.Text },
                { nameof(SupplierWebsite), EDataType.Text },
                { nameof(SupplierContact), EDataType.Text },
                { nameof(ProductTypesRaw), EDataType.Text }
        },
        nameof(SupplierName)
    );
}
