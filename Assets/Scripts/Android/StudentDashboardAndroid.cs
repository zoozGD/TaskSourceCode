#if UNITY_ANDROID
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

[System.Serializable]
public class AndroidStudentGroupData
{
    public string androidGroupId;
    public string androidGroupName;
    public string androidGrade;
    public string androidSubject;
    public int androidStudentCount;
}

[System.Serializable]
public class AndroidStudentGroupDataList
{
    public List<AndroidStudentGroupData> androidGroups;
}

public class StudentDashboardAndroid : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform androidGroupsListContent; // ScrollView content for listing student groups
    public GameObject androidGroupItemPrefab; // Prefab for displaying group info

    private FirebaseAuth androidAuth;
    private FirebaseUser androidUser;
    private DatabaseReference androidDbReference;

    void Start()
    {
        androidAuth = FirebaseAuth.DefaultInstance;
        androidUser = androidAuth.CurrentUser;
        androidDbReference = FirebaseDatabase.DefaultInstance.RootReference;

        if (androidUser != null)
        {
            FetchStudentGroupsAndroid();
        }
        else
        {
            Debug.LogError("No authenticated user found for Android.");
        }
    }

    void FetchStudentGroupsAndroid()
    {
        if (androidUser == null)
        {
            Debug.LogError("Android user not authenticated.");
            return;
        }

        string androidStudentId = androidUser.UserId;
        Debug.Log($"Fetching groups for Android Student ID: {androidStudentId}");

        androidDbReference.Child("groups").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to fetch student groups (Android): " + task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                List<AndroidStudentGroupData> androidGroups = new List<AndroidStudentGroupData>();

                foreach (var group in snapshot.Children)
                {
                    Dictionary<string, object> groupData = group.Value as Dictionary<string, object>;
                    if (groupData == null) continue; // Skip null groups

                    if (groupData.ContainsKey("students") && groupData["students"] is Dictionary<string, object> androidStudents && androidStudents.ContainsKey(androidStudentId))
                    {
                        AndroidStudentGroupData androidStudentGroup = new AndroidStudentGroupData
                        {
                            androidGroupId = group.Key,
                            androidGroupName = groupData.ContainsKey("name") ? groupData["name"].ToString() : "Unknown",
                            androidGrade = groupData.ContainsKey("grade") ? groupData["grade"].ToString() : "Unknown",
                            androidSubject = groupData.ContainsKey("subject") ? groupData["subject"].ToString() : "Unknown",
                            androidStudentCount = androidStudents.Count
                        };
                        androidGroups.Add(androidStudentGroup);
                    }
                }

                string json = JsonUtility.ToJson(new AndroidStudentGroupDataList { androidGroups = androidGroups });
                Debug.Log($"Successfully parsed {androidGroups.Count} student groups for Android.");
                StartCoroutine(PopulateStudentGroupsAndroid(json));
            }
        });
    }

    IEnumerator PopulateStudentGroupsAndroid(string androidGroupsJson)
    {
        yield return new WaitForEndOfFrame(); // Ensures it runs on the main thread
        LoadStudentGroupsAndroid(androidGroupsJson);
    }

    public void LoadStudentGroupsAndroid(string androidGroupsJson)
    {
        Debug.Log("LoadStudentGroupsAndroid() called!");

        if (string.IsNullOrEmpty(androidGroupsJson))
        {
            Debug.LogWarning("LoadStudentGroupsAndroid received an empty JSON!");
            return;
        }

        Debug.Log($"Received student groups JSON (Android): {androidGroupsJson}");

        try
        {
            Debug.Log("Attempting to parse JSON for Android...");
            AndroidStudentGroupDataList wrapper = JsonUtility.FromJson<AndroidStudentGroupDataList>(androidGroupsJson);

            if (wrapper == null || wrapper.androidGroups == null)
            {
                Debug.LogWarning("JSON Parsing Failed (Android)! Wrapper or Groups list is NULL.");
                return;
            }

            Debug.Log($"Successfully parsed {wrapper.androidGroups.Count} student groups for Android.");

            // Clear the UI before adding new groups
            foreach (Transform child in androidGroupsListContent)
            {
                Destroy(child.gameObject);
            }

            foreach (var group in wrapper.androidGroups)
            {
                Debug.Log($"Instantiating Student Group (Android): {group.androidGroupName} | Grade: {group.androidGrade} | Subject: {group.androidSubject} | Students: {group.androidStudentCount}");
                InstantiateAndroidGroupUI(group.androidGroupId, group.androidGroupName, group.androidGrade, group.androidSubject, group.androidStudentCount);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON Parsing Error (Android): {e.Message}");
        }
    }

    void InstantiateAndroidGroupUI(string androidGroupId, string androidGroupName, string androidGrade, string androidSubject, int androidStudentCount)
    {
        if (androidGroupItemPrefab == null || androidGroupsListContent == null)
        {
            Debug.LogError("ERROR: androidGroupItemPrefab or androidGroupsListContent is NULL! Assign it in the Inspector.");
            return;
        }

        GameObject androidGroupItem = Instantiate(androidGroupItemPrefab, androidGroupsListContent);

        TMP_Text androidGroupNameText = androidGroupItem.transform.Find("GroupNameText")?.GetComponent<TMP_Text>();
        TMP_Text androidStudentCountText = androidGroupItem.transform.Find("StudentCountText")?.GetComponent<TMP_Text>();

        if (androidGroupNameText == null || androidStudentCountText == null)
        {
            Debug.LogError("ERROR: Missing UI elements in androidGroupItemPrefab! Check 'GroupNameText' and 'StudentCountText'.");
            return;
        }

        androidGroupNameText.text = $"{androidGroupName} | Grade: {androidGrade} | Subject: {androidSubject}";
        androidStudentCountText.text = $"Students: {androidStudentCount}";

        Debug.Log($"Android Student group UI created: {androidGroupName} (ID: {androidGroupId})");
    }
}
#endif
