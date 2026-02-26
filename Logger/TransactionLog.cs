using System;
using System.Collections.Generic;
using System.Text;

namespace Logger;

public class TransactionLog
{
    public string transactionID;
    public string message;

    public TransactionLog(string tID, string msg)
    {
        transactionID = tID;
        message = msg;
    }

    public TransactionLog(string msg)
    {
        transactionID = string.Empty;
        message = msg;
    }

}