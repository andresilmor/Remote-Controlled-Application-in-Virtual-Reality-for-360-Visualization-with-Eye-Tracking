using BestHTTP.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = XRDebug;

public static class SessionManager {

    private static SessionState _sessionStatus = SessionState.Disconnected;

    public static bool Connect() {
        AppCommandCenter.Instance.DisplayScreenCentralMessage("Connecting...");

        WebSocket session = APIManager.CreateWebSocketConnection(APIManager.VRHealSession, null, null, (WebSocket ws) => {
            ws.OnMessage = null;
            ws.OnMessage += DisplaySessionChannel;

            ws.Send(JObject.Parse("{ \"state\" : \"initialize\" }").ToString());

        });

        session.Open();
        return true;

    }

    private static void DisplaySessionChannel(WebSocket webSocket, string message) {
        JObject response = JObject.Parse(message);
        _sessionStatus = SessionState.Initialized;
        AppCommandCenter.Instance.DisplayScreenCentralMessage("Session ID:\n\n" + response["channel"]);

    }
}
