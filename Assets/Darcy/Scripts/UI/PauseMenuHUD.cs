using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Author: Darcy Matheson
// Purpose: Controls the pause menu HUD, including saving and loading the mouse sensitivity from memory (PlayerPrefs)

public class PauseMenuHUD : MonoBehaviour
{
    private enum NavigationAction
    {
        None,
        RestartRun,
        ExitToMenu,
        QuitGame
    }

    private NavigationAction desiredAction;

    public Canvas pauseMenuCanvas;
    public GameObject blurEffect;

    public GameObject navigationScreen;
    public GameObject confirmationScreen;

    public Slider sensitivitySlider;

    public CameraController cameraController;

    void Start()
    {
        // Display the saved sensitivity value on the slider, defaulting to 3.0
        sensitivitySlider.value = Mathf.RoundToInt(PlayerPrefs.GetFloat("MouseSensitivity", 3f) * 10f);
        
        desiredAction = NavigationAction.None;
    }

    // Update is called once per frame
    void Update()
    {
        // Pauses the game when escape is pressed
        if (UpgradeStationHUD.showUpgradeHUD == false)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && PlayerStats.gamePaused == false)
            {
                // Change paused state
                PlayerStats.gamePaused = true;
            }

            // Adjust game behaviour accordingly
            Time.timeScale = (PlayerStats.gamePaused) ? 0f : 1f;
            pauseMenuCanvas.enabled = PlayerStats.gamePaused;
            UserInterfaceHUD.showHUD = !PlayerStats.gamePaused;
            Cursor.lockState = (PlayerStats.gamePaused) ? CursorLockMode.None : CursorLockMode.Locked;
            blurEffect.SetActive(PlayerStats.gamePaused);
        }
    }

    public void Resume()
    {
        PlayerStats.gamePaused = false;

        // Adjust game behaviour accordingly
        Time.timeScale = (PlayerStats.gamePaused) ? 0f : 1f;
        pauseMenuCanvas.enabled = PlayerStats.gamePaused;
        UserInterfaceHUD.showHUD = !PlayerStats.gamePaused;
        Cursor.lockState = (PlayerStats.gamePaused) ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void AttemptRestartStage()
    {
        desiredAction = NavigationAction.RestartRun;

        navigationScreen.SetActive(false);
        confirmationScreen.SetActive(true);
    }

    public void AttemptReturnToMenu()
    {
        desiredAction = NavigationAction.ExitToMenu;

        navigationScreen.SetActive(false);
        confirmationScreen.SetActive(true);
    }

    public void AttemptQuit()
    {
        desiredAction = NavigationAction.QuitGame;

        navigationScreen.SetActive(false);
        confirmationScreen.SetActive(true);
    }

    // Called on Slider.ValueChanged()
    public void UpdateSensitivity()
    {
        // Update the mouse sensitivity
        cameraController.mouseSensitivity = sensitivitySlider.value / 10f;
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivitySlider.value / 10f);
    }

    // When the player presses the confirm button
    public void ConfirmAction()
    {
        switch (desiredAction)
        {
            case NavigationAction.RestartRun:
                RestartStage();
                break;
            case NavigationAction.ExitToMenu:
                ReturnToMenu();
                break;
            case NavigationAction.QuitGame:
                Quit();
                break;
        }
    }

    public void DenyAction()
    {
        desiredAction = NavigationAction.None;
        navigationScreen.SetActive(true);
        confirmationScreen.SetActive(false);
    }

    private void RestartStage()
    {
        Debug.Log("Restarting...");
    }

    private void ReturnToMenu()
    {
        Debug.Log("Returning to menu...");
    }

    private void Quit()
    {
        Debug.Log("Quitting...");
        Application.Quit();
    }
}
