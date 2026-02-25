using System;
using System.Collections.Generic;
using System.Text;

namespace FlowModels;

public class Process
{
    public required string ProcessName { get; set; }
    public required string InputState { get; set; }
    public required string OutputState { get; set; }
    public string? NextLocation { get; set; }
    public uint ProcessTime { get; set; }
}
