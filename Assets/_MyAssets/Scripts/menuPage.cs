using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class menuPage : MonoBehaviour
{
    public void Quit()
    {
        Application.Quit();
    }

    public void Practice()
    {
        SceneManager.LoadScene(1);
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Adventure()
    {
        SceneManager.LoadScene(2);
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
