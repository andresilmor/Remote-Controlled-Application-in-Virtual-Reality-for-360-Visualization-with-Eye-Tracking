using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using Tobii.G2OM;
using UnityEngine;
using Debug = XRDebug;

public class HotspotHandler : MonoBehaviour, IGazeFocusable
{

    public string Alias;
    public string UUID;
    private JToken _content;

    //------------------------------------------------------------------------------------------------------

    public static bool ToRecordEyeFocus = false;

    public float FocusSeconds;
    public float Countdown;
    public int FocusCount;

    public bool HasFocus = false;

    //------------------------------------------------------------------------------------------------------

    public bool IsBoundingBox = false;
    private PanoramicManager.BoundingBoxData _boundingBoxData;

    //------------------------------------------------------------------------------------------------------


    public IEnumerator RunningCoroutine;


    // Start is called before the first frame update
    void Start()
    {
        ResetEyeTrackingData();
        EyeTrackingManager.AddObjectFocus(this);

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetHotspotData(string alias, string uuid, JToken content = null, PanoramicManager.BoundingBoxData boundingBoxData = null) {
        Alias = alias;
        UUID = uuid;   
        _content = content; 
        _boundingBoxData = boundingBoxData;

    }

    public void GazeFocusChanged(bool hasFocus) {
        HasFocus = hasFocus;

        if (!ToRecordEyeFocus)
            return;

        if (hasFocus) {
            EyeTrackingManager.OnHasFocus?.Invoke(this);

        } else {
            EyeTrackingManager.OnLostFocus?.Invoke(this);

        }

    }

    public void ResetEyeTrackingData() {
        if (ExerciseManager.PanoramicExercise == PanoramicExercise.Recognition)
            FocusSeconds = 0;
        Countdown = 0;

    }


}
