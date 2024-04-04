using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CsvExporter : MonoBehaviour
{
    public void ExportListToCsv<T>(List<T> list, string filePath)
    {
        string delimiter = ";"; // Define the delimiter used in the CSV file

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write the header row (optional)
            string header = string.Join(delimiter, typeof(T).GetProperties().Select(p => p.Name));
            writer.WriteLine(header);

            // Write the data rows
            foreach (T item in list)
            {
                string dataRow = string.Join(delimiter, typeof(T).GetProperties().Select(p => p.GetValue(item, null).ToString()));
                writer.WriteLine(dataRow);
            }
        }
    }
}
