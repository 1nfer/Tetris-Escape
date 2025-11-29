using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialStartMenu : MonoBehaviour
{
    [Header("UI Pages")]
    public GameObject mainMenu;
    public GameObject movement;
    public GameObject proyection;
    public GameObject victory;

    [Header("Main Menu Buttons")]
    public Button movementButton;
    public Button proyectionButton;
    public Button victoryButton;

    public List<Button> returnButtons;

    // Start is called before the first frame update
    void Start()
    {
        EnableMainMenu();

        //Hook events
        movementButton.onClick.AddListener(EnableMovement);
        proyectionButton.onClick.AddListener(EnableProyection);
        victoryButton.onClick.AddListener(EnableVictory);

        foreach (var item in returnButtons)
        {
            item.onClick.AddListener(EnableMainMenu);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void HideAll()
    {
        mainMenu.SetActive(false);
        movement.SetActive(false);
        proyection.SetActive(false);
        victory.SetActive(false);
    }

    public void EnableMainMenu()
    {
        HideAll();
        mainMenu.SetActive(true);
    }

    public void EnableMovement()
    {
        HideAll();
        movement.SetActive(true);
    }

    public void EnableProyection()
    {
        HideAll();
        proyection.SetActive(true);
    }

    public void EnableVictory()
    {
        HideAll();
        victory.SetActive(true);
    }
}