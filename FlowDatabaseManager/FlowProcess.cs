using DataManagement;
using DataManagement.Enum;
using FlowModels.Structures;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowDatabaseManager;

public class FlowProcess : ProcessStructure
{
    public static TableMetadata Metadata => new(
        typeof(FlowProcess).Name,
        new Dictionary<string, EDataType>
        {
            { nameof(ProcessId), EDataType.Key },
            { nameof(ProcessName), EDataType.Text },
            { nameof(InputState), EDataType.Text },
            { nameof(OutputState), EDataType.Text },
            { nameof(NextLocation), EDataType.Text },
            { nameof(ProcessTime), EDataType.Integer }
        },
        nameof(ProcessName)
    );
}