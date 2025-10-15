using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetwork : MonoBehaviour
{
    public static GameNetwork Instance { get; private set; }
    public static string CurrentGameId { get; private set; }
    private static readonly Dictionary<string, (string Address, ushort Port)> GameIdDirectory = new();

    public NetworkManager Net => NetworkManager.Singleton ?? GetComponent<NetworkManager>();

    [Header("Transport")]
    [SerializeField] private UnityTransport transport;

    [Header("Gameplay")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";
    [SerializeField] private uint maxPlayers = 2; // two-person coop
    [SerializeField] private bool useConnectionApproval = true;

    public static string LocalPlayerName { get; private set; } = "Player";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!transport) transport = GetComponent<UnityTransport>();
        var nm = NetworkManager.Singleton ?? GetComponent<NetworkManager>();
        if (nm == null)
        {
            Debug.LogError("[Net] NetworkManager missing. Place GameNetwork + NetworkManager + UnityTransport on same GameObject!");
            return;
        }

        if (useConnectionApproval)
        {
            nm.NetworkConfig.ConnectionApproval = true;
            nm.ConnectionApprovalCallback = OnConnectionApproval;
        }

        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public void SetLocalPlayerName(string name) =>
        LocalPlayerName = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim();

    public void StartHost(string address, ushort port)
    {
        transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");
        Net.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(LocalPlayerName);
        Net.StartHost();

        CurrentGameId = GameCodeGenerator.Generate();
        GameIdDirectory[CurrentGameId] = (address, port);
        Debug.Log($"[Net] Host started ({CurrentGameId}) on {address}:{port}");

        // prevent duplicates
        var existing = UnityEngine.Object.FindFirstObjectByType<LanDiscoveryAdvertiser>();
        if (existing != null) Destroy(existing.gameObject);

        var advGo = new GameObject("LAN_Advertiser");
        DontDestroyOnLoad(advGo);
        var adv = advGo.AddComponent<LanDiscoveryAdvertiser>();
        adv.Initialize(port, $"{LocalPlayerName}'s Lobby");
    }

    public void StartClient(string address, ushort port)
    {
        transport.SetConnectionData(address, port, "0.0.0.0");
        Net.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(LocalPlayerName);
        Net.StartClient();
        Debug.Log($"[Net] Client connecting to {address}:{port}");
    }

    private void OnClientConnected(ulong id)
    {
        Debug.Log($"[Net] Client connected: {id}");

        // if host and 2 players present -> start game
        if (Net.IsHost && Net.ConnectedClientsIds.Count == maxPlayers)
        {
            Debug.Log("[Net] Both players connected. Starting match...");
            StartMatch();
        }
    }

    private void OnClientDisconnected(ulong id)
    {
        Debug.Log($"[Net] Client disconnected: {id}");
    }

    private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest req,
                                      NetworkManager.ConnectionApprovalResponse res)
    {
        string name = "Player";
        if (req.Payload != null && req.Payload.Length > 0)
        {
            try { name = Encoding.UTF8.GetString(req.Payload); } catch { }
        }

        bool hasRoom = Net.ConnectedClientsIds.Count < maxPlayers;
        bool valid = !string.IsNullOrWhiteSpace(name);

        res.Approved = hasRoom && valid;
        res.CreatePlayerObject = true;
        res.Reason = res.Approved ? null : (!hasRoom ? "Server full" : "Invalid name");

        if (res.Approved)
            NameCache.Set(req.ClientNetworkId, name);
    }

    public void StartMatch()
    {
        if (!Net.IsHost) return;
        Net.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public bool JoinByGameId(string gameId)
    {
        if (GameIdDirectory.TryGetValue(gameId.Trim().ToUpper(), out var info))
        {
            StartClient(info.Address, info.Port);
            Debug.Log($"[Net] Joining {gameId} ({info.Address}:{info.Port})");
            return true;
        }

        Debug.LogWarning($"[Net] Game ID {gameId} not found!");
        return false;
    }

    private void OnDisable()
    {
        var adv = UnityEngine.Object.FindFirstObjectByType<LanDiscoveryAdvertiser>();
        if (adv != null) Destroy(adv.gameObject);
    }

    private static class NameCache
    {
        private static readonly Dictionary<ulong, string> cache = new();
        public static void Set(ulong id, string name) => cache[id] = name;
        public static string Get(ulong id) => cache.TryGetValue(id, out var n) ? n : null;
        public static void Clear() => cache.Clear();
        public static IReadOnlyDictionary<ulong, string> All => cache;
    }

    public static class LobbyRoster
    {
        public static string GetName(ulong clientId) => NameCache.Get(clientId) ?? $"Player_{clientId}";
        public static IReadOnlyDictionary<ulong, string> AllNames => NameCache.All;
    }
}
