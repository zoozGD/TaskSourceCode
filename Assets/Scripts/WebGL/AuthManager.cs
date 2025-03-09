#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
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

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void InitializeFirebase();

    [DllImport("__Internal")]
    private static extern void SignUpUser(string email, string password, int roleIndex);

    [DllImport("__Internal")]
    private static extern void LoginUser(string email, string password);

    [DllImport("__Internal")]
    private static extern void FetchUserRole(string userId);
#endif

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InitializeFirebase();
#endif
        loginButton.onClick.AddListener(Login);
        signUpButton.onClick.AddListener(SignUp);
    }

    void Login()
    {
        if (string.IsNullOrEmpty(loginEmailInput.text) || string.IsNullOrEmpty(loginPasswordInput.text))
        {
            statusText.text = "Please enter email and password.";
            return;
        }

        loginButton.interactable = false; // Disable button to prevent spam
        statusText.text = "Logging in...";

#if UNITY_WEBGL && !UNITY_EDITOR
        LoginUser(loginEmailInput.text, loginPasswordInput.text);
#endif
    }

    void SignUp()
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

        signUpButton.interactable = false; // Disable button while processing
        statusText.text = "Signing up...";

#if UNITY_WEBGL && !UNITY_EDITOR
        SignUpUser(signupEmailInput.text, signupPasswordInput.text, roleDropdown.value);
#endif
    }

    public void OnLoginSuccess(string userId)
    {
        statusText.text = "Login successful!";
        loginButton.interactable = true; // Re-enable login button

#if UNITY_WEBGL && !UNITY_EDITOR
        FetchUserRole(userId);
#endif
    }

    public void OnLoginFailed(string error)
    {
        statusText.text = "Login failed: " + error;
        loginButton.interactable = true; // Re-enable button on failure
    }

    public void OnSignUpSuccess(string userId)
    {
        statusText.text = "Signup successful!";
        signUpButton.interactable = true; // Re-enable button after success
    }

    public void OnSignUpFailed(string error)
    {
        statusText.text = "Signup failed: " + error;
        signUpButton.interactable = true; // Re-enable button on failure
    }

    public void OnRoleFetched(string role)
    {
        statusText.text = "Role: " + role;
        SceneManager.LoadScene(role == "teacher" ? "TeachersDashboard_WebGL" : "StudentsDashboard_WebGL");
    }
}
