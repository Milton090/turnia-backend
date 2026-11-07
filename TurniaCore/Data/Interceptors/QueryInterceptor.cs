using TurniaCore.Libraries.DateTimeLibrary;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace TurniaCore.Data.Interceptors
{
    public class QueryInterceptor : DbCommandInterceptor
    {
        private int slowQueryThresholdInMilliSecond = 5000;
        private bool enableLogging;

        public QueryInterceptor(bool enLogging, int? tresholdMs)
        {
            enableLogging = enLogging;
            if(tresholdMs != null)
            {
                slowQueryThresholdInMilliSecond = tresholdMs.Value;
            }
        }
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            QueryInterceptorLogToConsole(slowQueryThresholdInMilliSecond, enableLogging, command, eventData, result);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }
        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            QueryInterceptorLogToConsole(slowQueryThresholdInMilliSecond, enableLogging, command, eventData, result);
            return base.ReaderExecuted(command, eventData, result);
        }

        private void QueryInterceptorLogToConsole(int queryTreshold, bool enableLogging, DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            if (eventData.Duration.TotalMilliseconds > slowQueryThresholdInMilliSecond && enableLogging)
            {
                try
                {
                    Console.WriteLine($"Slow Query Detected @ {DateTimeLibrary.Now.ToString("g")}: {command.CommandText} | Duration (Seconds): {eventData.Duration.TotalSeconds} Duration (Ms): {eventData.Duration.TotalMilliseconds}");
                } catch
                {
                }
            }
        }
    }
}
