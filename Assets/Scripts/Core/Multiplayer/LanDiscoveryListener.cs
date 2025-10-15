using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class LanDiscoveryListener : MonoBehaviour
{
    public class Entry { public string Name; public string Ip; public ushort Port; public float Last; }
    public delegate void Changed(List<Entry> list);
    public static event Changed OnChanged;

    [SerializeField] private string multicast = "239.255.255.233";
    [SerializeField] private int mcastPort = 47777;

    readonly Dictionary<string, Entry> map = new();
    UdpClient udp; CancellationTokenSource cts;

    void OnEnable(){ cts = new CancellationTokenSource(); _=Loop(cts.Token); }
    void OnDisable(){ try{cts?.Cancel();}catch{} try{udp?.Close();}catch{} map.Clear(); }

    async Task Loop(CancellationToken t)
    {
        udp = new UdpClient(mcastPort);
        udp.JoinMulticastGroup(IPAddress.Parse(multicast));
        while (!t.IsCancellationRequested)
        {
            UdpReceiveResult r; try { r = await udp.ReceiveAsync(); } catch { break; }
            var s = Encoding.UTF8.GetString(r.Buffer);
            var p = s.Split('|'); if (p.Length < 3) continue;
            var name = p[0]; var ip = p[1]; if (!ushort.TryParse(p[2], out var port)) continue;
            map[$"{ip}:{port}"] = new Entry{ Name=name, Ip=ip, Port=port, Last=Time.time };
            Prune(); OnChanged?.Invoke(new List<Entry>(map.Values));
        }
    }
    void Update(){ Prune(); }
    void Prune()
    {
        var now = Time.time; var del = new List<string>();
        foreach (var kv in map) if (now - kv.Value.Last > 5f) del.Add(kv.Key);
        foreach (var k in del) map.Remove(k);
    }
}
