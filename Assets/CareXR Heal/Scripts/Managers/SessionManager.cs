using BestHTTP.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = XRDebug;

public static class SessionManager {

    private static string _applicationUID;
    private static string _managerUID;
    private static string _sessionChannel;
    private static string _sessionUID;

    private static SessionState _sessionStatus = SessionState.Disconnected;

    public static SessionState SessionStatus {
         get {
            return _sessionStatus;  
        }

    }

    public static bool InStartScene = true;

    public static bool Connect() {
        Debug.Log("Gonna connect");
        StartMenu.Instance.DisplayScreenCentralMessage("Connecting...");


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

        RealmManager.DisableDeviceSync();

        if (forcedDisconnection) {
            Debug.Log("Forced Disconnection");


        }
        Debug.Log("Disconnected in scene " + SceneManager.GetActiveScene().name);
        Debug.Log("Is in Start Scene? " + SessionManager.InStartScene);
        if (!InStartScene) {
            Debug.Log("Yo");


            //SceneManager.LoadScene(SceneTransitionManager.Scenes["Start"]);
            //SceneTransitionManager.Instance.GoToSceneAsync("Start Scene", null);
            SceneTransitionManager.Instance.GoToSceneAsync(SceneTransitionManager.Scenes["Start"], null);
        }


        StartMenu.Instance.RestartScreen();


    }

    private static void DisplaySessionChannel(WebSocket ws, string message) {
        JObject response = JObject.Parse(message);
        _sessionStatus = SessionState.Initialized;
        StartMenu.Instance.DisplayScreenCentralMessage("Session ID:\n\n" + response["channel"]);

        ws.OnMessage = null;
        ws.OnMessage += (WebSocket ws, string message) => {
            JObject response = JObject.Parse(message);

            if (response["state"] != null && response["state"].ToString() == "connected") {
                _sessionStatus = SessionState.Connected;

                RealmManager.EnableDeviceSync();

                _applicationUID = response["applicationUUID"].ToString();
                _managerUID = response["managerUUID"].ToString();
                _sessionChannel = response["channel"].ToString();

                StartMenu.Instance.DisplayScreenCentralMessage("Connected Successfully");
                ws.OnMessage = null;
                ws.OnMessage += ProcessMessage;
                ws.OnBinary += ProcessBinary;

            }

        };

    }
    public static GameObject sphere;
    private static void ProcessBinary(WebSocket ws, byte[] data) {
        if (APIManager.ReceivingProtobuf) {
            switch (APIManager.ProtoInUse) {
                case "ProtoImage":
                    ProtoImage protoImage;
                    using (var memoryStream = new MemoryStream(data)) {
                        protoImage = Serializer.Deserialize<ProtoImage>(memoryStream);
                    }

                    if (protoImage != null && protoImage.image != null && protoImage.image.Length > 0) {
                        Texture2D panoramicTexture = new Texture2D(2, 2);
                        panoramicTexture.LoadImage(protoImage.image);

                        //sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        //sphere.transform.position = new Vector3(0, 0, 0);//1.43f
                        //sphere.transform.localScale = new Vector3(9, 9, 9);
                        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        sphere.transform.position = new Vector3(0, 1.43f, 0);
                        sphere.transform.localScale = Vector3.one * 9f;
                        sphere.transform.rotation = Quaternion.identity;
                        sphere.transform.Rotate(0, -179, 0, Space.Self);

                        PanoramicManager.ApplySphereTexture(ref sphere, panoramicTexture);
                        return;
                        SceneTransitionManager.Instance.GoToSceneAsync(SceneTransitionManager.Scenes["Panoramic Session"], () => {

                            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            sphere.transform.position = new Vector3(0, 1.43f, 0);
                            sphere.transform.localScale = new Vector3(9, 9, 9);
                            sphere.transform.rotation = Quaternion.identity;
                            PanoramicManager.ApplySphereTexture(ref sphere, panoramicTexture);
                            Debug.Log("LOADED");

                        } );

                        APIManager.ReceivingProtobuf = false;

                    }

                    break;

            }
        }
    }

    private static void ProcessMessage(WebSocket ws, string message) {
        JObject jsonMessage = JObject.Parse(message);
        Debug.Log(jsonMessage);


        if (jsonMessage["execute"] != null) {
            JObject executionRequest =  JObject.Parse(jsonMessage["execute"].ToString());
            Debug.Log(executionRequest);
            switch (executionRequest["operation"].ToString()) {
                case "loadScene":
                    Debug.Log("here loadScene");

                    Action onLoaded = () => {
                        Debug.Log("Invoked");
                        Debug.Log(executionRequest);
                        Debug.Log(executionRequest);

                        JObject returnValues = new JObject();

                        returnValues.Add("currentScene", SceneTransitionManager.Scenes[executionRequest["params"]["scene"].ToString()]);
                        executionRequest.Remove("params");

                        Debug.Log(returnValues);
                        executionRequest.Add("return", JToken.FromObject(returnValues));
                        Debug.Log(executionRequest);
                        jsonMessage["execute"] = executionRequest;
                        Debug.Log(jsonMessage);
                        Debug.Log("Sending");
                        Debug.Log(jsonMessage.ToString());



                        ws.Send(JObject.Parse(jsonMessage.ToString()).ToString());

                    };
                    SceneTransitionManager.Instance.GoToSceneAsync(SceneTransitionManager.Scenes[executionRequest["params"]["scene"].ToString()], onLoaded);
                    /*
                    if (executionRequest["params"]["scene"].ToString().Equals("Start")) {
                        
                        onLoaded?.Invoke();


                    } else
                        SceneTransitionManager.Instance.GoToSceneAsync(SceneTransitionManager.Scenes[executionRequest["params"]["scene"].ToString()], onLoaded);

                    */
                    break;

                case "downloadHotspot":


                    Debug.Log("HERE <Z- " + executionRequest["params"]["imageHeight"].ToString());

                    PanoramicManager.MountHotspots(executionRequest["params"], () => {

                    });


                    break;


            }

        } else if (jsonMessage["warning"] != null) {

            switch (jsonMessage["warning"]["message"].ToString()) {
                case "protobuf_incoming":
                    
                    APIManager.ReceivingProtobuf = true;
                    APIManager.ProtoInUse = jsonMessage["warning"]["proto"].ToString();

                    JObject warningResponse = new JObject();
                    warningResponse.Add("message", jsonMessage["warning"]["message"].ToString());
                    warningResponse.Add("response", true);
                    warningResponse.Add("to", jsonMessage["warning"]["to"].ToString());
                    warningResponse.Add("blocked", jsonMessage["warning"]["blocked"]);

                    JObject response = new JObject();
                    response.Add("warning", warningResponse);
                    ws.Send(JObject.Parse(response.ToString()).ToString());


                    break;



            }



        }

    }

}
