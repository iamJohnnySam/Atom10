using FlowModels.Command;
using FlowModels.Enum;
using Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Utilities;

namespace FlowModels;

public class Manipulator : INotifyPropertyChanged
{
    // REQUIRED PARAMS
    public required string ManipulatorId { get; set; }
    public required Dictionary<int, EndEffector> EndEffectors { get; set; }
    public required List<string> Locations { get; set; }
    public required uint MotionTime { get; set; }
    public required uint ExtendTime { get; set; }
    public required uint RetractTime { get; set; }


    // OTHER PARAMS
    private ManipulatorState state = ManipulatorState.Off;
    public ManipulatorState State
    {
        get { return state; }
        set { state = value; OnPropertyChanged(); }
    }

    private string currentLocation;
    public string CurrentLocationStationId
    {
        get { return currentLocation; }
        set { currentLocation = value; OnPropertyChanged(); }
    }


    public Manipulator()
    {
        if (Locations is null || Locations.Count == 0)
            throw new ErrorResponse(ErrorCode.MissingArguments, $"No Locations for Manipulator {ManipulatorId}");
        currentLocation = Locations[0];
    }

    private void CheckBusy()
    {
        if (State != ManipulatorState.Idle)
            throw new ErrorResponse(ErrorCode.ActionWhileBusy, $"Manipulator {ManipulatorId} is busy.");

        foreach (KeyValuePair<int, EndEffector> EE in EndEffectors)
        {
            if (EE.Value.ArmState != ManipulatorArmState.retracted)
                throw new ErrorResponse(ErrorCode.UnknownArmState, $"Manipulator {ManipulatorId} has Arm {EE.Key} Extended while idle.");
        }
    }
    private void CheckTransferCompatibility(int endEffectorId, List<Reservation> reservations)
    {
        if (!EndEffectors.ContainsKey(endEffectorId))
            throw new ErrorResponse(ErrorCode.EndEffectorOutOfBounds, $"Manipulator {ManipulatorId} does not contain end effector {endEffectorId}");

        if (reservations.Count == 0)
            throw new ErrorResponse(ErrorCode.InvalidReservation, "No Reservations provided");

        if (reservations.Count > EndEffectors[endEffectorId].PayloadSlots)
            throw new ErrorResponse(ErrorCode.InvalidReservation, $"Too many Reservations provided for Manipulator {ManipulatorId} End Effector {endEffectorId}");

        if (reservations.Count != EndEffectors[endEffectorId].PayloadSlots)
            TransactionLogger.Instance.Info(new TransactionLog("", $"Manipulator {ManipulatorId} End Effector {endEffectorId} received {reservations.Count} Reservations but has {EndEffectors[endEffectorId].PayloadSlots} Payload Slots"));

        if (reservations.Any(r => r.TargetStation == null))
            throw new ErrorResponse(ErrorCode.ProgramError, "One or more Reservations have no Target Station set");

        foreach (Reservation reservation in reservations)
        {
            if (reservation.TargetStation == null)
                throw new ErrorResponse(ErrorCode.ProgramError, "Target Station is not set");
        }

        string expectedPayloadId = reservations.FirstOrDefault()!.Payload.PayloadID;
        string expectedTargetStationId = reservations.FirstOrDefault()!.TargetStation!.StationId;
        ReservationType expectedReservationType = reservations.FirstOrDefault()!.Type;

        foreach (Reservation reservation in reservations)
        {
            if ((expectedPayloadId != reservation.Payload.PayloadID) || (expectedTargetStationId != reservation.TargetStation!.StationId) || (expectedReservationType != reservation.Type))
                throw new ErrorResponse(ErrorCode.InvalidReservation, $"Reservations did not match");
        }

        if (!Locations.Intersect(reservations.FirstOrDefault()!.TargetStation!.Locations.Keys).Any())
            throw new ErrorResponse(ErrorCode.StationNotReachable, $"Manipulator {ManipulatorId} could not access any locations.");

        if (EndEffectors[endEffectorId].PayloadType != reservations.FirstOrDefault()!.PayloadType)
            throw new ErrorResponse(ErrorCode.PayloadTypeMismatch, $"Manipulator {ManipulatorId} End Effector did not match the payload type for this station.");
    }


