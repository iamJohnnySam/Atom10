using FlowModels.Command;
using FlowModels.Enum;
using Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Utilities;
using System.Linq;
using System.Threading;

namespace FlowModels;

public class Station : INotifyPropertyChanged
{
    // REQUIRED PARAMS
    public string StationId { get; set; }
    public bool AutoMode { get; set; }
    public Cassette? Cassette { get; set; }
    public Dictionary<string, Access> Locations { get; set; }
    public Dictionary<string, Process> Processes { get; set; }
    public bool IsInputAndPodDockable { get; set; }
    public bool IsOutputAndPodDockable { get; set; }
    public bool Processable { get; set; }
    public bool HighPriority { get; set; }


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
            if (string.IsNullOrEmpty(value))
                podId = null;
            else
                podId = value;
            OnPropertyChanged();
        }
    }
    protected internal bool PodDockable { get; set; }
    public List<int> PendingReservationIds { get; set; } = new List<int>();
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



    public Station(string stationId, bool autoMode, Cassette? cassette, Dictionary<string, Access> locations, Dictionary<string, Process> processes, 
        bool isInputAndPodDockable, bool isOutputAndPodDockable, bool processable, bool highPriority)
    {
        StationId = stationId;
        AutoMode = autoMode;
        Cassette = cassette;
        Locations = locations;
        Processes = processes;
        IsInputAndPodDockable = isInputAndPodDockable;
        IsOutputAndPodDockable= isOutputAndPodDockable;
        PodDockable = IsInputAndPodDockable || IsOutputAndPodDockable;
        Processable = processable;
        HighPriority = highPriority;

        // Guard required collections to avoid null/empty usage in constructor
        if (Locations == null || Locations.Count == 0)
            throw new ErrorResponse(ErrorCode.ProgramError, $"Station {StationId} must have at least one location defined.");

        // pick a deterministic starting location
        CurrentLocation = Locations.Keys.First();

        if (!PodDockable && Cassette == null)
            throw new ErrorResponse(ErrorCode.PodNotAvailable, $"Station {StationId} does not contain expected cassette.");
    }

    private void CheckPod()
    {
        // Avoid using PodId getter which throws; check backing field and PodDockable state
        if (Cassette == null || (PodDockable && podId == null))
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

        if (!Locations.ContainsKey(accessFromLocation))
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Location {accessFromLocation} is Out of Bounds");

        if (CurrentLocation != accessFromLocation)
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Station {StationId} is not currently at location {accessFromLocation}");

        if (startingSlot == 0)
            startingSlot = Cassette.GetNextOccupiedSlot();

        if (startingSlot < 0)
            throw new ErrorResponse(ErrorCode.SlotOutOfBounds, $"Starting slot cannot be less than 1. Or you did not pass in a starting slot and all slots are unavailable.");

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

        if (!Locations.ContainsKey(accessFromLocation))
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Location {accessFromLocation} is Out of Bounds");

        if (CurrentLocation != accessFromLocation)
            throw new ErrorResponse(ErrorCode.NotAccessible, $"Station {StationId} is not currently at location {accessFromLocation}");

        if (startingSlot == 0)
            startingSlot = Cassette.GetNextEmptySlot();

        if (startingSlot < 0)
            throw new ErrorResponse(ErrorCode.SlotOutOfBounds, $"Starting slot cannot be less than 1. Or you did not pass in a starting slot and all slots are unavailable.");

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
            TransactionLogger.Instance.Info(new TransactionLog(tID, $"Reservation {reservation.Id} sent to Cassette"));
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
                TransactionLogger.Instance.Info(new TransactionLog(tID, $"Station {StationId} started Auto Processing"));
            }
        }

    }

    private void CloseAllDoors(string tID)
    {
        List<Thread> threads = new List<Thread>();
        foreach (var location in Locations)
        {
            if (location.Value.HasDoor && location.Value.DoorStatus != DoorStatus.Closed)
            {
                Thread t = new(() => location.Value.CloseDoor(tID));
                t.Start();
                threads.Add(t);
            }
        }

        // Join started door threads with a timeout to avoid infinite busy-wait
        const int joinTimeoutMs = 5000;
        foreach (Thread thread in threads)
        {
            thread.Join(joinTimeoutMs);
        }

        if (!AllClosableDoorsAreClosed)
            throw new ErrorResponse(ErrorCode.IncorrectState, $"Not all doors could be closed for Station {StationId} within timeout.");

        TransactionLogger.Instance.Info(new TransactionLog(tID, $"Station {StationId} All closable doors closed."));
    }
    private void OpenAllDoors(string tID)
    {
        List<Thread> threads = new List<Thread>();
        foreach (var location in Locations)
        {
            if (location.Value.HasDoor && location.Value.DoorStatus != DoorStatus.Open)
            {
                Thread t = new(() => location.Value.OpenDoor(tID));
                t.Start();
                threads.Add(t);
            }
        }

        const int joinTimeoutMs = 5000;
        foreach (Thread thread in threads)
        {
            thread.Join(joinTimeoutMs);
        }

        if (!AllDoorsAreOpened)
            throw new ErrorResponse(ErrorCode.IncorrectState, $"Not all doors could be opened for Station {StationId} within timeout.");

        TransactionLogger.Instance.Info(new TransactionLog(tID, $"Station {StationId} All openable doors opened."));
    }
    private void MoveCassetteToSlot(string tID, List<Reservation> reservations)
    {
        if (reservations == null || reservations.Count == 0)
            return;

        int minSlot = int.MaxValue;
        int maxSlot = int.MinValue;
        foreach (Reservation reservation in reservations)
        {
            if (reservation.SlotId < minSlot)
                minSlot = reservation.SlotId;

            if (reservation.SlotId > maxSlot)
                maxSlot = reservation.SlotId;
        }

        string accessFrom = reservations[0].AccessFromLocation;
        int accessible = Locations[accessFrom].AccessiblePayloads;

        // If cassette window does not include [minSlot..maxSlot], move to minSlot
        if (Cassette!.CurrentSlot > minSlot || (Cassette!.CurrentSlot + accessible - 1) < maxSlot)
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
            TransactionLogger.Instance.Info(new TransactionLog(tID, $"Station {StationId} inferred Process {processName} from Cassette Payload State"));
        }
        if (!Processes.ContainsKey(processName))
            throw new ErrorResponse(ErrorCode.ProcessNotAvailable, $"Process {processName} is not available at Station {StationId}");


        if (AutoMode)
        {
            State = StationState.Closing;
            CloseAllDoors(tID);
        }

        State = StationState.Processing;
        TransactionLogger.Instance.Info(new TransactionLog(tID, $"Station {StationId} started Process {processName}"));
        InternalClock.Instance.ProcessWait(Processes[processName].ProcessTime);
        TransactionLogger.Instance.Info(new TransactionLog(tID, $"Station {StationId} completed Process {processName}"));

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