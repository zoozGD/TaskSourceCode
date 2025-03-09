using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices;

[System.Serializable]
public class GroupData
{
    public string groupId;
    public string name;
    public string grade;
    public string subject;
}

[System.Serializable]
public class GroupDataList
{
    public List<GroupData> groups;
}

[System.Serializable]
public class StudentData
{
    public string studentId;
    public string uniqueId; // Short unique student ID
}

[System.Serializable]
public class StudentsWrapper
{
    public List<StudentData> students;
}

public class TeacherDashboardUI : MonoBehaviour
{
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

    [Header("Create Group UI")]
    public TMP_InputField groupNameInput;
    public TMP_InputField gradeInput;
    public TMP_InputField subjectInput;
    public Button createGroupButton;

    private string selectedGroupId = "";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void CreateStudyGroupWebGL(string name, string grade, string subject);
    [DllImport("__Internal")] private static extern void LoadTeacherGroupsWebGL();
    [DllImport("__Internal")] private static extern void LoadRegisteredStudentsWebGL();
    [DllImport("__Internal")] private static extern void LoadAssignedStudentsWebGL(string groupId);
    [DllImport("__Internal")] private static extern void AddStudentToGroupWebGL(string groupId, string studentId);
    [DllImport("__Internal")] private static extern void RemoveStudentFromGroupWebGL(string groupId, string studentId);
    [DllImport("__Internal")] private static extern void DeleteStudyGroupWebGL(string groupId);
#endif

