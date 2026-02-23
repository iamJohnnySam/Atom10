using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels.Enum;

public enum DependencyType
{
    FinishToStart,
    StartToStart,
    StartToFinish,
    FinishToFinish
}
