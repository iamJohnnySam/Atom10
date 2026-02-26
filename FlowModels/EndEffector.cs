using FlowModels.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlowModels;

public class EndEffector : INotifyPropertyChanged
{
    public required string PayloadType { get; set; }
    public required uint PayloadSlots { get; set; }
    public List<Payload> Payloads { get; set; } = [];

    private ManipulatorArmState armState = ManipulatorArmState.retracted;
    public ManipulatorArmState ArmState
    {
        get { return armState; }
        set { armState = value; OnPropertyChanged(); }
    }
    public bool IsEndEffectorEmpty()
    {
        return Payloads.Count == 0;
    }


    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
