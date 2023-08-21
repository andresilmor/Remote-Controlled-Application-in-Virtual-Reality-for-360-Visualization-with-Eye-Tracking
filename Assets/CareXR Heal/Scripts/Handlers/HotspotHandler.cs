using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using Tobii.G2OM;
using UnityEngine;
using Debug = XRDebug;

public class HotspotHandler : MonoBehaviour, IGazeFocusable
{
    public static bool ToRecordEyeFocus = false;

    public static Action<HotspotHandler> OnHasFocus;
    public static Action<HotspotHandler> OnLostFocus;

    public bool IsBoundingBox = false;

    [SerializeField] string _alias;
    [SerializeField] string _uuid;
    private JToken _content;

    private PanoramicManager.BoundingBoxData _boundingBoxData;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetHotspotData(string alias, string uuid, JToken content = null, PanoramicManager.BoundingBoxData boundingBoxData = null) {
        _alias = alias;
        _uuid = uuid;   
        _content = content; 
        _boundingBoxData = boundingBoxData;

    }

    public void GazeFocusChanged(bool hasFocus) {

        if (!ToRecordEyeFocus)
            return;

        if (hasFocus) {
            OnHasFocus?.Invoke(this);

        } else {
            OnLostFocus?.Invoke(this);

        }

    }


}
