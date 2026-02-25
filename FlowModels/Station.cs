using FlowModels.Command;
using FlowModels.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Utilities;

namespace FlowModels;

public class Station : INotifyPropertyChanged
{
    // REQUIRED PARAMS
    public required string StationId { get; set; }
    public required bool AutoMode { get; set; }
    public required Cassette? Cassette { get; set; }
    public required Dictionary<string, Access> Locations { get; set; }
    public required Dictionary<string, Process> Processes { get; set; }
    public required bool IsInputAndPodDockable { get; set; }
    public required bool IsOutputAndPodDockable { get; set; }
    public required bool Processable { get; set; }
    public required bool HighPriority { get; set; }


    // OTHER PARAMS
    private StationState state;
    public StationState State
    {
        get { return state; }
        set
        {
            state = value;
            OnPropertyChanged();
        }
    }
    private string? podId = null;
    public string PodId
    {
        get
        {
            if (!PodDockable)
                throw new ErrorResponse(ErrorCode.NotPodDockable, $"Station {StationId} is not a Pod Dockable station.");
            if (podId != null)
                return podId;
            else
                throw new ErrorResponse(ErrorCode.PodNotAvailable, $"Station {StationId} does not have a Pod.");
        }
        set
        {
            if (value == string.Empty)
                podId = null;
            podId = value; OnPropertyChanged();
        }
    }
    protected internal bool PodDockable { get; set; }
    public List<int> PendingReservationIds { get; set; } = [];
    private bool IsFullAndReadyToProcess
    {
        get
        {
            if (Cassette == null)
                return false;
            if (Cassette.CurrentCapacity < Cassette.Capacity)
                return false;
            if (!Cassette.AllSlotsHavePayloadsWithSamePayloadState)
                return false;
            return true;
        }
    }
    private bool AllClosableDoorsAreClosed
    {
        get
        {
            foreach (Access access in Locations.Values)
            {
                if (access.HasDoor && access.DoorStatus != DoorStatus.Closed)
                    return false;
            }
            return true;
        }
    }
    private bool AllDoorsAreOpened
    {
        get
        {
            foreach (Access access in Locations.Values)
            {
                if (access.HasDoor && access.DoorStatus != DoorStatus.Open)
                    return false;
            }
            return true;
        }
    }
    private string currentLocation = string.Empty;
    public string CurrentLocation
    {
        get { return currentLocation; }
        private set { currentLocation = value; OnPropertyChanged(); }
    }



    public Station()
    {
        PodDockable = IsInputAndPodDockable || IsOutputAndPodDockable;
        CurrentLocation = Locations!.Keys.First();

        if (!PodDockable && Cassette == null)
            throw new ErrorResponse(ErrorCode.PodNotAvailable, $"Station {StationId} does not contain expected cassette.");
    }

    private void CheckPod()
    {
        if (Cassette == null || PodId == null)
            throw new ErrorResponse(ErrorCode.PodNotAvailable, $"Station {StationId} did not contain Pod");
    }
    protected internal void CheckLocationAccess(string location)
    {
        if (!Locations.ContainsKey(location))
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Location {location} is Out of Bounds");

        if (!AutoMode && Locations[location].DoorStatus != DoorStatus.Open)
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Door to Location {location} is not open.");
    }
    private void CheckReservations(List<Reservation> reservations)
    {
        foreach (Reservation reservation in reservations)
        {
            if (!PendingReservationIds.Contains(reservation.Id))
                throw new ErrorResponse(ErrorCode.InvalidReservation, $"Reservation {reservation.Id} not found in Waiting List");
        }
    }
    private void CheckAccessibility(Reservation reservation)
    {
        if (Cassette!.CurrentSlot > reservation.SlotId || (Cassette!.CurrentSlot + Locations[reservation.AccessFromLocation].AccessiblePayloads) < reservation.SlotId)
            throw new ErrorResponse(ErrorCode.SlotOutOfBounds, $"Reservation {reservation.Id} Slot {reservation.SlotId} is out of bounds for Cassette in Station {StationId}");
    }



