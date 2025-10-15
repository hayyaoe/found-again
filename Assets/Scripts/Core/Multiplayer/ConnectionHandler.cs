using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button _buttonStartHost;
    [SerializeField] private Button _buttonStartClient;
    private void Start()
    {
        _buttonStartHost.onClick.AddListener(OnButtonStartHost);
        _buttonStartClient.onClick.AddListener(OnButtonStartClient);
    }

    private void OnDestroy()
    {
        _buttonStartHost.onClick.RemoveAllListeners();
        _buttonStartClient.onClick.RemoveAllListeners();
    }

    public void OnButtonStartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void OnButtonStartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
