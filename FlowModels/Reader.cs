using FlowModels.Command;
using FlowModels.Enum;
using Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace FlowModels;

public class Reader : INotifyPropertyChanged
{
    // PROPERTIES
    public required string ReaderId { get; set; }
    public required Station TargetStation { get; set; }
    public int SlotId { get; set; } = 0;
    public required ReaderType Type { get; set; }


    public Reader()
    {
        if (TargetStation == null)
            throw new ErrorResponse(ErrorCode.ProgramError, "Target Station not assigned for Reader");

        if (Type == ReaderType.Payload)
        {
            if (SlotId == 0)
                throw new ErrorResponse(ErrorCode.MissingArguments, $"No Slot assigned for reader for Target Station {TargetStation.StationId}");
            // TargetStation.PairReader(readerID, slot);
        }
        else
        {
            if (TargetStation.PodDockable)
            {
                // targetStation.PairReader(readerID);
            }
            else
            {

            }

        }
    }

    public string ReadID(string transactionID)
    {
        string value;
        if (Type == ReaderType.Payload)
        {
            if (TargetStation.Cassette.Slots[SlotId].IsOccupied)
            {
                value = TargetStation.Cassette.Slots[SlotId].Payload!.PayloadID;
                TransactionLogger.Instance.Info(new TransactionLog(transactionID, $"Reader {ReadID} returned slot Id {value} at {TargetStation.StationId}"));
            }
            else
                throw new ErrorResponse(ErrorCode.PayloadNotAvailable, $"Reader {ReaderId} did not have any payload on {TargetStation.StationId} slot {SlotId} to read.");
        }
        else
        {
            value = TargetStation.PodId;
            TransactionLogger.Instance.Info(new TransactionLog(transactionID, $"Reader {ReadID} returned Pod Id {value} at {TargetStation.StationId}"));
        }
        return value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
