using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FusionSessionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    #region Instance
    public static FusionSessionManager Instance { get; private set; }
    #endregion

    #region Inspector Variables
    [SerializeField] private GameObject _networkRunner;
    [SerializeField] private bool _isDebugEnabled = false;
    [SerializeField] private string _sessionName = "FusionSession";
    [SerializeField] private int _sceneIndex = 1; // Default scene index to load
    [SerializeField] private int _maxPlayers = 4;
    #endregion

    #region Private Variables
    [HideInInspector] public NetworkRunner _runnerInstance;
    private string _sessionArgs = "";
    #endregion

    #region PUBLIC API
    public static Action<NetworkRunner, PlayerRef> OnPlayerJoinedEvent;
    public static Action<NetworkRunner, PlayerRef> OnPlayerLeftEvent;
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSessionName(_sessionName);
    }

    private async void Start()
    {
        bool isHeadlessServer = false;

        string[] args = Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            // -batchmode is standard for Unity headless builds
            if (arg == "-batchmode")
            {
                isHeadlessServer = true;
                break;
            }
        }

        if (isHeadlessServer)
        {
            if (_isDebugEnabled) Debug.Log("Starting Dedicated Headless Server...");
            // Bypasses the need for UI and starts the authoritative server immediately
            await StartSession(GameMode.Server);
        }
        else
        {
#if UNITY_EDITOR
            // Keeps AutoHostOrClient for convenient local testing inside the Unity Editor
            if (_isDebugEnabled) Debug.Log("Starting Editor Local Test...");
            await StartSession(GameMode.AutoHostOrClient);
#else
        // Forces built VR headsets to join as Clients without hosting
        if (_isDebugEnabled) Debug.Log("Starting VR Client...");
        await StartSession(GameMode.Client);
#endif
        }
    }
    #endregion

    private void InitializeRunner()
    {
        // Safely destroy the old runner if it exists to clear network ports
        if (_runnerInstance != null)
        {
            _runnerInstance.Shutdown();
            Destroy(_runnerInstance.gameObject);
            _runnerInstance = null;
        }

        GameObject runnerGo = Instantiate(_networkRunner);
        runnerGo.name = "NetworkRunner";

        _runnerInstance = runnerGo.GetComponent<NetworkRunner>();
        _runnerInstance.AddCallbacks(this);
        _runnerInstance.ProvideInput = true;
    }

    #region Private Methods
    private async Task StartSession(GameMode gameMode)
    {
        InitializeRunner();

        var startGameArgs = new StartGameArgs()
        {
            GameMode = gameMode,
            SessionName = _sessionArgs,
            Scene = GetSceneRefFromIndex(_sceneIndex),
            PlayerCount = _maxPlayers
        };

        var result = await _runnerInstance.StartGame(startGameArgs);

        if (result.Ok)
        {
            if (_isDebugEnabled) Debug.Log("Successfully started the session!");
        }
        else
        {
            Debug.LogError($"Failed to start session : {result.ShutdownReason}");
        }
    }

    private void InitializeSessionName(string sessionName)
    {
        _sessionArgs = $"{sessionName}_v{Application.version}";
        if (_isDebugEnabled) Debug.Log($"Session Name : {_sessionArgs}");
    }

    private SceneRef GetSceneRefFromIndex(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Invalid scene index : {sceneIndex}. Please ensure the index is within the valid range.");
            return default;
        }
        return SceneRef.FromIndex(sceneIndex);
    }

    // --- NetworkRunner Callbacks ---

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        OnPlayerJoinedEvent?.Invoke(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        OnPlayerLeftEvent?.Invoke(runner, player);
    }

    // Remaining empty callbacks required by INetworkRunnerCallbacks
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    #endregion
}