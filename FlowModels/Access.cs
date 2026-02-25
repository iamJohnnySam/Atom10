using FlowModels.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Utilities;

namespace FlowModels;

public class Access : INotifyPropertyChanged
{
    public bool HasDoor { get; internal set; }
    private DoorStatus doorStatus;

    public DoorStatus DoorStatus
    {
        get { return doorStatus; }
        set
        {
            doorStatus = value;
            OnPropertyChanged();
        }
    }

    public bool IsAccessible
    {
        get
        {
            if (!HasDoor)
                return true;
            else
            {
                if (DoorStatus == DoorStatus.Open)
                    return true;
                else
                    return false;
            }
        }
    }

    public int AccessiblePayloads { get; set; }
    public uint DoorTransitionTime { get; set; }

    public Access(bool hasDoor, uint transitionTime, int accessiblePayloads)
    {
        HasDoor = hasDoor;
        if (HasDoor)
            DoorStatus = DoorStatus.Closed;
        else
            DoorStatus = DoorStatus.Open;
        DoorTransitionTime = transitionTime;
        AccessiblePayloads = accessiblePayloads;
    }

    public void OpenDoor(string tID)
    {
        if (HasDoor)
        {
            DoorStatus = DoorStatus.Opening;
            InternalClock.Instance.ProcessWait(DoorTransitionTime);
            DoorStatus = DoorStatus.Open;
        }
        else
        {
            DoorStatus = DoorStatus.Open;
        }
    }

    public void CloseDoor(string tID)
    {
        if (HasDoor)
        {
            DoorStatus = DoorStatus.Closing;
            InternalClock.Instance.ProcessWait(DoorTransitionTime);
            DoorStatus = DoorStatus.Closed;
        }
        else
        {
            DoorStatus = DoorStatus.Open;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