    public void DockPod(Pod pod)
    {
        if (State != StationState.Idle)
            throw new ErrorResponse(ErrorCode.ActionWhileBusy, $"Station {StationId} is Busy");

        if (Cassette != null)
            throw new ErrorResponse(ErrorCode.PodAlreadyAvailable, $"Station {StationId} already contains Pod {PodId}");

        PodId = pod.PodID;
        Cassette = pod.Cassette;

        if (AutoMode)
        {
            if (Processes.Count == 0)
                throw new ErrorResponse(ErrorCode.NotProcessable, $"Station {StationId} does not have any Processes defined.");
            Cassette.UpdatePayloadState(Processes.Values.FirstOrDefault()!.OutputState);
        }
    }
    public Pod UndockPod()
    {
        if (State != StationState.Idle)
            throw new ErrorResponse(ErrorCode.ActionWhileBusy, $"Station {StationId} is Busy");
        CheckPod();


        Pod returnPod = new()
        {
            PodID = PodId,
            Cassette = Cassette!
        };

        PodId = string.Empty;
        Cassette = null;

        return returnPod;
    }

    private void AdjustAndAddReservationsToWaitingList(string tID, IEnumerable<Reservation> reservations, string accessFromLocation)
    {
        foreach (Reservation reservation in reservations)
        {
            reservation.TargetStation = this;
            reservation.AccessFromLocation = accessFromLocation;
            PendingReservationIds.Add(reservation.Id);
        }

    }
    private void RemoveReservationFromWaitingList(string tID, List<Reservation> reservations)
    {
        foreach (Reservation reservation in reservations)
        {
            if (PendingReservationIds.Contains(reservation.Id))
                PendingReservationIds.Remove(reservation.Id);
            else
                throw new ErrorResponse(ErrorCode.IncorrectState, $"Reservation {reservation.Id} was not found in the pending list");
        }
    }

    protected internal List<Reservation> ReservePickFromStation(string tID, string accessFromLocation, int startingSlot = 0, int slotCount = 1)
    {
        if (State != StationState.Idle)
            throw new ErrorResponse(ErrorCode.ActionWhileBusy, $"Station {StationId} is Busy");

        if (Cassette == null)
            throw new ErrorResponse(ErrorCode.PodNotAvailable, $"Station {StationId} does not contain expected cassette");

        if (CurrentLocation != accessFromLocation)
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Station {StationId} is not currently at location {accessFromLocation}");

        if (startingSlot == 0)
            startingSlot = Cassette.GetNextOccupiedSlot();

        if (startingSlot < 0)
            throw new ErrorResponse(ErrorCode.SlotOutOfBounds, $"Starting slot cannot be less than 1. Or you did not pass in a starting slot and all slots are unavailable.");

        if (!Locations.ContainsKey(accessFromLocation))
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Location {accessFromLocation} is Out of Bounds");

        if (Locations[accessFromLocation].AccessiblePayloads < slotCount)
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Location {accessFromLocation} door is not big enough to access {slotCount} payloads.");

        List<Reservation> reservations = Cassette.SetReservation(ReservationType.pickFromStation, startingSlot, slotCount);
        AdjustAndAddReservationsToWaitingList(tID, reservations, accessFromLocation);
        State = StationState.WaitingToBeAccessed;
        return reservations;
    }
    protected internal List<Reservation> ReservePlaceToStation(string tID, string accessFromLocation, List<Payload> payloads, int startingSlot = 0, int slotCount = 1)
    {
        if (State != StationState.Idle)
            throw new ErrorResponse(ErrorCode.ActionWhileBusy, $"Station {StationId} is Busy");

        if (Cassette == null)
            throw new ErrorResponse(ErrorCode.PodNotAvailable, $"Station {StationId} does not contain expected cassette");

        if (CurrentLocation != accessFromLocation)
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Station {StationId} is not currently at location {accessFromLocation}");

        if (startingSlot == 0)
            startingSlot = Cassette.GetNextEmptySlot();

        if (startingSlot < 0)
            throw new ErrorResponse(ErrorCode.SlotOutOfBounds, $"Starting slot cannot be less than 1. Or you did not pass in a starting slot and all slots are unavailable.");

        if (!Locations.ContainsKey(accessFromLocation))
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Location {accessFromLocation} is Out of Bounds");

        if (Locations[accessFromLocation].AccessiblePayloads < slotCount)
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Location {accessFromLocation} door is not big enough to access {slotCount} payloads.");

        List<Reservation> reservations = Cassette.SetReservation(ReservationType.placeInStation, startingSlot, slotCount, payloads);
        AdjustAndAddReservationsToWaitingList(tID, reservations, accessFromLocation);
        State = StationState.WaitingToBeAccessed;
        return reservations;
    }

