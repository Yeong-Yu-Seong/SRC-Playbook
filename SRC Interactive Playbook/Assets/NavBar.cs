/*
    Author: Yeong Yu Seong
    Date Created: 15 June 2026
    Last Edited: 15 June 2026
    Description: This script is used to manage the navigation between different scenes in the game.
*/
using UnityEngine;
using UnityEngine.SceneManagement;

public class NavBar : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Load the specified scene when the corresponding button is clicked.
    /// This method can be called from the UI buttons in the navigation bar to allow users to navigate between different scenes in the game. The sceneName parameter should match the name of the scene you want to load, which can be set in the Unity Editor's Build Settings.
    /// </summary>
    /// <param name="sceneName"></param>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName); // Load the specified scene when the corresponding button is clicked
    }
}
