#if UNITY_ANDROID 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;

[System.Serializable]
public class AndroidStudentData
{
    public string studentId;
    public string uniqueId;
}

[System.Serializable]
public class AndroidStudentsWrapper
{
    public List<AndroidStudentData> students;
}

public class TeacherDashboardAndroid : MonoBehaviour
{
    [Header("Create Group UI")]
    public TMP_InputField groupNameInput;
    public TMP_InputField gradeInput;
    public TMP_InputField subjectInput;
    public Button createGroupButton;


    [Header("Main Dashboard UI")]
    public Transform groupsListContent;
    public GameObject groupItemPrefab;
    public GameObject groupManagementPanel;

    [Header("Group Management Panel UI")]
    public TMP_Text selectedGroupName;
    public Transform assignedStudentsListContent;
    public Transform registeredStudentsListContent;
    public GameObject assignedStudentItemPrefab;
    public GameObject registeredStudentItemPrefab;
    public Button backButton;

    private DatabaseReference dbReference;
    private FirebaseAuth auth;
    private string selectedGroupId = "";

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        if (auth.CurrentUser != null)
        {
            LoadTeacherGroupsAndroid();
            LoadRegisteredStudents();
        }
        else
        {
            Debug.LogError("No authenticated teacher found.");
        }

        if (groupManagementPanel != null)
        {
            groupManagementPanel.SetActive(false);
        }

