using BestHTTP.WebSocket;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AppCommandCenter : MonoBehaviour {
    [Header("Buttons")]
    [SerializeField] Button _connectButton;

    [Header("Text")]
    [SerializeField] TextMeshProUGUI _statusText;

    private bool _internetConnected = true;


    private static AppCommandCenter _instance = null;
    public static AppCommandCenter Instance {
        get { return _instance; }
        set {
            if (_instance == null) {
                _instance = value;
            } else {
                Destroy(value);
            }
        }
    }

    void Awake() {
        if (_instance == null)
            _instance = this;
        else {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

    }

    void Start() {
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            InternetConnectionStatus(false);

        }
        WebSocket authChannel = APIManager.CreateWebSocketConnection(APIManager.VRHealSession, null,null, (WebSocket ws) => { ws.Send("Hello there"); });

        
    }

    void Update() {
        InternetConnectionStatus(Application.internetReachability != NetworkReachability.NotReachable);

    }

    private void InternetConnectionStatus(bool connected) {
        if (!connected && _internetConnected) {
            _connectButton.gameObject.SetActive(false);

            _statusText.text = "No Internet";
            _statusText.gameObject.SetActive(true);

            _internetConnected = false;
            return;

        } else if (connected && !_internetConnected) {

            _statusText.gameObject.SetActive(false);
            _connectButton.gameObject.SetActive(true);

            _internetConnected = true;

        }


    }

    public void EstablishConnection() {
        WebSocket ws = APIManager.GetWebSocket(APIManager.VRHealSession);
        ws.Open();

        _connectButton.gameObject.SetActive(false);
        _statusText.text = "Connecting...";
        _statusText.gameObject.SetActive(true);

    }




    private void OnDestroy() {
        APIManager.CloseAllWebSockets();
        StopAllCoroutines();

    }

}
