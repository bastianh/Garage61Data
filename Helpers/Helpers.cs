using System;
using SimHub;

namespace Garage61Data.Helpers
{
    public class Helper
    {
        public static void LogException(Exception ex)
        {
            if (ex is AggregateException aggregateException)
            {
                Logging.Current.Error("Garage61Data: AggregateException encountered:");
                foreach (var innerException in aggregateException.InnerExceptions)
                    Logging.Current.Error($"-- {innerException.Message}");
            }
            else
            {
                Logging.Current.Error($"Garage61Data: Exception: {ex.Message}");
            }
        }
    }
}