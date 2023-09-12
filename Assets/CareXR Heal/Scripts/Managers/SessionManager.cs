using BestHTTP.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using Realms.Sync;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = XRDebug;

public static class SessionManager {

    private static string _applicationUID;
    private static string _managerUID;
    private static string _sessionUID;
   
  
    public static ExerciseType ExerciseType;
    public static PanoramicExercise PanoramicExercise;

    public static WebSocket StreamChannel = null;
   
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

        WebSocket session = APIManager.CreateWebSocketConnection(APIManager.VRHealSession, (WebSocket ws, string message) => {
            JObject response = JObject.Parse(message);
            _sessionStatus = SessionState.Initialized;
            Debug.Log(response["channel"]);
            StartMenu.Instance.DisplayScreenCentralMessage("Session ID:\n\n" + response["channel"]);

            ws.OnMessage = null;
            ws.OnMessage += (WebSocket ws, string message) => {
                JObject response = JObject.Parse(message);

                if (response["state"] != null && response["state"].ToString() == "connected") {
                    _sessionStatus = SessionState.Connected;

                    RealmManager.EnableDeviceSync();

                    _applicationUID = response["applicationUUID"].ToString();
                    _managerUID = response["managerUUID"].ToString();
                    _sessionUID = response["channel"].ToString();

                    StartMenu.Instance.DisplayScreenCentralMessage("Connected Successfully");
                    ws.OnMessage = null;
                    ws.OnMessage += ProcessMessage;
                    ws.OnBinary += ProcessBinary;

                }

            };

        }, null, (WebSocket ws) => {
     

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

        if (!InStartScene) {

            SceneTransitionManager.Instance.GoToSceneAsync(SceneTransitionManager.Scenes["Start"], null);
        }


        StartMenu.Instance.RestartScreen();


    }


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
                        PanoramicManager.CurrentHotspotTexture = panoramicTexture;

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

                    SceneTransitionManager.Instance.GoToSceneAsync(SceneTransitionManager.Scenes[executionRequest["params"]["scene"].ToString()], () => {
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

                    });
                 
                    break;

                case "downloadHotspot":

                    SessionManager.ExerciseType = ExerciseType.Panoramic;

                    SceneTransitionManager.Instance.GoToSceneAsync(SceneTransitionManager.Scenes["Panoramic Session"], () => {

                        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        sphere.transform.position = new Vector3(0, 1.43f, 0);
                        sphere.transform.localScale = new Vector3(9, 9, 9);
                        sphere.transform.rotation = Quaternion.identity;
                        sphere.transform.Rotate(Vector3.up, -180);
                        sphere.gameObject.name = "360º Image";
                        PanoramicManager.ApplySphereTexture(ref sphere, PanoramicManager.CurrentHotspotTexture);
                        PanoramicManager.MountHotspots(executionRequest["params"], () => {
                            JObject streamJsonMessage = JObject.Parse(message);

                            JObject executionRequest = JObject.Parse(jsonMessage["execute"].ToString());

                            ExerciseManager.ExerciseEnvUUID = executionRequest["params"]["uuid"].ToString();

                            executionRequest.Remove("params");

                            JObject returnValues = new JObject();
                            returnValues.Add("loaded", true);
                            returnValues.Add("exerciseEnvUUID", ExerciseManager.ExerciseEnvUUID);

                            executionRequest.Add("return", JToken.FromObject(returnValues));
                            jsonMessage["execute"] = executionRequest;

                            jsonMessage["state"] = "running";

                            _sessionStatus = SessionState.Running;

                            //jsonMessage.Add("streamChannel", streamJsonMessage["channel"].ToString());

                            APIManager.GetWebSocket(APIManager.VRHealSession).Send(JObject.Parse(jsonMessage.ToString()).ToString());

                            /*
                            
                            */
                        });

                    });

                    


                    break;

                case "startExercise":
                    Debug.Log("startExercise");

                    JObject streamJsonMessage = JObject.Parse(message);

                    ExerciseManager.SetupStreamingData(streamJsonMessage["execute"]["params"]["streamChannel"].ToString(), streamJsonMessage["execute"]["params"]["receiverUUID"].ToString());
                    ExerciseManager.StartExercise(streamJsonMessage["execute"]["params"]["type"].ToObject<int>() - 1);


                    break;


                case "pauseExercise":
                    ExerciseManager.PauseExercise(() => {

                        JObject returnValues = new JObject();
                        returnValues.Add("isPaused", true);

                        executionRequest.Remove("params");

                        executionRequest.Add("return", JToken.FromObject(returnValues));
                        jsonMessage["execute"] = executionRequest;
                        Debug.Log(jsonMessage.ToString());

                        ws.Send(JObject.Parse(jsonMessage.ToString()).ToString());

                    });


                    break;

                case "continueExercise":
                    ExerciseManager.ContinueExercise(() => {

                        JObject returnValues = new JObject();
                        returnValues.Add("continued", true);

                        executionRequest.Remove("params");

                        executionRequest.Add("return", JToken.FromObject(returnValues));
                        jsonMessage["execute"] = executionRequest;
                        Debug.Log(jsonMessage.ToString());

                        ws.Send(JObject.Parse(jsonMessage.ToString()).ToString());

                    });

                    break;

                case "restartExercise":
                    ExerciseManager.RestartExercise(() => {

                        JObject returnValues = new JObject();
                        returnValues.Add("hasRestarted", true);

                        executionRequest.Remove("params");

                        executionRequest.Add("return", JToken.FromObject(returnValues));
                        jsonMessage["execute"] = executionRequest;
                        Debug.Log(jsonMessage.ToString());

                        ws.Send(JObject.Parse(jsonMessage.ToString()).ToString());

                    });

                    break;

                case "stopExercise":
                    ExerciseManager.StopExercise(() => {

                        JObject returnValues = new JObject();
                        returnValues.Add("hasStopped", true);
                        returnValues.Add("exerciseLog", JToken.FromObject(ExerciseManager.ExerciseLog));


                        executionRequest.Remove("params");

                        executionRequest.Add("return", JToken.FromObject(returnValues));
                        jsonMessage["execute"] = executionRequest;
                        Debug.Log(jsonMessage.ToString());

                        ws.Send(JObject.Parse(jsonMessage.ToString()).ToString());

                    });

                    break;

                case "endSession":
                    Debug.Log("Ending Session");
                    
                    SceneTransitionManager.Instance.GoToSceneAsync(SceneTransitionManager.Scenes["Start"], () => {
                     

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
