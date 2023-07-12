using BestHTTP.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = XRDebug;

public static class SessionManager {

    private static SessionState _sessionStatus = SessionState.Disconnected;

    public static bool Connect() {
        Debug.Log("Gonna connect");
        AppCommandCenter.Instance.DisplayScreenCentralMessage("Connecting...");


        APIManager.GetWebSocket(APIManager.VRHealSession)?.Close();
        WebSocket session = APIManager.CreateWebSocketConnection(APIManager.VRHealSession, null, null, (WebSocket ws) => {
            ws.OnMessage = null;
            ws.OnMessage += DisplaySessionChannel;

            ws.Send(JObject.Parse("{ \"state\" : \"initialize\" }").ToString());

        });

        session.StartPingThread = true;

        session.PingFrequency = (1000 * 30);


        session.OnClosed += (WebSocket webSocket, UInt16 code, string message) => {
    
            DisconnectSession(true);

        };

        session.OnError += (WebSocket webSocket, string reason) => {
            Debug.Log("3");

            DisconnectSession(true);

        };



        session.Open();
        return true;

    }

    private static void DisconnectSession(bool forcedDisconnection = false) {

        if (forcedDisconnection) {
            Debug.Log("Forced Disconnection");
            APIManager.GetWebSocket(APIManager.VRHealSession).Close();


        }

        if (SceneManager.GetActiveScene() != SceneManager.GetSceneAt(0)) 
            SceneTransitionManager.singleton.GoToScene(0);

        APIManager.RemoveWebSocket(APIManager.VRHealSession);
        AppCommandCenter.Instance.RestartScreen();

    }

    private static void DisplaySessionChannel(WebSocket ws, string message) {
        JObject response = JObject.Parse(message);
        _sessionStatus = SessionState.Initialized;
        AppCommandCenter.Instance.DisplayScreenCentralMessage("Session ID:\n\n" + response["channel"]);

        ws.OnMessage = null;
        ws.OnMessage += (WebSocket ws, string message) => {
            JObject response = JObject.Parse(message);

            if (response["state"] != null && response["state"].ToString() == "connected") {
                _sessionStatus = SessionState.Connected;
                AppCommandCenter.Instance.DisplayScreenCentralMessage("Connected Successfully");


            }

        };

    }
}
