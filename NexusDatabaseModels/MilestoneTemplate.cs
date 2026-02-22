using DataManagement;
using DataManagement.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class MilestoneTemplate
{
    public int MilestoneTemplateId { get; set; }
    public string MilestoneName { get; set; } = string.Empty;
    public int Days { get; set; }
    public bool IsSelected { get; set; }

    public static TableMetadata Metadata => new(
        typeof(MilestoneTemplate).Name,
        new Dictionary<string, EDataType>
        {
            { nameof(MilestoneTemplateId), EDataType.Key },
            { nameof(MilestoneName), EDataType.Text },
            { nameof(Days), EDataType.Integer }
        },
        nameof(Days)
    );
}
