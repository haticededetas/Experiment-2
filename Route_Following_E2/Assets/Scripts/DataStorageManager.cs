using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class DataStorageManager : MonoBehaviour
{
    private List<DecisionData> decisionDataList = new List<DecisionData>();

    public void AddDecisionData(string decisionPointName, int currentDecisionIndex)
    {
        DecisionData decisionData = new DecisionData(decisionPointName, currentDecisionIndex);
        decisionDataList.Add(decisionData);
    }

    public void SaveDecisionDataToCSV()
    {
        StringBuilder sb = new StringBuilder();

        // Add header to the CSV
        sb.AppendLine("DecisionPointName,CurrentDecisionIndex");

        // Add decision data to the CSV
        foreach (DecisionData decisionData in decisionDataList)
        {
            sb.AppendLine($"{decisionData.decisionPointName},{decisionData.currentDecisionIndex}");
        }

        // Save the CSV file
        string filePath = Path.Combine(Application.persistentDataPath, "decision_data.csv");
        File.WriteAllText(filePath, sb.ToString());
    }

    private void OnDestroy()
    {
        SaveDecisionDataToCSV();
    }
}

public struct DecisionData
{
    public string decisionPointName;
    public int currentDecisionIndex;

    public DecisionData(string decisionPointName, int currentDecisionIndex)
    {
        this.decisionPointName = decisionPointName;
        this.currentDecisionIndex = currentDecisionIndex;
    }
}
