using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class LanDiscoveryAdvertiser : MonoBehaviour
{
    [Header("Multicast (leave default unless testing)")]
    [SerializeField] private string multicastAddress = "239.255.255.233";
    [SerializeField] private int multicastPort = 47777;

    [Header("Lobby")]
    [SerializeField] private ushort gamePort = 7777;
    [SerializeField] private string lobbyName = "My Lobby";

    private UdpClient udp;
    private IPEndPoint endpoint;
    private CancellationTokenSource cts;

    public void Initialize(ushort port, string name = null)
    {
        gamePort = port;
        if (!string.IsNullOrWhiteSpace(name)) lobbyName = name;
    }

    private void OnEnable()
    {
        if (string.IsNullOrWhiteSpace(multicastAddress))
            multicastAddress = "239.255.255.233";
        if (multicastPort <= 0)
            multicastPort = 47777;
        if (gamePort == 0)
            gamePort = 7777;
        if (string.IsNullOrWhiteSpace(lobbyName))
            lobbyName = "My Lobby";

        endpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), multicastPort);
        udp = new UdpClient();
        udp.MulticastLoopback = false;

        cts = new CancellationTokenSource();
        _ = BroadcastLoop(cts.Token);
    }

    private void OnDisable()
    {
        try { cts?.Cancel(); } catch { }
        try { udp?.Close(); } catch { }
        cts = null; udp = null;
    }

    private async Task BroadcastLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var payload = $"{lobbyName}|{GetLocalIPv4()}|{gamePort}";
            var data = Encoding.UTF8.GetBytes(payload);
            try { await udp.SendAsync(data, data.Length, endpoint); } catch { }
            await Task.Delay(1000, token);
        }
    }

    private static string GetLocalIPv4()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
        }
        catch { }
        return "127.0.0.1";
    }
}
