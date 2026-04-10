// Gestiona la connexió WebSocket amb el servidor de joc
using System;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

public class CombatNetworkManager : MonoBehaviour
{
    private const string ServerUrl = "ws://localhost/game/";

    private WebSocket websocket;
    public  bool      IsConnected =>
        websocket != null && websocket.State == WebSocketState.Open;

    // Events que escolta CombatUIManager
    public event Action<SocketMessage> OnMessageReceived;
    public event Action                OnConnected;
    public event Action                OnDisconnected;

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    // Connecta al servidor i envia join_game automàticament
    public async Task Connect()
    {
        websocket = new WebSocket(ServerUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket connectat");
            OnConnected?.Invoke();
        };

        websocket.OnMessage += bytes =>
        {
            string json = Encoding.UTF8.GetString(bytes);
            try
            {
                var msg = JsonUtility.FromJson<SocketMessage>(json);
                OnMessageReceived?.Invoke(msg);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error parsejant missatge: " + ex.Message);
            }
        };

        websocket.OnError += err =>
            Debug.LogError("WebSocket error: " + err);

        websocket.OnClose += _ =>
        {
            Debug.Log("WebSocket desconnectat");
            OnDisconnected?.Invoke();
        };

        try { await websocket.Connect(); }
        catch (Exception ex)
        { Debug.LogError("No s'ha pogut connectar: " + ex.Message); }
    }

    // Envia un objecte com a JSON
    public async Task Send(object payload)
    {
        if (!IsConnected) return;
        string json = JsonUtility.ToJson(payload);
        await websocket.SendText(json);
    }

    public async Task Disconnect()
    {
        if (websocket != null && IsConnected)
            await websocket.Close();
    }

    async void OnDestroy()
    {
        await Disconnect();
    }
}
