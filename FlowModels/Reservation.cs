using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace FlowModels;

public class Reservation
{
    public required EReservationType Type { get; set; }
    public int Id { get; set; } = GenerateId.Instance.GetReservationId();
    public Station? TargetStation { get; set; }
    public required Slot Slot { get; set; }
    public int SlotId { get { return Slot.SlotId; } }
    public required Payload Payload { get; set; }
    public string PayloadType { get { return Payload.PayloadType; } }
    public string AccessFromLocation { get; set; } = string.Empty;
}
