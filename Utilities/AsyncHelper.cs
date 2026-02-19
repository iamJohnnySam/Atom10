using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities;

public static class AsyncHelper
{
    public static void RunInBackground(Func<Task> func)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log or handle exception
                Console.Error.WriteLine(ex);
            }
        });
    }
}