    void Start()
    {
        if (groupManagementPanel != null)
        {
            groupManagementPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("GroupManagementPanel is NULL! Assign it in the Inspector.");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        LoadTeacherGroupsWebGL();
        LoadRegisteredStudentsWebGL();
#endif

        backButton.onClick.AddListener(CloseGroupManagementPanel);
        createGroupButton.onClick.AddListener(CreateStudyGroup);
    }

    void CreateStudyGroup()
    {
        if (string.IsNullOrEmpty(groupNameInput.text) || string.IsNullOrEmpty(gradeInput.text) || string.IsNullOrEmpty(subjectInput.text))
        {
            Debug.LogWarning("Please fill in all fields before creating a group.");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        CreateStudyGroupWebGL(groupNameInput.text, gradeInput.text, subjectInput.text);
        Debug.Log("CreateStudyGroupWebGL called!");
        LoadTeacherGroupsWebGL();
#endif

        groupNameInput.text = "";
        gradeInput.text = "";
        subjectInput.text = "";
    }

    void InstantiateGroupUI(string groupId, string groupName, string grade, string subject)
    {
        GameObject groupItem = Instantiate(groupItemPrefab, groupsListContent);
        TMP_Text groupNameText = groupItem.transform.Find("GroupNameText").GetComponent<TMP_Text>();
        Button selectGroupButton = groupItem.transform.Find("SelectGroupButton").GetComponent<Button>();
        Button deleteGroupButton = groupItem.transform.Find("DeleteGroupButton").GetComponent<Button>();

        groupNameText.text = $"- {groupName} | {grade} | {subject} -";

        selectGroupButton.onClick.AddListener(() => OpenGroupManagementPanel(groupId, groupName));
        deleteGroupButton.onClick.AddListener(() => DeleteStudyGroup(groupId, groupItem));
    }

    void OpenGroupManagementPanel(string groupId, string groupName)
    {
        selectedGroupId = groupId;
        selectedGroupName.text = $"Managing: {groupName}";

        if (!groupManagementPanel.activeSelf)
        {
            groupManagementPanel.SetActive(true);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        LoadAssignedStudentsWebGL(groupId);
#endif
    }

    public void LoadTeacherGroups(string groupsJson)
    {
        foreach (Transform child in groupsListContent)
        {
            Destroy(child.gameObject);
        }

        try
        {
            GroupDataList wrapper = JsonUtility.FromJson<GroupDataList>(groupsJson);
            foreach (var group in wrapper.groups)
            {
                InstantiateGroupUI(group.groupId, group.name, group.grade, group.subject);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON Parsing Error: {e.Message}");
        }
    }

    public void LoadRegisteredStudents(string studentsJson)
    {
        foreach (Transform child in registeredStudentsListContent)
        {
            Destroy(child.gameObject);
        }

        try
        {
            StudentsWrapper wrapper = JsonUtility.FromJson<StudentsWrapper>(studentsJson);
            foreach (var student in wrapper.students)
            {
                InstantiateRegisteredStudentUI(student.studentId, student.uniqueId);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON Parsing Error: {e.Message}");
        }
    }

    public void LoadAssignedStudentsList(string studentsJson)
    {
        foreach (Transform child in assignedStudentsListContent)
        {
            Destroy(child.gameObject);
        }

        try
        {
            StudentsWrapper wrapper = JsonUtility.FromJson<StudentsWrapper>(studentsJson);
            foreach (var student in wrapper.students)
            {
                InstantiateAssignedStudentUI(student.studentId, student.uniqueId);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON Parsing Error: {e.Message}");
        }
    }

    void InstantiateRegisteredStudentUI(string studentId, string uniqueId)
    {
        GameObject studentItem = Instantiate(registeredStudentItemPrefab, registeredStudentsListContent);
        TMP_Text studentText = studentItem.transform.Find("StudentNameText")?.GetComponent<TMP_Text>();
        Button addButton = studentItem.transform.Find("AddStudentButton")?.GetComponent<Button>();
        TMP_Text addButtonText = addButton.GetComponentInChildren<TMP_Text>();

        if (studentText == null || addButton == null || addButtonText == null)
        {
            Debug.LogError("ERROR: Missing UI elements in RegisteredStudentItemPrefab! Check 'StudentNameText' and 'AddStudentButton'.");
            return;
        }

        studentText.text = uniqueId;

        addButton.onClick.AddListener(() =>
        {
            AddStudentToGroup(studentId);
            addButton.interactable = false; 
            addButtonText.text = "Added"; 
        });

        Debug.Log($"Registered student UI created: {uniqueId}");
    }


    void InstantiateAssignedStudentUI(string studentId, string uniqueId)
    {
        GameObject studentItem = Instantiate(assignedStudentItemPrefab, assignedStudentsListContent);
        TMP_Text studentText = studentItem.transform.Find("StudentNameText")?.GetComponent<TMP_Text>();
        Button removeButton = studentItem.transform.Find("RemoveStudentButton")?.GetComponent<Button>();

        if (studentText == null || removeButton == null)
        {
            Debug.LogError("ERROR: Missing UI elements in AssignedStudentItemPrefab! Check 'StudentNameText' and 'RemoveStudentButton'.");
            return;
        }

        string displayId = string.IsNullOrEmpty(uniqueId) ? studentId : uniqueId;
        studentText.text = displayId;

        Debug.Log($"Assigned student UI created: {displayId}");

        removeButton.onClick.AddListener(() => RemoveStudentFromGroup(studentId));
    }

    void AddStudentToGroup(string studentId)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        AddStudentToGroupWebGL(selectedGroupId, studentId);
        LoadRegisteredStudentsWebGL();
        LoadAssignedStudentsWebGL(selectedGroupId);
#endif
    }

    void RemoveStudentFromGroup(string studentId)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RemoveStudentFromGroupWebGL(selectedGroupId, studentId);
        LoadRegisteredStudentsWebGL();
        LoadAssignedStudentsWebGL(selectedGroupId);
#endif
    }

    void DeleteStudyGroup(string groupId, GameObject groupItem)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        DeleteStudyGroupWebGL(groupId);
#endif
        Destroy(groupItem);
    }

    void CloseGroupManagementPanel()
    {
        groupManagementPanel.SetActive(false);
        selectedGroupId = "";
    }
}
