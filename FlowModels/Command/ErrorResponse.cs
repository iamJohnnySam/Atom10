using FlowModels.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowModels.Command;

public class ErrorResponse : Exception
{
    public ErrorCode Code { get; private set; }
    public string ErrorMessage { get; private set; }

    public ErrorResponse(ErrorCode code, string message) : base($"An error occurred: {code}")
    {
        Code = code;
        ErrorMessage = message;
    }
}