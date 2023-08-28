using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Debug = XRDebug;

public static class EyeExerciseManager
{

    public static Action<HotspotHandler> OnHasFocus;
    public static Action<HotspotHandler> OnLostFocus;

    public static void EnableTobiiXR() {
        GameObject.Instantiate(Controller.Instance.TobiiXR_Initializer);
        HotspotHandler.ToRecordEyeFocus = true;
  

    }

}
