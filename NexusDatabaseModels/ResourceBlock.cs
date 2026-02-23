using DataManagement;
using DataManagement.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class ResourceBlock
{
    public int ResourceBlockId { get; set; }
    public Employee? Employee { get; set; }
    public int EmployeeId { get; set; }
    public int ProjectId { get; set; }
    public int Year { get; set; }
    public int Week { get; set; }

    public static TableMetadata Metadata => new(
        typeof(ResourceBlock).Name,
        new Dictionary<string, EDataType>
        {
                { nameof(ResourceBlockId), EDataType.Key },
                { nameof(EmployeeId), EDataType.Integer },
                { nameof(ProjectId), EDataType.Integer },
                { nameof(Year), EDataType.Integer },
                { nameof(Week), EDataType.Integer }
        },
        nameof(EmployeeId)
    );
}