    protected internal List<Payload> PickFromStation(string tID, List<Reservation> reservations)
    {
        if (State != StationState.BeingAccessed)
            throw new ErrorResponse(ErrorCode.IncorrectState, "Station Expected to be switched to Being Accessed.");
        CheckPod();
        CheckReservations(reservations);

        if (AutoMode)
        {
            Thread moveCassetteThread = new(() => MoveCassetteToSlot(tID, reservations));
            moveCassetteThread.Start();
            Thread openDoorThread = new(() => Locations[reservations[0].AccessFromLocation].OpenDoor(tID));
            openDoorThread.Start();

            moveCassetteThread.Join();
            openDoorThread.Join();
        }

        List<Payload> outputPayloads = new List<Payload>();
        foreach (Reservation reservation in reservations)
        {
            CheckAccessibility(reservation);
            outputPayloads.Add(Cassette!.RemovePayload(reservation));
        }
        return outputPayloads;
    }
    protected internal void PlaceInStation(string tID, List<Reservation> reservations)
    {
        if (State != StationState.BeingAccessed)
            throw new ErrorResponse(ErrorCode.IncorrectState, "Station Expected to be switched to Being Accessed.");
        CheckPod();
        CheckReservations(reservations);

        if (AutoMode)
        {
            Thread moveCassetteThread = new(() => MoveCassetteToSlot(tID, reservations));
            moveCassetteThread.Start();
            Thread openDoorThread = new(() => Locations[reservations[0].AccessFromLocation].OpenDoor(tID));
            openDoorThread.Start();

            moveCassetteThread.Join();
            openDoorThread.Join();
        }

        foreach (Reservation reservation in reservations)
        {
            CheckAccessibility(reservation);
            Cassette!.AddPayload(reservation);
            Log.Instance.Info(new LogMessage(tID, $"Reservation {reservation.Id} sent to Cassette"));
        }
    }

    protected internal void StartAccess(string tID, List<Reservation> reservations)
    {
        if (State != StationState.WaitingToBeAccessed)
            throw new ErrorResponse(ErrorCode.IncorrectState, "Station Expected to be waiting for Payload.");
        CheckReservations(reservations);

        State = StationState.BeingAccessed;
    }
    protected internal void StopAccess(string tID, List<Reservation> reservations)
    {
        RemoveReservationFromWaitingList(tID, reservations);
        if (PendingReservationIds.Count == 0)
        {
            State = StationState.Idle;
            if (AutoMode && Processable && IsFullAndReadyToProcess)
            {
                Process(tID);
                Log.Instance.Info(new LogMessage(tID, $"Station {StationId} started Auto Processing"));
            }
        }

    }

