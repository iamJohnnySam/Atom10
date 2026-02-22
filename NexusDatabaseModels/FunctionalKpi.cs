using DataManagement;
using DataManagement.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NexusDatabaseModels;

public class FunctionalKpi
{
    [Key]
    public int FunctionalKpiId { get; set; }
    public string KpiName { get; set; } = "Untitled KPI";
    public string KpiDescription { get; set; } = string.Empty;
    public string KpiDepartment { get; set; } = "Engineering Design";
    public int KpiEffectiveFrom { get; set; } = DateTime.Now.Year;

    public static TableMetadata Metadata => new(
        typeof(FunctionalKpi).Name,
        new Dictionary<string, EDataType>
        {
                { nameof(FunctionalKpiId), EDataType.Key },
                { nameof(KpiName), EDataType.Text },
                { nameof(KpiDescription), EDataType.Text },
                { nameof(KpiDepartment), EDataType.Text },
                { nameof(KpiEffectiveFrom), EDataType.Date }
        },
        nameof(KpiName)
    );
}