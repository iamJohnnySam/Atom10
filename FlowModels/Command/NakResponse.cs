using FlowModels.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowModels.Command;

public class NAckResponse : Exception
{
    public NAckCode Code { get; private set; }
    public string ErrorMessage { get; private set; }

    public NAckResponse(NAckCode code, string message) : base($"An error occurred: {code}")
    {
        Code = code;
        ErrorMessage = message;
    }
}
