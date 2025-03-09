using UnityEngine;
using UnityEngine.SceneManagement;

public class UINavigation : MonoBehaviour
{

    public void GoToLoginAndroid()
    {
        SceneManager.LoadScene("Login_Android");
    }

    public void GoToLoginWebGL()
    {
        SceneManager.LoadScene("Login_WebGL");
    }

}
