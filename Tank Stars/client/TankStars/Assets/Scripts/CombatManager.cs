using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class CombatManager : MonoBehaviour
{
    private const string SocketUrl = "ws://localhost/game/";
    private const float DefaultPlayer1X = 15f;
    private const float DefaultPlayer2X = 85f;

    private static readonly Dictionary<string, int[]> TerrainPresets = new Dictionary<string, int[]>
    {
        { "desert",    new[] { 34, 36, 39, 44, 49, 53, 56, 58, 57, 54, 48, 43, 39, 37, 36, 38, 43, 50, 58, 63, 66, 65, 61, 54 } },
        { "snow",      new[] { 52, 55, 60, 65, 69, 71, 68, 63, 57, 52, 50, 49, 51, 55, 61, 68, 74, 78, 76, 71, 64, 58, 54, 51 } },
        { "grassland", new[] { 41, 43, 45, 48, 52, 55, 58, 56, 51, 46, 42, 40, 42, 46, 51, 57, 62, 64, 61, 56, 50, 46, 43, 41 } },
        { "canyon",    new[] { 46, 50, 55, 60, 62, 58, 49, 38, 28, 22, 20, 23, 31, 42, 55, 66, 72, 74, 69, 60, 52, 47, 45, 44 } },
        { "volcanic",  new[] { 39, 42, 46, 52, 60, 70, 78, 82, 74, 61, 48, 39, 35, 37, 45, 57, 68, 76, 73, 64, 53, 46, 41, 38 } },
    };

    private WebSocket websocket;
    private GameManager gameManager;

    private Label roomCodeLabel;
    private Label mapTypeLabel;
    private Label connectionStatusLabel;
    private Label turnLabel;
    private Label combatLogLabel;
    private Label localPlayerNameLabel;
    private Label enemyPlayerNameLabel;
    private Label localHpLabel;
    private Label enemyHpLabel;
    private Label angleValueLabel;
    private Label powerValueLabel;
    private VisualElement localHpFill;
    private VisualElement enemyHpFill;
    private VisualElement battlefieldStage;
    private VisualElement terrainStrip;
    private VisualElement terrainSurface;
    private Label localTankMarker;
    private Label enemyTankMarker;
    private Slider angleSlider;
    private Slider powerSlider;
    private Button fireButton;
    private Button leaveButton;

    private int player1Id;
    private int player2Id;
    private float player1X = DefaultPlayer1X;
    private float player2X = DefaultPlayer2X;
    private string activeMapType = "desert";
    private int[] terrainHeights;
    private int lastTerrainEventId = -1;
    private string lastMessageType;
    private bool gameFinished;

    void OnEnable()
    {
        var document = GetComponent<UIDocument>();
        if (document == null)
        {
            Debug.LogError("CombatManager requires a UIDocument on the same GameObject.");
            enabled = false;
            return;
        }

        var root = document.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("CombatManager could not access the UIDocument rootVisualElement.");
            enabled = false;
            return;
        }

        roomCodeLabel = root.Q<Label>("room-code-label");
        mapTypeLabel = root.Q<Label>("map-type-label");
        connectionStatusLabel = root.Q<Label>("connection-status-label");
        turnLabel = root.Q<Label>("turn-label");
        combatLogLabel = root.Q<Label>("combat-log-label");
        localPlayerNameLabel = root.Q<Label>("local-player-name");
        enemyPlayerNameLabel = root.Q<Label>("enemy-player-name");
        localHpLabel = root.Q<Label>("local-hp-label");
        enemyHpLabel = root.Q<Label>("enemy-hp-label");
        angleValueLabel = root.Q<Label>("angle-value-label");
        powerValueLabel = root.Q<Label>("power-value-label");
        localHpFill = root.Q<VisualElement>("local-hp-fill");
        enemyHpFill = root.Q<VisualElement>("enemy-hp-fill");
        battlefieldStage = root.Q<VisualElement>("battlefield-stage");
        terrainStrip = root.Q<VisualElement>("terrain-strip");
        terrainSurface = root.Q<VisualElement>("terrain-surface");
        localTankMarker = root.Q<Label>("local-tank-marker");
        enemyTankMarker = root.Q<Label>("enemy-tank-marker");
        angleSlider = root.Q<Slider>("angle-slider");
        powerSlider = root.Q<Slider>("power-slider");
        fireButton = root.Q<Button>("fire-btn");
        leaveButton = root.Q<Button>("leave-btn");

        if (roomCodeLabel == null || mapTypeLabel == null || connectionStatusLabel == null ||
            turnLabel == null || combatLogLabel == null || localPlayerNameLabel == null ||
            enemyPlayerNameLabel == null || localHpLabel == null || enemyHpLabel == null ||
            angleValueLabel == null || powerValueLabel == null || localHpFill == null ||
            enemyHpFill == null || battlefieldStage == null || terrainStrip == null || terrainSurface == null ||
            localTankMarker == null || enemyTankMarker == null || angleSlider == null ||
            powerSlider == null || fireButton == null || leaveButton == null)
        {
            Debug.LogError("CombatManager is missing one or more UI elements. Check CombatScreen.uxml is assigned to the scene UIDocument.");
            enabled = false;
            return;
        }

        gameManager = GameManager.EnsureInstance();
        roomCodeLabel.text = "Room " + (string.IsNullOrEmpty(gameManager.roomCode) ? "------" : gameManager.roomCode);
        localPlayerNameLabel.text = string.IsNullOrEmpty(gameManager.username)
            ? "You"
            : gameManager.username + " (You)";
        enemyPlayerNameLabel.text = "Opponent";
        angleSlider.value = 45f;
        powerSlider.value = 75f;
        connectionStatusLabel.text = "Connecting to game service...";
        turnLabel.text = "Waiting for both players...";
        combatLogLabel.text = "Opening the combat socket and loading the selected map preset.";
        SetHpBar(localHpFill, 100);
        SetHpBar(enemyHpFill, 100);
        localHpLabel.text = "100 HP";
        enemyHpLabel.text = "100 HP";
        UpdateSliderLabels();
        UpdateFireButton();

        activeMapType = NormalizeMapType(gameManager.mapType);
        terrainHeights = CloneTerrainPreset(activeMapType);
        RenderTerrain();

        angleSlider.RegisterValueChangedCallback(_ => UpdateSliderLabels());
        powerSlider.RegisterValueChangedCallback(_ => UpdateSliderLabels());
        fireButton.clicked += OnFireClicked;
        leaveButton.clicked += OnLeaveClicked;

        if (gameManager.gameId <= 0 || gameManager.playerId <= 0)
        {
            connectionStatusLabel.text = "Missing game context.";
            combatLogLabel.text = "Return to the menu and create or join a room again.";
            return;
        }

        _ = ConnectWebSocket();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    void OnDisable()
    {
        if (fireButton != null)
        {
            fireButton.clicked -= OnFireClicked;
        }

        if (leaveButton != null)
        {
            leaveButton.clicked -= OnLeaveClicked;
        }

        _ = CloseWebSocket();
    }

    private async Task ConnectWebSocket()
    {
        websocket = new WebSocket(SocketUrl);

        websocket.OnOpen += () =>
        {
            connectionStatusLabel.text = "Connected. Joining room...";
            _ = SendJoinGame();
        };

        websocket.OnMessage += bytes =>
        {
            HandleSocketMessage(Encoding.UTF8.GetString(bytes));
        };

        websocket.OnError += error =>
        {
            connectionStatusLabel.text = "Socket error.";
            combatLogLabel.text = error;
            UpdateFireButton();
        };

        websocket.OnClose += closeCode =>
        {
            if (!gameFinished)
            {
                connectionStatusLabel.text = "Connection closed.";
            }

            UpdateFireButton();
        };

        try
        {
            await websocket.Connect();
        }
        catch (Exception exception)
        {
            connectionStatusLabel.text = "Could not connect to game service.";
            combatLogLabel.text = exception.Message;
        }
    }

    private async Task CloseWebSocket()
    {
        if (websocket == null)
        {
            return;
        }

        if (websocket.State == WebSocketState.Open || websocket.State == WebSocketState.Connecting)
        {
            await websocket.Close();
        }

        websocket = null;
    }

    private async Task SendJoinGame()
    {
        var payload = new JoinGameMessage
        {
            type = "join_game",
            gameId = gameManager.gameId,
            playerId = gameManager.playerId,
        };

        await SendJson(payload);
    }

    private async void OnFireClicked()
    {
        if (!CanFire())
        {
            return;
        }

        combatLogLabel.text = "Shot submitted to the server...";

        var payload = new FireShotMessage
        {
            type = "fire_shot",
            gameId = gameManager.gameId,
            playerId = gameManager.playerId,
            angle = Mathf.RoundToInt(angleSlider.value),
            power = Mathf.RoundToInt(powerSlider.value),
        };

        await SendJson(payload);
    }

    private async Task SendJson(object payload)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            connectionStatusLabel.text = "Socket is not ready.";
            UpdateFireButton();
            return;
        }

        await websocket.SendText(JsonUtility.ToJson(payload));
    }

    private void HandleSocketMessage(string json)
    {
        SocketMessage message = JsonUtility.FromJson<SocketMessage>(json);
        if (message == null || string.IsNullOrEmpty(message.type))
        {
            return;
        }

        lastMessageType = message.type;

        switch (message.type)
        {
            case "connected":
                connectionStatusLabel.text = message.message;
                break;
            case "joined_waiting":
                ApplyMapState(message);
                connectionStatusLabel.text = message.message;
                combatLogLabel.text = "Your opponent has not finished loading CombatScene yet.";
                break;
            case "terrain_destroyed":
                ApplyTerrainDestroyed(message);
                break;
            case "error":
                connectionStatusLabel.text = "Server rejected the action.";
                combatLogLabel.text = message.message;
                UpdateFireButton(turnLabel.text == "Your turn to fire");
                break;
            case "game_start":
            case "game_update":
            case "game_end":
                ApplyGameState(message);
                break;
        }
    }

    private void ApplyGameState(SocketMessage message)
    {
        ApplyMapState(message);

        player1Id = message.player1Id;
        player2Id = message.player2Id;
        player1X = message.player1X <= 0 ? DefaultPlayer1X : message.player1X;
        player2X = message.player2X <= 0 ? DefaultPlayer2X : message.player2X;
        bool isPlayerOne = gameManager.playerId == player1Id;

        int localHp = isPlayerOne ? message.player1Hp : message.player2Hp;
        int enemyHp = isPlayerOne ? message.player2Hp : message.player1Hp;

        localPlayerNameLabel.text = BuildPlayerLabel(isPlayerOne ? 1 : 2, true);
        enemyPlayerNameLabel.text = BuildPlayerLabel(isPlayerOne ? 2 : 1, false);
        localHpLabel.text = localHp + " HP";
        enemyHpLabel.text = enemyHp + " HP";
        SetHpBar(localHpFill, localHp);
        SetHpBar(enemyHpFill, enemyHp);

        if (!string.IsNullOrEmpty(message.roomCode))
        {
            roomCodeLabel.text = "Room " + message.roomCode;
        }

        gameFinished = message.type == "game_end" || message.status == "finished";

        if (gameFinished)
        {
            connectionStatusLabel.text = "Match finished.";
            turnLabel.text = message.winnerPlayerId == gameManager.playerId ? "Victory" : "Defeat";
        }
        else
        {
            connectionStatusLabel.text = "Both players connected.";
            turnLabel.text = message.currentTurnPlayerId == gameManager.playerId ? "Your turn to fire" : "Opponent turn";
        }

        combatLogLabel.text = BuildCombatLog(message);
        UpdateFireButton(message.currentTurnPlayerId == gameManager.playerId);
        UpdateTankMarkers();
    }

    private void ApplyMapState(SocketMessage message)
    {
        if (!string.IsNullOrEmpty(message.mapType))
        {
            activeMapType = NormalizeMapType(message.mapType);
            gameManager.mapType = activeMapType;
        }

        if (message.terrainHeights != null && message.terrainHeights.Length > 0)
        {
            terrainHeights = (int[])message.terrainHeights.Clone();
            lastTerrainEventId = Mathf.Max(lastTerrainEventId, message.terrainEventId);
        }
        else if (terrainHeights == null || terrainHeights.Length == 0)
        {
            terrainHeights = CloneTerrainPreset(activeMapType);
        }

        RenderTerrain();
    }

    private void ApplyTerrainDestroyed(SocketMessage message)
    {
        if (message.terrainEventId <= lastTerrainEventId)
        {
            return;
        }

        if (terrainHeights == null || terrainHeights.Length == 0)
        {
            activeMapType = NormalizeMapType(message.mapType);
            terrainHeights = CloneTerrainPreset(activeMapType);
        }

        ApplyCrater(terrainHeights, message.impactX, message.impactY, message.radius);
        lastTerrainEventId = message.terrainEventId;
        RenderTerrain();
    }

    private void RenderTerrain()
    {
        if (terrainSurface == null || terrainHeights == null || terrainHeights.Length == 0)
        {
            return;
        }

        terrainSurface.Clear();

        ThemeColors theme = GetThemeColors(activeMapType);
        battlefieldStage.style.backgroundColor = new StyleColor(theme.Sky);
        terrainStrip.style.backgroundColor = new StyleColor(theme.Sky);
        mapTypeLabel.text = "Map: " + FormatMapType(activeMapType);

        for (int index = 0; index < terrainHeights.Length; index++)
        {
            var column = new VisualElement();
            column.AddToClassList("terrain-column");
            column.style.height = Length.Percent(Mathf.Clamp(terrainHeights[index], 0, 100));
            column.style.backgroundColor = new StyleColor(theme.Terrain);
            terrainSurface.Add(column);
        }

        UpdateTankMarkers();
    }

    private void UpdateTankMarkers()
    {
        if (player1Id == 0 || player2Id == 0)
        {
            PositionTankMarker(localTankMarker, DefaultPlayer1X);
            PositionTankMarker(enemyTankMarker, DefaultPlayer2X);
            return;
        }

        PositionTankMarker(localTankMarker, gameManager.playerId == player1Id ? player1X : player2X);
        PositionTankMarker(enemyTankMarker, gameManager.playerId == player1Id ? player2X : player1X);
    }

    private void PositionTankMarker(Label marker, float xPosition)
    {
        if (marker == null || terrainHeights == null || terrainHeights.Length == 0)
        {
            return;
        }

        float height = GetTerrainHeightAtX(terrainHeights, xPosition);
        marker.style.left = Length.Percent(Mathf.Clamp(xPosition - 6f, 2f, 88f));
        marker.style.bottom = Length.Percent(Mathf.Clamp(height - 2f, 8f, 86f));
    }

    private string BuildPlayerLabel(int slot, bool isLocal)
    {
        if (isLocal)
        {
            return string.IsNullOrEmpty(gameManager.username)
                ? "Tank P" + slot + " (You)"
                : gameManager.username + " (P" + slot + ")";
        }

        return "Opponent (P" + slot + ")";
    }

    private string BuildCombatLog(SocketMessage message)
    {
        if (gameFinished)
        {
            string result = message.winnerPlayerId == gameManager.playerId ? "You win" : "You lose";
            return result + " in " + message.durationSeconds + "s on " + FormatMapType(activeMapType) +
                   ". Last shot: " + DescribeShot(message);
        }

        if (message.type == "game_start")
        {
            return "Loaded " + FormatMapType(activeMapType) + ". " +
                   (message.currentTurnPlayerId == gameManager.playerId
                       ? "The duel started and you fire first."
                       : "The duel started and your opponent fires first.");
        }

        return DescribeShot(message);
    }

    private string DescribeShot(SocketMessage message)
    {
        if (message.lastAttackerPlayerId == 0)
        {
            return "Adjust the angle and power, then wait for the first valid shot.";
        }

        string shooter = message.lastAttackerPlayerId == gameManager.playerId ? "You" : "Opponent";
        string resultText = "missed";

        if (message.lastShotResult == "direct_hit")
        {
            resultText = "landed a direct hit";
        }
        else if (message.lastShotResult == "near_hit")
        {
            resultText = "landed a near hit";
        }

        return shooter + " " + resultText + " with angle " + message.lastAngle +
               " and power " + message.lastPower + ", dealing " + message.lastDamage +
               " damage and carving terrain at x=" + message.lastImpactX.ToString("0.0") + ".";
    }

    private void UpdateSliderLabels()
    {
        angleValueLabel.text = Mathf.RoundToInt(angleSlider.value) + "°";
        powerValueLabel.text = Mathf.RoundToInt(powerSlider.value) + "%";
    }

    private void SetHpBar(VisualElement fill, int hp)
    {
        fill.style.width = Length.Percent(Mathf.Clamp(hp, 0, 100));
    }

    private bool CanFire()
    {
        return !gameFinished &&
               websocket != null &&
               websocket.State == WebSocketState.Open &&
               lastMessageType != "joined_waiting" &&
               player1Id != 0 &&
               player2Id != 0 &&
               turnLabel.text == "Your turn to fire";
    }

    private void UpdateFireButton()
    {
        UpdateFireButton(false);
    }

    private void UpdateFireButton(bool isPlayerTurn)
    {
        if (fireButton == null)
        {
            return;
        }

        bool enabled = isPlayerTurn &&
                       websocket != null &&
                       websocket.State == WebSocketState.Open &&
                       !gameFinished;
        fireButton.SetEnabled(enabled);
    }

    private void OnLeaveClicked()
    {
        gameManager.ResetMatchState();
        SceneManager.LoadScene("MenuScene");
    }

    private string NormalizeMapType(string mapType)
    {
        if (!string.IsNullOrEmpty(mapType) && TerrainPresets.ContainsKey(mapType))
        {
            return mapType;
        }

        return "desert";
    }

    private int[] CloneTerrainPreset(string mapType)
    {
        int[] preset = TerrainPresets[NormalizeMapType(mapType)];
        int[] clone = new int[preset.Length];
        Array.Copy(preset, clone, preset.Length);
        return clone;
    }

    private string FormatMapType(string mapType)
    {
        if (string.IsNullOrEmpty(mapType))
        {
            return "Desert";
        }

        return char.ToUpper(mapType[0]) + mapType.Substring(1);
    }

    private float GetColumnX(int index, int totalColumns)
    {
        if (totalColumns <= 1)
        {
            return 0f;
        }

        return (index / (float)(totalColumns - 1)) * 100f;
    }

    private int GetTerrainHeightAtX(int[] heights, float impactX)
    {
        int bestIndex = 0;
        float smallestDelta = float.MaxValue;

        for (int index = 0; index < heights.Length; index++)
        {
            float delta = Mathf.Abs(GetColumnX(index, heights.Length) - impactX);
            if (delta < smallestDelta)
            {
                smallestDelta = delta;
                bestIndex = index;
            }
        }

        return heights[bestIndex];
    }

    private void ApplyCrater(int[] heights, float impactX, float impactY, float radius)
    {
        if (heights == null || heights.Length == 0 || radius <= 0f)
        {
            return;
        }

        for (int index = 0; index < heights.Length; index++)
        {
            float columnX = GetColumnX(index, heights.Length);
            float deltaX = Mathf.Abs(columnX - impactX);
            if (deltaX > radius)
            {
                continue;
            }

            float depthFactor = 1f - (deltaX / radius);
            int carvedHeight = Mathf.RoundToInt(impactY - (radius * 1.6f * depthFactor));
            heights[index] = Mathf.Max(6, Mathf.Min(heights[index], carvedHeight));
        }
    }

    private ThemeColors GetThemeColors(string mapType)
    {
        switch (NormalizeMapType(mapType))
        {
            case "snow":
                return new ThemeColors(new Color32(121, 154, 194, 255), new Color32(235, 243, 250, 255));
            case "grassland":
                return new ThemeColors(new Color32(103, 156, 129, 255), new Color32(88, 132, 74, 255));
            case "canyon":
                return new ThemeColors(new Color32(157, 121, 96, 255), new Color32(121, 74, 48, 255));
            case "volcanic":
                return new ThemeColors(new Color32(88, 64, 76, 255), new Color32(74, 37, 37, 255));
            default:
                return new ThemeColors(new Color32(190, 149, 88, 255), new Color32(169, 132, 58, 255));
        }
    }

    private readonly struct ThemeColors
    {
        public ThemeColors(Color sky, Color terrain)
        {
            Sky = sky;
            Terrain = terrain;
        }

        public Color Sky { get; }
        public Color Terrain { get; }
    }
}

[Serializable]
public class JoinGameMessage
{
    public string type;
    public int gameId;
    public int playerId;
}

[Serializable]
public class FireShotMessage
{
    public string type;
    public int gameId;
    public int playerId;
    public int angle;
    public int power;
}

[Serializable]
public class SocketMessage
{
    public string type;
    public string message;
    public int gameId;
    public string roomCode;
    public string mapType;
    public int[] terrainHeights;
    public int terrainEventId;
    public int player1Id;
    public int player2Id;
    public int player1Hp;
    public int player2Hp;
    public float player1X;
    public float player2X;
    public int currentTurnPlayerId;
    public int winnerPlayerId;
    public string status;
    public string lastShotResult;
    public int lastDamage;
    public float lastLandingX;
    public int lastAttackerPlayerId;
    public int lastAngle;
    public int lastPower;
    public float lastImpactX;
    public float lastImpactY;
    public float lastImpactRadius;
    public float impactX;
    public float impactY;
    public float radius;
    public int durationSeconds;
}
