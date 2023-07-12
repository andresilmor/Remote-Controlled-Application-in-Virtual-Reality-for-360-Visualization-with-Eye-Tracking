using BestHTTP.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AppCommandCenter : MonoBehaviour {


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
     
        
    }

    void Update() {
        InternetConnectionStatus(Application.internetReachability != NetworkReachability.NotReachable);

    }

    private void InternetConnectionStatus(bool connected) {
        if (!connected && _internetConnected) {
            StartMenu.instance.DisplayScreenCentralMessage("No Internet");

            _internetConnected = false;
            return;

        } else if (connected && !_internetConnected) {

            StartMenu.instance.RestartScreen();

            _internetConnected = true;

        }


    }

    



    private void OnDestroy() {
        APIManager.CloseAllWebSockets();
        StopAllCoroutines();

    }


}
