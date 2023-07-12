using BestHTTP.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = XRDebug;

public static class SessionManager {

    private static SessionState _sessionStatus = SessionState.Disconnected;
    public static bool InStartScene = true;

    public static bool Connect() {
        Debug.Log("Gonna connect");
        StartMenu.instance.DisplayScreenCentralMessage("Connecting...");


        WebSocket session = APIManager.GetWebSocket(APIManager.VRHealSession); 
          
        session = APIManager.CreateWebSocketConnection(APIManager.VRHealSession, null, null, (WebSocket ws) => {
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
        _sessionStatus = SessionState.Disconnected;
        if (forcedDisconnection) {
            Debug.Log("Forced Disconnection");


        }
        Debug.Log("Disconnected in scene " + SceneManager.GetActiveScene().name);
        Debug.Log("Is in Start Scene? " + SessionManager.InStartScene);
        if (!InStartScene) {
            Debug.Log("Yo");
            SceneManager.LoadScene("Start Scene");
            //SceneTransitionManager.singleton.GoToSceneAsync("Start Scene", null);
        
        }


        StartMenu.instance.RestartScreen();


    }

    private static void DisplaySessionChannel(WebSocket ws, string message) {
        JObject response = JObject.Parse(message);
        _sessionStatus = SessionState.Initialized;
        StartMenu.instance.DisplayScreenCentralMessage("Session ID:\n\n" + response["channel"]);

        ws.OnMessage = null;
        ws.OnMessage += (WebSocket ws, string message) => {
            JObject response = JObject.Parse(message);

            if (response["state"] != null && response["state"].ToString() == "connected") {
                _sessionStatus = SessionState.Connected;
                StartMenu.instance.DisplayScreenCentralMessage("Connected Successfully");
                ws.OnMessage = null;
                ws.OnMessage += ProcessRequest;

            }

        };

    }

    private static void ProcessRequest(WebSocket ws, string message) {
        JObject response = JObject.Parse(message);

        if (response["execute"] != null) {
            JObject executionRequest =  JObject.Parse(response["execute"].ToString());
            Debug.Log(executionRequest);
            switch (executionRequest["operation"].ToString()) {
                case "loadScene":
                    Debug.Log("here loadScene");
                    string loadScene = executionRequest["params"]["scene"].ToString();
                    Debug.Log("Going to Scene: " + loadScene);

                    Action onLoaded = () => {
                        Debug.Log("Invoked");
                        Debug.Log(executionRequest);
                        Debug.Log(executionRequest);

                        JObject returnValues = new JObject();

                        returnValues.Add("currentScene", loadScene);
                        executionRequest.Remove("params");

                        Debug.Log(returnValues);
                        executionRequest.Add("return", JToken.FromObject(returnValues));
                        Debug.Log(executionRequest);
                        response["execute"] = executionRequest;
                        Debug.Log(response);
                        Debug.Log("Sending");
                        Debug.Log(response.ToString());



                        ws.Send(JObject.Parse(response.ToString()).ToString());

                    };

                    if (loadScene.Equals("Start")) {
                        SceneManager.LoadScene("Start");
                        onLoaded?.Invoke();


                    } else
                        SceneTransitionManager.singleton.GoToSceneAsync(loadScene, onLoaded);


                    break;


            }

        }

    }

}
