using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Sec;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

using Debug = XRDebug;

public static class XRDebug
{

    private static GameObject _cubeForTest;
    private static GameObject _sphereForTest;

    private static List<AppLog> _logs = new List<AppLog>();

    public static TextMeshPro Console = null;

    public struct AppLog {
        public LogType type;
        public string info;

        public AppLog(LogType type, string info) {
            this.type = type;
            this.info = info;
        }
    }

    public static void Log(object message, LogType logType = LogType.Info)
    {
        string text = message.ToString();
        string preText = "";

        _logs.Add(new AppLog(logType, System.DateTime.Now + " | " + Enum.GetName(typeof(LogType), logType) + " | " + text + "\n"));
        UnityEngine.Debug.Log(Enum.GetName(typeof(LogType), logType) + " | " + text + "\n");


    }

    public static List<AppLog> GetLog(bool filterInfo, bool filterWarning, bool filterException, bool filterError, bool filterFatal) {
        List<AppLog> filteredLogs = new List<AppLog>();

        foreach (AppLog log in _logs) {
            if (
                (log.type is LogType.Info && filterInfo) ||
                (log.type is LogType.Warning && filterWarning) ||
                (log.type is LogType.Exception && filterException) ||
                (log.type is LogType.Error && filterError) ||
                (log.type is LogType.Fatal && filterFatal)
                )
                filteredLogs.Add(log);

        }

        return filteredLogs;

    }

 
    public static GameObject GetCubeForTest()
    {
        return _cubeForTest;
    }

    public static GameObject GetSphereForTest()
    {
        return _sphereForTest;
    }

    public static void SetCubeForTest(GameObject cube)
    {
        _cubeForTest = cube;
    }

    public static void SetSphereForTest(GameObject sphere)
    {
        _sphereForTest = sphere;
    }




}
