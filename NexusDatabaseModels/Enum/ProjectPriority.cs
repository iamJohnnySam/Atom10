using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace NexusDatabaseModels.Enum;

public enum ProjectPriority
{
    [Description("Not Started")]
    NotStarted = 0,
    Low = 3,
    Normal = 4,
    High = 5,
    Completed = 2,
    Discarded = 1
}