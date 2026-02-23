using DataManagement;
using DataManagement.Enum;
using NexusDatabaseModels.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NexusDatabaseModels;

public class ProductModule
{
    [Key]
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = "Untitled Module";
    public ModuleType ModuleType { get; set; } = ModuleType.None;
    public int Rank { get; set; }

    public static TableMetadata Metadata => new(
        typeof(ProductModule).Name,
        new Dictionary<string, EDataType>
        {
                { nameof(ModuleId), EDataType.Key },
                { nameof(ModuleType), EDataType.Integer },
                { nameof(ModuleName), EDataType.Text },
                { nameof(Rank), EDataType.Integer }
        },
        nameof(Rank)
    );
}