        createGroupButton.onClick.AddListener(CreateStudyGroupAndroid);
        backButton.onClick.AddListener(CloseGroupManagementPanel);
    }

    void LoadTeacherGroupsAndroid()
    {
        string teacherId = auth.CurrentUser.UserId;
        Debug.Log($"Fetching groups for Teacher ID: {teacherId}");

        dbReference.Child("groups").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to fetch teacher groups: " + task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                foreach (Transform child in groupsListContent)
                {
                    Destroy(child.gameObject);
                }

                foreach (var group in snapshot.Children)
                {
                    Dictionary<string, object> groupData = group.Value as Dictionary<string, object>;
                    if (groupData == null || !groupData.ContainsKey("teacherId")) continue;

                    if (groupData["teacherId"].ToString() == teacherId)
                    {
                        InstantiateGroupUI(group.Key, groupData["name"].ToString(), groupData["grade"].ToString(), groupData["subject"].ToString());
                    }
                }
            }
        });
    }

    void LoadRegisteredStudents()
    {
        dbReference.Child("users").OrderByChild("role").EqualTo("student").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to load registered students: " + task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                foreach (Transform child in registeredStudentsListContent)
                {
                    Destroy(child.gameObject);
                }

                foreach (var child in snapshot.Children)
                {
                    string studentId = child.Key;
                    string uniqueId = child.Child("studentId").Value?.ToString() ?? "N/A";
                    InstantiateRegisteredStudentUI(studentId, uniqueId);
                }
            }
        });
    }

    void LoadAssignedStudents(string groupId)
    {
        dbReference.Child("groups").Child(groupId).Child("students").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to load assigned students: " + task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                foreach (Transform child in assignedStudentsListContent)
                {
                    Destroy(child.gameObject);
                }

                foreach (var student in snapshot.Children)
                {
                    string studentId = student.Key;
                    string uniqueId = student.Value.ToString();
                    InstantiateAssignedStudentUI(studentId, uniqueId);
                }
            }
        });
    }

    void InstantiateRegisteredStudentUI(string studentId, string uniqueId)
    {
        GameObject studentItem = Instantiate(registeredStudentItemPrefab, registeredStudentsListContent);
        TMP_Text studentText = studentItem.transform.Find("StudentNameText")?.GetComponent<TMP_Text>();
        Button addButton = studentItem.transform.Find("AddStudentButton")?.GetComponent<Button>();

        if (studentText == null || addButton == null)
        {
            Debug.LogError("ERROR: Missing UI elements in RegisteredStudentItemPrefab!");
            return;
        }

        studentText.text = uniqueId;
        addButton.onClick.AddListener(() => AddStudentToGroup(studentId, uniqueId));
    }

    void InstantiateAssignedStudentUI(string studentId, string uniqueId)
    {
        GameObject studentItem = Instantiate(assignedStudentItemPrefab, assignedStudentsListContent);
        TMP_Text studentText = studentItem.transform.Find("StudentNameText")?.GetComponent<TMP_Text>();
        Button removeButton = studentItem.transform.Find("RemoveStudentButton")?.GetComponent<Button>();

        if (studentText == null || removeButton == null)
        {
            Debug.LogError("ERROR: Missing UI elements in AssignedStudentItemPrefab!");
            return;
        }

        studentText.text = uniqueId;
        removeButton.onClick.AddListener(() => RemoveStudentFromGroup(studentId));
    }

    void InstantiateGroupUI(string groupId, string groupName, string grade, string subject)
    {
        GameObject groupItem = Instantiate(groupItemPrefab, groupsListContent);
        TMP_Text groupNameText = groupItem.transform.Find("GroupNameText")?.GetComponent<TMP_Text>();
        Button selectGroupButton = groupItem.transform.Find("SelectGroupButton")?.GetComponent<Button>();
        Button deleteGroupButton = groupItem.transform.Find("DeleteGroupButton")?.GetComponent<Button>();

        if (groupNameText == null || selectGroupButton == null || deleteGroupButton == null)
        {
            Debug.LogError("ERROR: Missing UI elements in GroupItemPrefab!");
            return;
        }

        groupNameText.text = $"- {groupName} | {grade} | {subject} -";

        selectGroupButton.onClick.AddListener(() => OpenGroupManagementPanel(groupId, groupName));
        deleteGroupButton.onClick.AddListener(() => DeleteStudyGroup(groupId, groupItem));
    }

    void DeleteStudyGroup(string groupId, GameObject groupItem)
    {
        dbReference.Child("groups").Child(groupId).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Destroy(groupItem);
                LoadTeacherGroupsAndroid();
            }
            else
            {
                Debug.LogError("Failed to delete group: " + task.Exception);
            }
        });
    }

    void AddStudentToGroup(string studentId, string uniqueId)
    {
        if (string.IsNullOrEmpty(selectedGroupId))
        {
            Debug.LogError("No group selected for student addition!");
            return;
        }

        string studentPath = $"users/{studentId}/studentId";
        string groupPath = $"groups/{selectedGroupId}/students/{studentId}";

        dbReference.Child(studentPath).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                Debug.LogError($"Student unique ID not found for {studentId}");
                return;
            }

            string fetchedUniqueId = task.Result.Value.ToString();
            dbReference.Child(groupPath).SetValueAsync(fetchedUniqueId).ContinueWithOnMainThread(updateTask =>
            {
                if (updateTask.IsCompleted)
                {
                    LoadAssignedStudents(selectedGroupId);
                    LoadRegisteredStudents();
                }
                else
                {
                    Debug.LogError("Failed to add student: " + updateTask.Exception);
                }
            });
        });
    }

    void RemoveStudentFromGroup(string studentId)
    {
        if (string.IsNullOrEmpty(selectedGroupId))
        {
            Debug.LogError("No group selected for student removal!");
            return;
        }

        dbReference.Child("groups").Child(selectedGroupId).Child("students").Child(studentId).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                LoadAssignedStudents(selectedGroupId);
                LoadRegisteredStudents();
            }
            else
            {
                Debug.LogError("Failed to remove student: " + task.Exception);
            }
        });
    }

    void CreateStudyGroupAndroid()
    {
        if (string.IsNullOrEmpty(groupNameInput.text) || string.IsNullOrEmpty(gradeInput.text) || string.IsNullOrEmpty(subjectInput.text))
        {
            Debug.LogWarning("Please fill in all fields before creating a group.");
            return;
        }

        string groupId = dbReference.Child("groups").Push().Key; // Generate a unique group ID
        string teacherId = auth.CurrentUser.UserId; // Get current teacher ID

        // Create group data
        Dictionary<string, object> groupData = new Dictionary<string, object>
    {
        { "name", groupNameInput.text },
        { "grade", gradeInput.text },
        { "subject", subjectInput.text },
        { "teacherId", teacherId } // Assign teacher ID
    };

        // Store group in Firebase
        dbReference.Child("groups").Child(groupId).SetValueAsync(groupData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"Group '{groupNameInput.text}' Created Successfully.");
                LoadTeacherGroupsAndroid(); // Refresh groups list
            }
            else
            {
                Debug.LogError("Failed to create group: " + task.Exception);
            }
        });

        // Clear input fields after creation
        groupNameInput.text = "";
        gradeInput.text = "";
        subjectInput.text = "";
    }

    void OpenGroupManagementPanel(string groupId, string groupName)
    {
        selectedGroupId = groupId;
        selectedGroupName.text = $"Managing: {groupName}";
        groupManagementPanel.SetActive(true);
        LoadAssignedStudents(groupId);
    }

    void CloseGroupManagementPanel()
    {
        groupManagementPanel.SetActive(false);
        selectedGroupId = "";
    }
}
#endif