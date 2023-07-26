using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [Header("UI Pages")]
    public GameObject Options;

    [Header("Buttons")]
    public Button ConnectButton;
    public Button OptionButton;


    [Header("Text")]
    [SerializeField] TextMeshProUGUI _statusText;

    public List<Button> ReturnButtons;

    public static StartMenu Instance;
    //AudioManager

    void Awake() {
        if (Instance == null)
            Instance = this;
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
        ConnectButton.gameObject.SetActive(false);
        _statusText.text = messsage;
        _statusText.gameObject.SetActive(true);
    }

    public void RestartScreen() {
        _statusText.gameObject.SetActive(false);
        ConnectButton.gameObject.SetActive(true);
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
        SceneTransitionManager.Instance.GoToSceneAsync(SceneTransitionManager.Scenes["Start"]);
    }

    public void HideAll()
    {
        Options.SetActive(false);
    }

    public void EnableMainMenu()
    {
        Options.SetActive(false);
    }
    public void EnableOption()
    {
        Options.SetActive(true);
    }
    public void EnableAbout()
    {
        Options.SetActive(false);
    }
}
