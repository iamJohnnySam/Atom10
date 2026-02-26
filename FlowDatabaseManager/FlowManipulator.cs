using DataManagement;
using DataManagement.Enum;
using FlowModels.Structures;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FlowDatabaseManager;

public class FlowManipulator : ManipulatorStructure
{
    public int SimulationManipulatorId { get; set; }


    public static TableMetadata Metadata => new(
        typeof(FlowManipulator).Name,
        new Dictionary<string, EDataType>
        {
            { nameof(SimulationManipulatorId), EDataType.Key },
            { nameof(ManipulatorName), EDataType.Text },
            { nameof(ManipulatorIdentifier), EDataType.Text },
            { nameof(ProductModuleId), EDataType.Integer },
            { nameof(EndEffectorsCSV), EDataType.Text },
            { nameof(LocationsCSV), EDataType.Text },
            { nameof(MotionTime), EDataType.Integer },
            { nameof(ExtendTime), EDataType.Integer },
            { nameof(RetractTime), EDataType.Integer }
        },
        nameof(ManipulatorName)
    );
}

