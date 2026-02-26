using System;
using System.Collections.Generic;
using System.Text;

namespace Logger;

public sealed class TransactionLogger
{

    private static TransactionLogger? instance = null;

    private static readonly object padlock = new();

    SqliteLogger log = new SqliteLogger();

    private TransactionLogger()
    {

    }

    public static TransactionLogger Instance
    {
        get
        {
            lock (padlock)
            {
                instance ??= new TransactionLogger();
                return instance;
            }
        }
    }

    string Message (TransactionLog message)
    {
        if(message.transactionID == string.Empty)
        {
            return message.message;
        }
        else
        {
            return $"{message.transactionID}, {message.message}";
        }
    }

    public void Info(TransactionLog message)
    {
        log.Info(Message(message), "Flow");
    }

    public void Debug(TransactionLog message)
    {
        log.Debug(Message(message), "Flow");
    }

    public void Warn(TransactionLog message)
    {
        log.Warn(Message(message), "Flow");
    }

    public void Error(TransactionLog message)
    {
        log.Error(Message(message), "Flow");
    }
}