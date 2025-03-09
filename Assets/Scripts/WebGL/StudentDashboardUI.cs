using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices;

[System.Serializable]
public class StudentGroupData
{
    public string groupId;
    public string name;
    public string grade;
    public string subject;
    public int studentCount;
}

[System.Serializable]
public class StudentGroupDataList
{
    public List<StudentGroupData> groups;
}

public class StudentDashboardUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform groupsListContent; // ScrollView content for listing groups
    public GameObject groupItemPrefab; // Prefab for displaying group info

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void LoadStudentGroupsWebGL();
#endif

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        LoadStudentGroupsWebGL();
#endif
    }

    public void LoadStudentGroups(string groupsJson)
    {
        Debug.Log("LoadStudentGroups() called!");

        if (string.IsNullOrEmpty(groupsJson))
        {
            Debug.LogWarning("LoadStudentGroups received an empty JSON!");
            return;
        }

        Debug.Log($"Received student groups JSON: {groupsJson}");

        try
        {
            Debug.Log("Attempting to parse JSON...");
            StudentGroupDataList wrapper = JsonUtility.FromJson<StudentGroupDataList>(groupsJson);

            if (wrapper == null || wrapper.groups == null)
            {
                Debug.LogWarning("JSON Parsing Failed! Wrapper or Groups list is NULL.");
                return;
            }

            Debug.Log($"Successfully parsed {wrapper.groups.Count} student groups.");

            // Clear the UI before adding new groups
            foreach (Transform child in groupsListContent)
            {
                Destroy(child.gameObject);
            }

            foreach (var group in wrapper.groups)
            {
                Debug.Log($"Instantiating Student Group: {group.name} | Grade: {group.grade} | Subject: {group.subject} | Students: {group.studentCount}");
                InstantiateGroupUI(group.groupId, group.name, group.grade, group.subject, group.studentCount);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON Parsing Error: {e.Message}");
        }
    }

    void InstantiateGroupUI(string groupId, string groupName, string grade, string subject, int studentCount)
    {
        if (groupItemPrefab == null || groupsListContent == null)
        {
            Debug.LogError("ERROR: groupItemPrefab or groupsListContent is NULL! Assign it in the Inspector.");
            return;
        }

        GameObject groupItem = Instantiate(groupItemPrefab, groupsListContent);

        TMP_Text groupNameText = groupItem.transform.Find("GroupNameText")?.GetComponent<TMP_Text>();
        TMP_Text studentCountText = groupItem.transform.Find("StudentCountText")?.GetComponent<TMP_Text>();

        if (groupNameText == null || studentCountText == null)
        {
            Debug.LogError("ERROR: Missing UI elements in GroupItemPrefab! Check 'GroupNameText' and 'StudentCountText'.");
            return;
        }

        groupNameText.text = $"{groupName} | Grade: {grade} | Subject: {subject}";
        studentCountText.text = $"Students: {studentCount}";

        Debug.Log($"Student group UI created: {groupName} (ID: {groupId})");
    }
}
