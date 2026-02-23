using DataManagement;
using DataManagement.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NexusDatabaseModels;

public class Configuration
{
    [Key]
    public int ConfigurationId { get; set; }
    public string ConfigurationName { get; set; } = "New Configuration";
    public string ConfigurationDescription { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int ProductModuleId { get; set; }
    public ProductModule? ProductModule { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsAddOn { get; set; }
    public bool IsRequired { get; set; }

    public static TableMetadata Metadata => new(
        typeof(Configuration).Name,
        new Dictionary<string, EDataType>
        {
                { nameof(ConfigurationId), EDataType.Key },
                { nameof(ConfigurationName), EDataType.Text },
                { nameof(ConfigurationDescription), EDataType.Text },
                { nameof(ProjectId), EDataType.Integer },
                { nameof(ProductModuleId), EDataType.Integer },
                { nameof(Quantity), EDataType.Integer },
                { nameof(IsAddOn), EDataType.Boolean },
                { nameof(IsRequired), EDataType.Boolean }
        },
        nameof(ConfigurationName)
    );
}
