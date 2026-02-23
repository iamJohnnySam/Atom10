using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace NexusDatabaseModels.Enum;

public enum SalesStatus
{
    [Description("Concept Stage")]
    Concept,
    [Description("PO Project")]
    POProject,
    [Description("After Sales")]
    AfterSales,
    [Description("Internal Project")]
    InternalProject
}
