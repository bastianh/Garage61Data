using System;
using System.Collections.Generic;
using System.IO;

namespace Garage61Data.Helpers
{
    public class CsvParser
    {
        public static List<T> ParseCsvFromString<T>(string csvContent, Func<string[], T> mapFunction)
        {
            var result = new List<T>();

            using (var reader = new StringReader(csvContent))
            {
                string line;
                var lineNumber = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    if (lineNumber == 0) // Header überspringen, wenn nötig
                    {
                        lineNumber++;
                        continue;
                    }

                    // Zerlege die aktuelle Zeile in Teile (basierend auf ",")
                    var columns = line.Split(',');

                    // Verwenden Sie die mapFunction, um ein Objekt zu erstellen
                    var obj = mapFunction(columns);
                    result.Add(obj);

                    lineNumber++;
                }
            }

            return result;
        }
    }
}