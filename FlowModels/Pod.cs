using System;
using System.Collections.Generic;
using System.Text;

namespace FlowModels;

public class Pod()
{
    public required string PodID { get; set; }
    public required Cassette Cassette { get; set; }
}
