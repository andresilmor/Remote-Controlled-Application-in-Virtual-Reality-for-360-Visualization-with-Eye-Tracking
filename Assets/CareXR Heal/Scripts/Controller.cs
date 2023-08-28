using BestHTTP.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour {


    private bool _internetConnected = true;

    public GameObject TobiiXR_Initializer;


    private static Controller _instance = null;
    public static Controller Instance {
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
     
        
    }

    void Update() {
        InternetConnectionStatus(Application.internetReachability != NetworkReachability.NotReachable);

        if (Input.GetKeyDown("space") && SessionManager.SessionStatus.Equals(SessionState.Disconnected)) {
            SessionManager.Connect();

        }


    }

    private void InternetConnectionStatus(bool connected) {
        if (!connected && _internetConnected) {
            StartMenu.Instance.DisplayScreenCentralMessage("No Internet");

            _internetConnected = false;
            return;

        } else if (connected && !_internetConnected) {

            StartMenu.Instance.RestartScreen();

            _internetConnected = true;

        }


    }


    private void OnDisable() {
        if (RealmManager.Realm != null) {
            RealmManager.Realm.Dispose();

        }
    }


    private void OnDestroy() {
        APIManager.CloseAllWebSockets();
        StopAllCoroutines();

    }


}
