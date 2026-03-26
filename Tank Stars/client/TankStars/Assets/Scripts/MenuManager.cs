using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuManager : MonoBehaviour
{
    private string apiUrl = "http://localhost/api";

    private Label welcomeText;
    private TextField roomCodeInput;
    private Label messageText;
    private Button createBtn;
    private Button joinBtn;
    private Button quitBtn;

    void OnEnable()
    {
        var document = GetComponent<UIDocument>();
        if (document == null)
        {
            Debug.LogError("MenuManager requires a UIDocument on the same GameObject.");
            enabled = false;
            return;
        }

        var root = document.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("MenuManager could not access the UIDocument rootVisualElement.");
            enabled = false;
            return;
        }

        welcomeText   = root.Q<Label>("welcome-text");
        roomCodeInput = root.Q<TextField>("room-code-input");
        messageText   = root.Q<Label>("message-text");
        createBtn     = root.Q<Button>("create-btn");
        joinBtn       = root.Q<Button>("join-btn");
        quitBtn       = root.Q<Button>("quit-btn");

        if (welcomeText == null || roomCodeInput == null || messageText == null ||
            createBtn == null || joinBtn == null || quitBtn == null)
        {
            Debug.LogError("MenuManager is missing one or more UI elements. Check MenuScreen.uxml is assigned to the scene UIDocument.");
            enabled = false;
            return;
        }

        var gameManager = GameManager.EnsureInstance();
        welcomeText.text = string.IsNullOrEmpty(gameManager.username)
            ? "Welcome!"
            : "Welcome, " + gameManager.username + "!";

        createBtn.clicked += OnCreateClicked;
        joinBtn.clicked += OnJoinClicked;
        quitBtn.clicked += OnQuitClicked;
    }

    void OnDisable()
    {
        if (createBtn != null)
        {
            createBtn.clicked -= OnCreateClicked;
        }

        if (joinBtn != null)
        {
            joinBtn.clicked -= OnJoinClicked;
        }

        if (quitBtn != null)
        {
            quitBtn.clicked -= OnQuitClicked;
        }
    }

    void OnCreateClicked()
    {
        StartCoroutine(CreateGame());
    }

    void OnJoinClicked()
    {
        StartCoroutine(JoinByCode());
    }

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator CreateGame()
    {
        messageText.RemoveFromClassList("error-text");
        messageText.text = "Creating game...";

        var gameManager = GameManager.EnsureInstance();
        string json = "{\"playerId\":" + gameManager.playerId + "}";
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest req = new UnityWebRequest(apiUrl + "/games", "POST");
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            CreateGameResponse response = JsonUtility.FromJson<CreateGameResponse>(req.downloadHandler.text);
            gameManager.gameId   = response.gameId;
            gameManager.roomCode = response.roomCode;
            SceneManager.LoadScene("WaitingScene");
        }
        else
        {
            messageText.AddToClassList("error-text");
            messageText.text = "Could not create game. Is Docker running?";
        }
    }

    IEnumerator JoinByCode()
    {
        string code = roomCodeInput.value.Trim().ToUpper();
        if (code.Length == 0)
        {
            messageText.AddToClassList("error-text");
            messageText.text = "Please enter a room code.";
            yield break;
        }

        messageText.RemoveFromClassList("error-text");
        messageText.text = "Finding room...";

        UnityWebRequest findReq = UnityWebRequest.Get(apiUrl + "/games/room/" + code);
        yield return findReq.SendWebRequest();

        if (findReq.result != UnityWebRequest.Result.Success)
        {
            messageText.AddToClassList("error-text");
            messageText.text = "Room not found.";
            yield break;
        }

        GameResponse game = JsonUtility.FromJson<GameResponse>(findReq.downloadHandler.text);

        var gameManager = GameManager.EnsureInstance();
        string json = "{\"playerId\":" + gameManager.playerId + "}";
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest joinReq = new UnityWebRequest(apiUrl + "/games/" + game.id + "/join", "POST");
        joinReq.uploadHandler   = new UploadHandlerRaw(body);
        joinReq.downloadHandler = new DownloadHandlerBuffer();
        joinReq.SetRequestHeader("Content-Type", "application/json");

        yield return joinReq.SendWebRequest();

        if (joinReq.result == UnityWebRequest.Result.Success)
        {
            gameManager.gameId   = game.id;
            gameManager.roomCode = code;
            SceneManager.LoadScene("WaitingScene");
        }
        else
        {
            messageText.AddToClassList("error-text");
            messageText.text = "Could not join. Room may be full.";
        }
    }
}

[System.Serializable]
public class CreateGameResponse
{
    public int gameId;
    public string roomCode;
}

[System.Serializable]
public class GameResponse
{
    public int id;
    public string room_code;
    public string status;
}
