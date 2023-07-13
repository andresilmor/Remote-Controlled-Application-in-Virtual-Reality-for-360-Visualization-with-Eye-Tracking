using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [Header("UI Pages")]
    public GameObject options;

    [Header("Buttons")]
    public Button _connectButton;
    public Button optionButton;


    [Header("Text")]
    [SerializeField] TextMeshProUGUI _statusText;

    public List<Button> returnButtons;

    public static StartMenu instance;
    //AudioManager

    void Awake() {
        if (instance == null)
            instance = this;
        else {
            Destroy(gameObject);
            return;
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        EnableMainMenu();
        RestartScreen();
        //Hook events



    }

    private void OnEnable() {
        RestartScreen();
    }

    public void DisplayScreenCentralMessage(string messsage) {
        _connectButton.gameObject.SetActive(false);
        _statusText.text = messsage;
        _statusText.gameObject.SetActive(true);
    }

    public void RestartScreen() {
        _statusText.gameObject.SetActive(false);
        _connectButton.gameObject.SetActive(true);
    }

    public void EstablishConnection() {
        SessionManager.Connect();

    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void StartGame()
    {
        HideAll();
        SceneTransitionManager.singleton.GoToSceneAsync(SceneTransitionManager.Scenes["Start"]);
    }

    public void HideAll()
    {
        options.SetActive(false);
    }

    public void EnableMainMenu()
    {
        options.SetActive(false);
    }
    public void EnableOption()
    {
        options.SetActive(true);
    }
    public void EnableAbout()
    {
        options.SetActive(false);
    }
}
