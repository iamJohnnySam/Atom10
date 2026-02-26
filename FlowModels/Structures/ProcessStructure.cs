using System;
using System.Collections.Generic;
using System.Text;

namespace FlowModels.Structures;

public class ProcessStructure
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "Untitled Process";
    public string? InputState { get; set; }
    public string? OutputState { get; set; }
    public string? NextLocation { get; set; }
    public uint ProcessTime { get; set; } = 1;
}
