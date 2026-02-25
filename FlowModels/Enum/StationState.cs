using System;
using System.Collections.Generic;
using System.Text;

namespace FlowModels.Enum;

public enum StationState
{
    Off,
    Idle,
    UnDocked,
    Opening,
    Closing,
    Mapping,
    WaitingToBeAccessed,
    BeingAccessed,
    Processing
}
