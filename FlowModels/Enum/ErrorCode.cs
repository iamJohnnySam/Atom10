using System;
using System.Collections.Generic;
using System.Text;

namespace FlowModels.Enum;

public enum ErrorCode
{
    SimulatorStopped,
    ProgramError,
    InvalidReservation,
    PodAlreadyAvailable,
    PodNotAvailable,
    PayloadAlreadyAvailable,
    PayloadNotAvailable,
    SlotsEmpty,
    SlotsNotEmpty,
    SlotOutOfBounds,
    SlotBlocked,
    NotAccessible,
    NotPodDockable,
    NotProcessable,
    ProcessNotAvailable,
    ActionWhileBusy,
    StationNotReachable,
    UnknownArmState,
    EndEffectorOutOfBounds,
    PayloadTypeMismatch,
    SlotIndexMissing,
    CassetteNotMovable,
    IncorrectState,
    ModuleError,
    TimedOut,
    MissingArguments,
}