    private void GoToStation(string tID, string stationId)
    {
        if (CurrentLocationStationId != stationId)
        {
            State = ManipulatorState.Moving;
            InternalClock.Instance.ProcessWait(MotionTime);
            CurrentLocationStationId = stationId;
            TransactionLogger.Instance.Info(new TransactionLog(tID, $"Manipulator {ManipulatorId} Moved to Station {stationId}"));
        }
    }
    private void ExtendArm(string tID, int endEffectorId)
    {
        if (!EndEffectors.ContainsKey(endEffectorId))
            throw new ErrorResponse(ErrorCode.EndEffectorOutOfBounds, $"End Effector {endEffectorId} is not valid");

        if (EndEffectors[endEffectorId].ArmState == ManipulatorArmState.extended)
            throw new ErrorResponse(ErrorCode.IncorrectState, $"End Effector {endEffectorId} was already extended");

        State = ManipulatorState.Extending;
        InternalClock.Instance.ProcessWait(ExtendTime);
        EndEffectors[endEffectorId].ArmState = ManipulatorArmState.extended;
    }
    private void RetractArm(string tID, int endEffectorId)
    {
        if (!EndEffectors.ContainsKey(endEffectorId))
            throw new ErrorResponse(ErrorCode.EndEffectorOutOfBounds, $"End Effector {endEffectorId} is not valid");

        if (EndEffectors[endEffectorId].ArmState == ManipulatorArmState.retracted)
            throw new ErrorResponse(ErrorCode.IncorrectState, $"End Effector {endEffectorId} was already retracted");

        State = ManipulatorState.Retracting;
        InternalClock.Instance.ProcessWait(RetractTime);
        EndEffectors[endEffectorId].ArmState = ManipulatorArmState.retracted;
    }

    public void Home(string tID)
    {
        CheckBusy();
        TransactionLogger.Instance.Info(new TransactionLog(tID, $"Manipulator {ManipulatorId} Homing"));
        GoToStation(tID, "home");
        State = ManipulatorState.Idle;
        TransactionLogger.Instance.Info(new TransactionLog(tID, $"Manipulator {ManipulatorId} at Home"));
    }
    public void PowerOff(string tID)
    {
        CheckBusy();
        State = ManipulatorState.Off;
        TransactionLogger.Instance.Info(new TransactionLog(tID, $"Manipulator {ManipulatorId} Off."));
    }
    public void PowerOn(string tID)
    {
        if (State != ManipulatorState.Off && State != ManipulatorState.Idle)
            throw new ErrorResponse(ErrorCode.IncorrectState, "Invalid State");
        State = ManipulatorState.Idle;
        TransactionLogger.Instance.Info(new TransactionLog(tID, $"Manipulator {ManipulatorId} On"));
    }
    public void Pick(string tID, int endEffectorId, List<Reservation> reservations)
    {
        CheckBusy();
        CheckTransferCompatibility(endEffectorId, reservations);
        if (reservations.FirstOrDefault()!.Type != ReservationType.pickFromStation)
            throw new ErrorResponse(ErrorCode.InvalidReservation, $"Manipulator {ManipulatorId} Pick did not receieve a pick reservation");


        PickOrPlace(tID, endEffectorId, reservations);
    }
    public void Place(string tID, int endEffectorId, List<Reservation> reservations)
    {
        CheckBusy();
        CheckTransferCompatibility(endEffectorId, reservations);
        if (reservations.FirstOrDefault()!.Type != ReservationType.placeInStation)
            throw new ErrorResponse(ErrorCode.InvalidReservation, $"Manipulator {ManipulatorId} Place did not receieve a place reservation");


        PickOrPlace(tID, endEffectorId, reservations);
    }

    private void PickOrPlace(string tID, int endEffectorId, List<Reservation> reservations)
    {
        State = ManipulatorState.Moving;
        GoToStation(tID, reservations.FirstOrDefault()!.TargetStation!.StationId);

        State = ManipulatorState.Extending;

        HashSet<string> commonElements = new(Locations);
        commonElements.IntersectWith(reservations.FirstOrDefault()!.TargetStation!.Locations.Keys);
        reservations.FirstOrDefault()!.TargetStation!.CheckLocationAccess(commonElements.First());

        reservations.FirstOrDefault()!.TargetStation!.StartAccess(tID, reservations);
        ExtendArm(tID, endEffectorId);

        if (reservations.First()!.Type == ReservationType.pickFromStation)
        {
            EndEffectors[endEffectorId].Payloads = reservations.First()!.TargetStation!.PickFromStation(tID, reservations);
        }
        else
        {
            reservations.First()!.TargetStation!.PlaceInStation(tID, reservations);
            EndEffectors[endEffectorId].Payloads = [];
        }

        State = ManipulatorState.Retracting;
        RetractArm(tID, endEffectorId);

        reservations.First()!.TargetStation!.StopAccess(tID, reservations);
        State = ManipulatorState.Idle;
    }




    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
