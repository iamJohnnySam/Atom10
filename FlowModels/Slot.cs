using FlowModels.Command;
using FlowModels.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlowModels;

public class Slot(int slot) : INotifyPropertyChanged
{
    public int SlotId { get; set; } = slot;
    private Payload? payload;
    public Payload? Payload
    {
        get { return payload; }
        set
        {
            payload = value;
            OnPropertyChanged();
        }
    }

    private int blockedByReservationId = 0;
    public int BlockedByReservationId
    {
        get { return blockedByReservationId; }
        set { blockedByReservationId = value; }
    }

    public bool IsBlocked
    {
        get { return BlockedByReservationId != 0; }
    }
    public bool IsOccupied
    {
        get { return Payload != null; }
    }

    public void BlockSlot(Reservation reservation)
    {
        if (IsBlocked)
            throw new ErrorResponse(ErrorCode.SlotBlocked, $"Slot {SlotId} is blocked.");

        BlockedByReservationId = reservation.Id;
    }



    // INTERLOCK LOGIC
    private void AddPayloadInterlock(Reservation? reservation)
    {
        if (IsOccupied)
            throw new ErrorResponse(ErrorCode.PayloadAlreadyAvailable, $"Payload already in slot {SlotId}.");

        ReservationInterlock(reservation);
    }
    private void RemovePayloadInterlock(Reservation? reservation)
    {
        if (!IsOccupied)
            throw new ErrorResponse(ErrorCode.PayloadNotAvailable, $"Slot {SlotId} has no payload.");
        if (Payload == null)
            throw new ErrorResponse(ErrorCode.SlotsEmpty, "No payload to remove.");


        ReservationInterlock(reservation);
    }
    private void ReservationInterlock(Reservation? reservation)
    {
        if (IsBlocked)
        {
            if (reservation == null)
                throw new ErrorResponse(ErrorCode.SlotBlocked, $"Slot {SlotId} is blocked.");

            else if (reservation.Id != BlockedByReservationId)
                throw new ErrorResponse(ErrorCode.SlotBlocked, $"Slot {SlotId} is blocked by a different reservation");
        }
    }


    public Reservation SetReservation(ReservationType reservationType, Payload? payload = null)
    {
        if (reservationType == ReservationType.pickFromStation)
        {
            RemovePayloadInterlock(null);
            if (payload == null)
            {
                if (Payload != null)
                    payload = Payload;
                else
                    throw new ErrorResponse(ErrorCode.ProgramError, "Expected Payload not found in slot");
            }
        }
        else
        {
            AddPayloadInterlock(null);
            if (payload == null)
                throw new ErrorResponse(ErrorCode.PayloadNotAvailable, "No Payload found to reserve");
        }

        Reservation reservation = new()
        {
            Type = ReservationType.pickFromStation,
            Payload = payload,
            Slot = this
        };
        BlockSlot(reservation);
        return reservation;
    }
    private void ResetReservation(Reservation? reservation)
    {
        if (reservation != null)
        {
            if (reservation.Id != BlockedByReservationId)
            {
                BlockedByReservationId = 0;
            }
            else
            {
                throw new ErrorResponse(ErrorCode.SlotBlocked, $"Slot {SlotId} is blocked by a different reservation");
            }
        }
        else
        {
            if (BlockedByReservationId != 0)
            {
                throw new ErrorResponse(ErrorCode.SlotBlocked, $"Slot {SlotId} is blocked. Need reservation to unblock.");
            }
        }
    }
    protected internal void AddPayload(Payload payload, Reservation? reservation = null)
    {
        AddPayloadInterlock(reservation);
        Payload = payload;
        Payload.CurrentSlotId = SlotId;
        ResetReservation(reservation);
    }

    protected internal Payload RemovePayload(Reservation? reservation = null)
    {
        RemovePayloadInterlock(reservation);
        if (Payload == null)
            throw new ErrorResponse(ErrorCode.ProgramError, "Expected Payload not found");

        var temp = Payload;
        Payload = null;
        return temp;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
