#if UNITY_ANDROID
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AuthManagerAndroid : MonoBehaviour
{
    [Header("Firebase")]
    private FirebaseAuth auth;
    private DatabaseReference databaseReference;

    [Header("Login UI")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginButton;

    [Header("Signup UI")]
    public TMP_InputField signupEmailInput;
    public TMP_InputField signupPasswordInput;
    public TMP_Dropdown roleDropdown;
    public Button signUpButton;

    [Header("Status Display")]
    public TMP_Text statusText;

    async void Start()
    {
        statusText.text = "Initializing Firebase...";

        await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                statusText.text = "";
            }
            else
            {
                statusText.text = "Firebase init failed!";
                Debug.LogError("Firebase dependencies are not available!");
            }
        });

        loginButton.onClick.AddListener(Login);
        signUpButton.onClick.AddListener(SignUp);
    }

    async void Login()
    {
        if (string.IsNullOrEmpty(loginEmailInput.text) || string.IsNullOrEmpty(loginPasswordInput.text))
        {
            statusText.text = "Please enter email and password.";
            return;
        }

        loginButton.interactable = false;
        statusText.text = "Logging in...";

        try
        {
            FirebaseUser user = (await auth.SignInWithEmailAndPasswordAsync(loginEmailInput.text, loginPasswordInput.text)).User;
            if (user != null)
            {
                statusText.text = "Login successful!";
                FetchUserRole(user.UserId);
            }
            else
            {
                statusText.text = "Login failed: No user found.";
            }
        }
        catch (System.Exception e)
        {
            statusText.text = "Login failed: " + e.Message;
            loginButton.interactable = true;
        }
    }

    async void SignUp()
    {
        if (string.IsNullOrEmpty(signupEmailInput.text) || string.IsNullOrEmpty(signupPasswordInput.text))
        {
            statusText.text = "Please enter email and password.";
            return;
        }

        if (signupPasswordInput.text.Length < 6)
        {
            statusText.text = "Password must be at least 6 characters long.";
            return;
        }

        signUpButton.interactable = false;
        statusText.text = "Signing up...";

        try
        {
            FirebaseUser user = (await auth.CreateUserWithEmailAndPasswordAsync(signupEmailInput.text, signupPasswordInput.text)).User;
            if (user == null)
            {
                statusText.text = "Signup failed!";
                signUpButton.interactable = true;
                return;
            }

            string role = roleDropdown.value == 0 ? "teacher" : "student";
            string studentId = (role == "student") ? GenerateUniqueStudentID() : "";

            var userData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "role", role },
                { "email", signupEmailInput.text }
            };

            if (role == "student") userData["studentId"] = studentId;

            await databaseReference.Child("users").Child(user.UserId).SetValueAsync(userData);

            statusText.text = "Signup successful!";
        }
        catch (System.Exception e)
        {
            statusText.text = "Signup failed: " + e.Message;
        }
        finally
        {
            signUpButton.interactable = true;
        }
    }

    void FetchUserRole(string userId)
    {
        databaseReference.Child("users").Child(userId).Child("role").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.Result == null || task.Result.Value == null)
            {
                statusText.text = "Error fetching role.";
                return;
            }

            string role = task.Result.Value.ToString();
            statusText.text = "Role: " + role;

            SceneManager.LoadScene(role == "teacher" ? "TeachersDashboard_Android" : "StudentsDashboard_Android");
        });
    }

    private string GenerateUniqueStudentID()
    {
        string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string numbers = "0123456789";

        string randomLetters = letters[Random.Range(0, letters.Length)].ToString() + letters[Random.Range(0, letters.Length)].ToString();
        string randomNumbers = numbers[Random.Range(0, numbers.Length)].ToString() + numbers[Random.Range(0, numbers.Length)].ToString();

        return randomLetters + randomNumbers;
    }
}
#endif
