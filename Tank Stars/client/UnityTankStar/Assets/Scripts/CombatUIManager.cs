// Gestiona tota la lògica de l'escena de combat
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class CombatUIManager : MonoBehaviour
{
    [Header("GameObjects del joc")]
    public TerrainGenerator  terrain;
    public TankController    localTank;
    public TankController    enemyTank;
    public GameObject        projectilePrefab;
    public GameObject        explosionPrefab;
    public Camera            mainCamera;

    [Header("Xarxa")]
    public CombatNetworkManager network;

    // Elements HUD
    private Label   turnLabel;
    private Label   combatLog;
    private Label   localHpLabel;
    private Label   enemyHpLabel;
    private Slider  angleSlider;
    private Slider  powerSlider;
    private Button  fireButton;
    private Button  leaveButton;
    private VisualElement localHpFill;
    private VisualElement enemyHpFill;

    // Estat
    private bool isMyTurn      = false;
    private bool gameFinished  = false;
    private bool shotInFlight  = false;
    private int  terrainSeed   = 42;

    async void Start()
    {
        // Genera terreny inicial
        var gm = GameManager.EnsureInstance();
        terrain.GenerateTerrain(terrainSeed, gm.mapType);

        // Col·loca els tancs sobre el terreny
        localTank.PlaceOnTerrain();
        enemyTank.PlaceOnTerrain();

        // Connecta la UI
        SetupUI();

        // Connecta el WebSocket
        network.OnMessageReceived += HandleMessage;
        network.OnConnected       += OnNetworkConnected;
        await network.Connect();
    }

    void SetupUI()
    {
        var doc  = GetComponent<UIDocument>();
        if (doc == null) { Debug.LogError("Falta UIDocument!"); return; }

        var root = doc.rootVisualElement;

        turnLabel    = root.Q<Label>("turn-label");
        combatLog    = root.Q<Label>("combat-log");
        localHpLabel = root.Q<Label>("local-hp-label");
        enemyHpLabel = root.Q<Label>("enemy-hp-label");
        localHpFill  = root.Q<VisualElement>("local-hp-fill");
        enemyHpFill  = root.Q<VisualElement>("enemy-hp-fill");
        angleSlider  = root.Q<Slider>("angle-slider");
        powerSlider  = root.Q<Slider>("power-slider");
        fireButton   = root.Q<Button>("fire-btn");
        leaveButton  = root.Q<Button>("leave-btn");

        fireButton?.RegisterCallback<ClickEvent>(_ => OnFireClicked());
        leaveButton?.RegisterCallback<ClickEvent>(_ => OnLeaveClicked());

        SetFireButtonEnabled(false);
    }

    // S'executa quan el WebSocket es connecta
    async void OnNetworkConnected()
    {
        var gm = GameManager.EnsureInstance();

        // Si tenim gameId real, enviem join_game
        // Si estem provant sense login, saltem directament
        if (gm.gameId > 0 && gm.playerId > 0)
        {
            await network.Send(new JoinGameMessage
            {
                type     = "join_game",
                gameId   = gm.gameId,
                playerId = gm.playerId
            });
        }
        else
        {
            // MODE DE PROVA: simulem que és el torn del jugador local
            if (turnLabel != null) turnLabel.text = "MODE PROVA — El teu torn";
            isMyTurn = true;
            SetFireButtonEnabled(true);
        }
    }

    // Gestiona els missatges rebuts del servidor
    void HandleMessage(SocketMessage msg)
    {
        if (msg == null) return;

        switch (msg.type)
        {
            case "game_start":
                HandleGameStart(msg);
                break;
            case "game_update":
                HandleGameUpdate(msg);
                break;
            case "game_end":
                HandleGameEnd(msg);
                break;
            case "terrain_destroyed":
                HandleTerrainDestroyed(msg);
                break;
            case "joined_waiting":
                if (turnLabel != null)
                    turnLabel.text = "Esperant l'oponent...";
                break;
            case "error":
                if (combatLog != null)
                    combatLog.text = "Error: " + msg.message;
                break;
        }
    }

    void HandleGameStart(SocketMessage msg)
    {
        var gm    = GameManager.EnsureInstance();
        isMyTurn  = msg.currentTurnPlayerId == gm.playerId;

        // Genera el terreny amb la llavor del servidor si existeix
        terrain.GenerateTerrain(terrainSeed, msg.mapType ?? gm.mapType);
        localTank.PlaceOnTerrain();
        enemyTank.PlaceOnTerrain();

        UpdateHpBars(msg.player1Hp, msg.player2Hp, msg);
        UpdateTurnLabel();
        SetFireButtonEnabled(isMyTurn);

        if (combatLog != null)
            combatLog.text = isMyTurn ? "Comences tu!" : "Comença l'oponent";
    }

    void HandleGameUpdate(SocketMessage msg)
    {
        var gm   = GameManager.EnsureInstance();
        isMyTurn = msg.currentTurnPlayerId == gm.playerId;

        UpdateHpBars(msg.player1Hp, msg.player2Hp, msg);
        UpdateTurnLabel();
        SetFireButtonEnabled(isMyTurn && !gameFinished && !shotInFlight);

        if (combatLog != null)
            combatLog.text = BuildCombatLog(msg);
    }

    void HandleGameEnd(SocketMessage msg)
    {
        gameFinished = true;
        SetFireButtonEnabled(false);

        var gm = GameManager.EnsureInstance();
        bool won = msg.winnerPlayerId == gm.playerId;

        if (turnLabel != null)
            turnLabel.text = won ? "VICTÒRIA!" : "DERROTA";
        if (combatLog != null)
            combatLog.text = (won ? "Has guanyat!" : "Has perdut!") +
                             " Durada: " + msg.durationSeconds + "s";
    }

    void HandleTerrainDestroyed(SocketMessage msg)
    {
        // Converteix la posició del servidor (0-100%) a coordenades del món
        float worldX = (msg.impactX / 100f) * terrain.width - terrain.width / 2f;
        float worldY = terrain.GetHeightAtX(worldX);

        terrain.DestroyTerrain(new Vector2(worldX, worldY), msg.radius * 0.2f);

        // Reprodueix l'explosió
        if (explosionPrefab != null)
        {
            var exp = Instantiate(explosionPrefab,
                new Vector3(worldX, worldY, 0f), Quaternion.identity);
            Destroy(exp, 2f);
        }

        // Sacseja la càmera
        StartCoroutine(ShakeCamera(0.3f, 0.15f));

        // Torna a col·locar els tancs
        localTank.PlaceOnTerrain();
        enemyTank.PlaceOnTerrain();
    }

    // Quan el jugador local prem FIRE
    async void OnFireClicked()
    {
        if (!isMyTurn || gameFinished || shotInFlight) return;

        int angle = Mathf.RoundToInt(angleSlider?.value ?? 45f);
        int power = Mathf.RoundToInt(powerSlider?.value ?? 75f);

        shotInFlight = true;
        SetFireButtonEnabled(false);

        // Llança el projectil visual
        SpawnProjectile(angle, power);

        // Envia el tir al servidor
        var gm = GameManager.EnsureInstance();
        await network.Send(new FireShotMessage
        {
            type     = "fire_shot",
            gameId   = gm.gameId,
            playerId = gm.playerId,
            angle    = angle,
            power    = power
        });

        shotInFlight = false;
    }

    // Crea el projectil i el llança
    void SpawnProjectile(float angle, float power)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = localTank.barrel != null
            ? localTank.barrel.position
            : localTank.transform.position + Vector3.up * 0.5f;

        var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        var pc   = proj.GetComponent<ProjectileController>();

        bool facingRight = localTank.transform.position.x < enemyTank.transform.position.x;
        pc?.Launch(angle, power, facingRight);
    }

    // Actualitza les barres de vida
    void UpdateHpBars(int p1Hp, int p2Hp, SocketMessage msg)
    {
        var gm       = GameManager.EnsureInstance();
        bool amP1    = gm.playerId == msg.player1Id;
        int localHp  = amP1 ? p1Hp : p2Hp;
        int enemyHp  = amP1 ? p2Hp : p1Hp;

        localTank.currentHp = localHp;
        enemyTank.currentHp = enemyHp;

        if (localHpFill != null)
            localHpFill.style.width = Length.Percent(localHp);
        if (enemyHpFill != null)
            enemyHpFill.style.width = Length.Percent(enemyHp);
        if (localHpLabel != null)
            localHpLabel.text = localHp + " HP";
        if (enemyHpLabel != null)
            enemyHpLabel.text = enemyHp + " HP";
    }

    void UpdateTurnLabel()
    {
        if (turnLabel == null) return;
        turnLabel.text = isMyTurn ? "El teu torn" : "Torn de l'oponent";
    }

    void SetFireButtonEnabled(bool enabled)
    {
        if (fireButton == null) return;
        fireButton.SetEnabled(enabled);
    }

    string BuildCombatLog(SocketMessage msg)
    {
        if (msg.lastDamage > 0)
        {
            string who = msg.lastAttackerPlayerId == GameManager.Instance?.playerId
                ? "Tu" : "Oponent";
            string res = msg.lastShotResult == "direct_hit" ? "impacte directe"
                       : msg.lastShotResult == "near_hit"   ? "impacte proper"
                       : "fallada";
            return $"{who}: {res} — {msg.lastDamage} dany";
        }
        return "";
    }

    // Sacseja la càmera quan hi ha una explosió
    IEnumerator ShakeCamera(float duration, float magnitude)
    {
        if (mainCamera == null) yield break;

        Vector3 original = mainCamera.transform.position;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            float x = original.x + Random.Range(-magnitude, magnitude);
            float y = original.y + Random.Range(-magnitude, magnitude);
            mainCamera.transform.position = new Vector3(x, y, original.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = original;
    }

    void OnLeaveClicked()
    {
        GameManager.EnsureInstance().ResetMatchState();
        SceneManager.LoadScene("MenuScene");
    }

    async void OnDestroy()
    {
        await network.Disconnect();
    }
}

// ===== Missatges WebSocket =====

[System.Serializable]
public class JoinGameMessage  { public string type; public int gameId; public int playerId; }
[System.Serializable]
public class FireShotMessage  { public string type; public int gameId; public int playerId; public int angle; public int power; }

[System.Serializable]
public class SocketMessage
{
    public string type;
    public string message;
    public string mapType;
    public string roomCode;
    public int    gameId;
    public int    player1Id;
    public int    player2Id;
    public int    player1Hp;
    public int    player2Hp;
    public float  player1X;
    public float  player2X;
    public int    currentTurnPlayerId;
    public int    winnerPlayerId;
    public string status;
    public string lastShotResult;
    public int    lastDamage;
    public float  lastLandingX;
    public int    lastAttackerPlayerId;
    public int    lastAngle;
    public int    lastPower;
    public float  impactX;
    public float  impactY;
    public float  radius;
    public int    terrainEventId;
    public int    durationSeconds;
}
