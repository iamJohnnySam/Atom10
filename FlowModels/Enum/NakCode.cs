using System;
using System.Collections.Generic;
using System.Text;

namespace FlowModels.Enum;

public enum NAckCode
{
    SimulatorNotStarted,
    CommSpecError,
    CommandError,
    TargetNotExist,
    MissingArguments,
    Busy,
    NotDockable,
    NotMappable,
    StationDoesNotHaveDoor,
    PowerOff,
    EndEffectorMissing,
    ModuleNack
}