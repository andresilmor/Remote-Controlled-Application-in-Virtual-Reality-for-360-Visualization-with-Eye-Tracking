using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EyeTrackingManager 
{

    public static Action<HotspotHandler> OnHasFocus;
    public static Action<HotspotHandler> OnLostFocus;

    public static float StartCountAt = 2.5f;

    public static List<HotspotHandler> Hotspots = new List<HotspotHandler>();

    public static void EnableTobiiXR() {
        GameObject.Instantiate(Controller.Instance.TobiiXR_Initializer);
        HotspotHandler.ToRecordEyeFocus = true;

    }

    public static void AddObjectFocus(HotspotHandler hotspot) {
        Hotspots.Add(hotspot);

    }

    public static IEnumerator CountFocusTime(HotspotHandler hotspot) {
        while (hotspot.HasFocus) {
            if (hotspot.Countdown <= StartCountAt) {
                hotspot.Countdown += Time.deltaTime;

            } else {
                if (hotspot.FocusSeconds == 0)
                    hotspot.FocusCount += 1;

                hotspot.FocusSeconds += Time.deltaTime;

            }
            yield return null;

        }

    }

}
