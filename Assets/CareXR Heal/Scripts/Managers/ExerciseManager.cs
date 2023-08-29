using BestHTTP.WebSocket;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = XRDebug;

public static class ExerciseManager
{
    private static string _streamChannelUID;
    private static string _streamReceiverUID;
    private static string _streamSenderUID;

    public static PanoramicExercise PanoramicExercise;


    public static JObject ExerciseLog = new JObject();

    public static  void StartExercise(int exerciseType) {

        switch (SessionManager.ExerciseType) {
            case ExerciseType.Panoramic:

                PanoramicExercise = (PanoramicExercise)(exerciseType);

                switch (PanoramicExercise) {
                    case PanoramicExercise.Recognition:
                        ExerciseManager.StartRecognitionExercise();

                        break;

                    default:
                        Debug.Log("PanoramicExercise NOT IMPLEMENTED");

                        break;

                }

                break;

            case ExerciseType.Model:
                // Just for effect
                break;

            default:
                Debug.Log("ExerciseType NOT IMPLEMENTED");
                break;

        }

    }

    internal static void SetupStreamingData(string streamChannelUID, string streamReceiverUID) {
        _streamChannelUID = streamChannelUID;
        _streamReceiverUID = streamReceiverUID;
    }

    private static void StartRecognitionExercise() {
        Debug.Log("------------------------------------------");
        Debug.Log(EyeTrackingManager.Hotspots.Count);
        for (int index = 0; index < EyeTrackingManager.Hotspots.Count; index += 1) {
            Debug.Log(EyeTrackingManager.Hotspots[index].Alias);

        }

        Debug.Log("------------------------------------------");

        SessionManager.StreamChannel = APIManager.CreateWebSocketConnection(APIManager.VRHealSessionStream, (WebSocket ws, string message) => {
            Debug.Log(">>>>>>>>> Ok");

            _streamSenderUID = JObject.Parse(message)["streamerUUID"].ToString();

            EyeTrackingManager.EnableTobiiXR();

        }, null, (WebSocket ws) => {
            JObject initializeStream = new JObject();

            initializeStream.Add("state", "initialize");
            initializeStream.Add("receiverUUID", _streamReceiverUID);
            initializeStream.Add("streamChannel", _streamChannelUID);

            ws.Send(JObject.Parse(initializeStream.ToString()).ToString());

        });

        SessionManager.StreamChannel.Open();

        EyeTrackingManager.OnHasFocus += (HotspotHandler hotspot) => {
            if (SessionManager.StreamChannel != null && !SessionManager.StreamChannel.IsOpen && SessionManager.StreamChannel.OnOpen.GetInvocationList().Length > 0) {
                SessionManager.StreamChannel.Open();

            }

            hotspot.RunningCoroutine = EyeTrackingManager.CountFocusTime(hotspot, () => {
                if (SessionManager.StreamChannel != null && SessionManager.StreamChannel.IsOpen) {
                    JObject message = new JObject();

                    message.Add("state", "streaming");
                    message.Add("streamerUUID", _streamSenderUID);
                    message.Add("receiverUUID", _streamReceiverUID);
                    message.Add("streamChannel", _streamChannelUID);
                    message.Add("focusTarget", hotspot.UUID);
                    message.Add("focusState", true);

                    SessionManager.StreamChannel.Send(JObject.Parse(message.ToString()).ToString());

                }

            });

            hotspot.StartCoroutine(hotspot.RunningCoroutine);

        };

        EyeTrackingManager.OnLostFocus += (HotspotHandler hotspot) => {
            if (hotspot.FocusTime > EyeTrackingManager.StartCountAt) {
                JObject message = new JObject();

                message.Add("state", "streaming");
                message.Add("streamerUUID", _streamSenderUID);
                message.Add("receiverUUID", _streamReceiverUID);
                message.Add("streamChannel", _streamChannelUID);
                message.Add("focusTarget", hotspot.UUID);
                message.Add("focusState", false);

                SessionManager.StreamChannel.Send(JObject.Parse(message.ToString()).ToString());

            }

            hotspot.StopCoroutine(hotspot.RunningCoroutine);
         
            hotspot.FocusTime += (float)System.Math.Round(hotspot.FocusTime, 2);

            RegisterEyeTrackingData();

            if (PanoramicExercise == PanoramicExercise.Recognition)
                hotspot.ResetEyeTrackingData();


        };

        ExerciseLog = new JObject(
            new JProperty("recognition", new JArray(
                
                )));


    }

    public static void RegisterEyeTrackingData() {
        switch (SessionManager.ExerciseType) {
            case ExerciseType.Panoramic:
                if (PanoramicExercise == PanoramicExercise.PointOfInterest) {
                    Debug.Log("Something wrong is not correct");
                    SortData();

                }
                RegisterData();

                break;

            default:
                Debug.Log("RegisterEyeTrackingData NOT IMPLEMENTED");
                break;

        }


    }

    private static void RegisterData() {
        switch (SessionManager.ExerciseType) {
            case ExerciseType.Panoramic:
                switch (PanoramicExercise) {
                    case PanoramicExercise.Recognition:

                        JObject newLog = ExerciseLog;

                        for (int index = 0; index < EyeTrackingManager.Hotspots.Count; index += 1) {

                            if (EyeTrackingManager.Hotspots[index].Countdown <= EyeTrackingManager.StartCountAt)
                                continue;

                            JObject data = new JObject();

                            data.Add("alias", EyeTrackingManager.Hotspots[index].Alias);
                            data.Add("uuid", EyeTrackingManager.Hotspots[index].UUID);
                            data.Add("focusCount", EyeTrackingManager.Hotspots[index].FocusCount);
                            data.Add("focusTime", EyeTrackingManager.Hotspots[index].FocusTime);

                            (newLog["recognition"] as JArray).AddFirst(data);


                        }

                        ExerciseLog = newLog;

                        Debug.Log(ExerciseLog.ToString());

                        break;

                    default:
                        break;
                }

                break;

            default:
                Debug.Log("RegisterEyeTrackingData NOT IMPLEMENTED");
                break;

        }
    }

    private static void SortData() {

        QuickSortData(0, EyeTrackingManager.Hotspots.Count - 1);
    }

    private static void QuickSortData(int low, int high) {
        if (low < high) {
            int pivot = Partition(low, high);

            QuickSortData(low, pivot - 1);
            QuickSortData(pivot + 1, high);

        }

    }

    private static int Partition(int low, int high) {
        HotspotHandler temp;
        double pivot = EyeTrackingManager.Hotspots[high].FocusTime;
        int lowIndex = low - 1;

        for (int index = low; index < high; index += 1) {
            if (EyeTrackingManager.Hotspots[index].FocusTime >= pivot) {
                lowIndex += 1;

                temp = EyeTrackingManager.Hotspots[lowIndex];
                EyeTrackingManager.Hotspots[lowIndex] = EyeTrackingManager.Hotspots[index];
                EyeTrackingManager.Hotspots[index] = temp;

            }

        }

        temp = EyeTrackingManager.Hotspots[lowIndex + 1];
        EyeTrackingManager.Hotspots[lowIndex + 1] = EyeTrackingManager.Hotspots[high];
        EyeTrackingManager.Hotspots[high] = temp;

        return lowIndex + 1;

    }
}
