// Scripts/Network/GameNet.cs
using Unity.Netcode;
using UnityEngine;

public class GameNetwork : MonoBehaviour
{
    public static GameNetwork Instance { get; private set; }
    public NetworkManager Net => NetworkManager.Singleton;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