    private void CloseAllDoors(string tID)
    {
        List<Thread> threads = [];
        foreach (var location in Locations)
        {
            if (location.Value.HasDoor && location.Value.DoorStatus != DoorStatus.Closed)
            {
                Thread t = new(() => location.Value.CloseDoor(tID));
                t.Start();
                threads.Add(t);
            }
        }
        while (!AllClosableDoorsAreClosed)
        {
            Thread.Sleep(1);
        }
        foreach (Thread thread in threads)
        {
            if (thread.IsAlive)
            {
                thread.Join();
            }
        }
        Log.Instance.Info(new LogMessage(tID, $"Station {StationId} All closable doors closed."));
    }
    private void OpenAllDoors(string tID)
    {
        List<Thread> threads = [];
        foreach (var location in Locations)
        {
            if (location.Value.HasDoor && location.Value.DoorStatus != DoorStatus.Open)
            {
                Thread t = new(() => location.Value.OpenDoor(tID));
                t.Start();
                threads.Add(t);
            }
        }
        while (!AllDoorsAreOpened)
        {
            Thread.Sleep(1);
        }
        foreach (Thread thread in threads)
        {
            if (thread.IsAlive)
            {
                thread.Join();
            }
        }
        Log.Instance.Info(new LogMessage(tID, $"Station {StationId} All openable doors opened."));
    }
    private void MoveCassetteToSlot(string tID, List<Reservation> reservations)
    {
        int minSlot = 0;
        int maxSlot = 0;
        foreach (Reservation reservation in reservations)
        {
            if (minSlot == 0 || reservation.SlotId < minSlot)
                minSlot = reservation.SlotId;

            if (maxSlot == 0 || reservation.SlotId > maxSlot)
                maxSlot = reservation.SlotId;
        }

        if (Cassette!.CurrentSlot > reservations[0].SlotId || (Cassette!.CurrentSlot + Locations[reservations[0].AccessFromLocation].AccessiblePayloads) < reservations[0].SlotId)
            Cassette!.MoveToSlot(minSlot);
    }

    protected internal void Process(string tID, string? processName = null)
    {
        if (State != StationState.Idle)
            throw new ErrorResponse(ErrorCode.ActionWhileBusy, $"Station {StationId} is Busy");
        if (Cassette == null)
            throw new ErrorResponse(ErrorCode.PodNotAvailable, $"Station {StationId} does not contain expected cassette");
        if (!Processable)
            throw new ErrorResponse(ErrorCode.NotProcessable, $"Station {StationId} is not Processable");

        if (processName == null)
        {
            processName = Cassette.PayloadStateOfWafersInSlots ?? throw new ErrorResponse(ErrorCode.MissingArguments, $"Process name not provided and Cassette in Station {StationId} does not have Payload State set.");
            Log.Instance.Info(new LogMessage(tID, $"Station {StationId} inferred Process {processName} from Cassette Payload State"));
        }
        if (!Processes.ContainsKey(processName))
            throw new ErrorResponse(ErrorCode.ProcessNotAvailable, $"Process {processName} is not available at Station {StationId}");


        if (AutoMode)
        {
            State = StationState.Closing;
            CloseAllDoors(tID);
        }

        State = StationState.Processing;
        Log.Instance.Info(new LogMessage(tID, $"Station {StationId} started Process {processName}"));
        InternalClock.Instance.ProcessWait(Processes[processName].ProcessTime);
        Log.Instance.Info(new LogMessage(tID, $"Station {StationId} completed Process {processName}"));

        if (AutoMode)
        {
            string? NextLocation = Processes[processName].NextLocation;
            State = StationState.Opening;
            if (NextLocation is null || NextLocation == string.Empty)
            {
                OpenAllDoors(tID);
            }
            else
            {
                if (!Locations.ContainsKey(NextLocation))
                    throw new ErrorResponse(ErrorCode.NotAccessible, $"Next Location {NextLocation} is not accessible from Station {StationId}");

                Locations[NextLocation].OpenDoor(tID);
            }
        }

        Cassette.UpdatePayloadState(Processes[processName].OutputState);


        if (Processes[processName].NextLocation is not null)
        {
            CurrentLocation = Processes[processName].NextLocation!;
        }

        if (AutoMode && Locations[CurrentLocation].HasDoor)
        {
            State = StationState.Opening;
            Locations[CurrentLocation].OpenDoor(tID);
        }

        State = StationState.Idle;
    }


    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
