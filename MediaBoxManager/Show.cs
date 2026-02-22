using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBoxManager;

internal class Show
{
	public required string BaseName { get; set; }
	public int Season { get; set; }
	public int Episode { get; set; }
	public required string Magnet { get; set; }
	public required string Quality { get; set; }
}
