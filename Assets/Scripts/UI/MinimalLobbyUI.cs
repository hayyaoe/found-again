using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MinimalLobbyUI : MonoBehaviour
{
    // runtime UI refs
    private InputField nameField, ipField, portField;
    private Button hostBtn, startBtn, joinManualBtn;
    private Text statusText;

    // LAN list UI
    private RectTransform lanContent;
    private readonly List<GameObject> lanRowPool = new();

    // Roster UI
    private RectTransform rosterContent;
    private readonly List<GameObject> rosterRowPool = new();

    private void Awake()
    {
        EnsureEventSystem();
        var canvas = CreateCanvas("Canvas_MinLobby");
        var root = CreatePanel(canvas.transform, "Root", new Vector2(1280, 720));

        var header = CreateRow(root, "Header", 48);
        nameField = CreateInput(header, "Name", "Player", 260);
        hostBtn = CreateButton(header, "Host LAN", 160, () =>
        {
            var name = string.IsNullOrWhiteSpace(nameField.text) ? "Player" : nameField.text.Trim();
            GameNetwork.Instance.SetLocalPlayerName(name);
            ushort port = ReadPort(portField.text);
            GameNetwork.Instance.StartHost("0.0.0.0", port);
            statusText.text = "Hosting…";
        });

        var body = CreateRow(root, "Body", -1);
        var left = CreatePanel(body, "Left", new Vector2(0, 0), true);
        var right = CreatePanel(body, "Right", new Vector2(0, 0), true);

        CreateLabel(left, "LAN games nearby", 18);
        lanContent = CreateScrollList(left, "LAN_List", 420);

        CreateLabel(right, "Lobby", 18);
        rosterContent = CreateScrollList(right, "Roster_List", 340);
        startBtn = CreateButton(right, "Start Match", 160, () =>
        {
            GameNetwork.Instance.StartMatch();
        });

        // Status
        var footer = CreateRow(root, "Footer", 32);
        statusText = CreateLabel(footer, "Idle", 14);

        // Advanced manual join (quick + dirty)
        var adv = CreateRow(root, "Advanced", 40);
        ipField = CreateInput(adv, "IP", "127.0.0.1", 180);
        portField = CreateInput(adv, "Port", "7777", 100);
        joinManualBtn = CreateButton(adv, "Join", 120, () =>
        {
            var name = string.IsNullOrWhiteSpace(nameField.text) ? "Player" : nameField.text.Trim();
            GameNetwork.Instance.SetLocalPlayerName(name);
            GameNetwork.Instance.StartClient(ipField.text.Trim(), ReadPort(portField.text)); // client → host 
            statusText.text = $"Joining {ipField.text}:{portField.text}…";
        });

        try { LanDiscoveryListener.OnChanged += OnLanChanged; } catch { /* if not added, LAN list just stays empty */ }
    }

    private void OnDestroy()
    {
        try { LanDiscoveryListener.OnChanged -= OnLanChanged; } catch { }
    }

    private void Update()
    {
        if (!NetworkManager.Singleton) return;

        // Host can start
        startBtn.interactable = NetworkManager.Singleton.IsHost;

        // Status text
        if (NetworkManager.Singleton.IsHost)
            statusText.text = "Host active (lobby)";
        else if (NetworkManager.Singleton.IsClient)
            statusText.text = "Connected (lobby)";

        // Roster refresh (simple, each frame is fine for small lobbies)
        RebuildRoster();
    }

    // ---------- LAN list ----------
    private void OnLanChanged(List<LanDiscoveryListener.Entry> list)
    {
        // Clear rows
        foreach (var go in lanRowPool) Destroy(go);
        lanRowPool.Clear();

        foreach (var e in list)
        {
            var btn = CreateListButton(lanContent, $"{e.Name}  ({e.Ip}:{e.Port})");
            btn.onClick.AddListener(() =>
            {
                var name = string.IsNullOrWhiteSpace(nameField.text) ? "Player" : nameField.text.Trim();
                GameNetwork.Instance.SetLocalPlayerName(name);
                GameNetwork.Instance.StartClient(e.Ip, e.Port); // direct to host’s server 
                statusText.text = $"Joining {e.Name} at {e.Ip}:{e.Port}";
            });
            lanRowPool.Add(btn.gameObject);
        }
    }

    // ---------- Roster ----------
    private void RebuildRoster()
    {
        foreach (var go in rosterRowPool) Destroy(go);
        rosterRowPool.Clear();

        foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
        {
            var t = CreateListText(rosterContent, $"[{c.ClientId}] {GameNetwork.LobbyRoster.GetName(c.ClientId)}"); // uses your name cache
            rosterRowPool.Add(t.gameObject);
        }
    }

    // ================== UI helpers ==================
    private Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        return canvas;
    }

    private void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 size, bool expand = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (size.x > 0 && size.y > 0) rt.sizeDelta = size;
        var img = go.GetComponent<Image>(); img.color = new Color(0, 0, 0, 0.15f);
        var v = go.GetComponent<VerticalLayoutGroup>(); v.spacing = 8; v.padding = new RectOffset(8, 8, 8, 8);
        if (expand) { v.childForceExpandWidth = true; v.childControlWidth = true; v.childControlHeight = true; }
        return rt;
    }

    private RectTransform CreatePanel(Transform parent, string name, int height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (height > 0) rt.sizeDelta = new Vector2(0, height);
        var v = go.GetComponent<VerticalLayoutGroup>(); v.spacing = 8; v.padding = new RectOffset(8, 8, 8, 8);
        return rt;
    }

    private RectTransform CreateRow(Transform parent, string name, int height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (height > 0) rt.sizeDelta = new Vector2(0, height);
        var h = go.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 8; h.padding = new RectOffset(8, 8, 8, 8);
        h.childForceExpandHeight = true; h.childControlHeight = true;
        return rt;
    }

    private InputField CreateInput(Transform parent, string placeholder, string def, int width)
    {
        var go = new GameObject($"Input_{placeholder}", typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.color = new Color(1, 1, 1, 0.1f);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(go.transform, false);
        var txt = textGO.GetComponent<Text>(); txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = TextAnchor.MiddleLeft; txt.color = Color.white;

        var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        phGO.transform.SetParent(go.transform, false);
        var ph = phGO.GetComponent<Text>(); ph.font = txt.font; ph.text = placeholder; ph.color = new Color(1, 1, 1, 0.5f);

        var field = go.GetComponent<InputField>();
        field.textComponent = txt; field.placeholder = ph; field.text = def;

        var le = go.AddComponent<LayoutElement>(); le.preferredWidth = width; le.preferredHeight = 32;
        return field;
    }

    private Button CreateButton(Transform parent, string label, int width, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.color = new Color(0.2f, 0.6f, 1f, 0.9f);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(go.transform, false);
        var txt = textGO.GetComponent<Text>(); txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = label; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white;

        var le = go.AddComponent<LayoutElement>(); le.preferredWidth = width; le.preferredHeight = 32;

        var btn = go.GetComponent<Button>(); btn.onClick.AddListener(onClick);
        return btn;
    }

    private Text CreateLabel(Transform parent, string text, int size)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var txt = go.GetComponent<Text>(); txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = text; txt.fontSize = size; txt.color = Color.white; txt.alignment = TextAnchor.MiddleLeft;
        return txt;
    }

    private RectTransform CreateScrollList(Transform parent, string name, int height)
    {
        // Container
        var scrollGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
        scrollGO.transform.SetParent(parent, false);
        var le = scrollGO.AddComponent<LayoutElement>(); if (height > 0) le.preferredHeight = height;
        scrollGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);
        scrollGO.GetComponent<Mask>().showMaskGraphic = false;

        // Viewport
        var vp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        vp.transform.SetParent(scrollGO.transform, false);
        vp.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);
        vp.GetComponent<Mask>().showMaskGraphic = false;

        // Content
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(vp.transform, false);
        var v = content.GetComponent<VerticalLayoutGroup>(); v.spacing = 4; v.childForceExpandWidth = true;
        var fitter = content.GetComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Wire scroll
        var sr = scrollGO.GetComponent<ScrollRect>();
        sr.viewport = vp.GetComponent<RectTransform>();
        sr.content = content.GetComponent<RectTransform>();
        sr.horizontal = false; sr.vertical = true;

        return content.GetComponent<RectTransform>();
    }

    private Button CreateListButton(Transform parent, string label)
    {
        var btn = CreateButton(parent, label, 0, () => { });
        btn.GetComponent<LayoutElement>().preferredWidth = -1; // stretch
        return btn;
    }

    private Text CreateListText(Transform parent, string label)
    {
        var t = CreateLabel(parent, label, 14);
        var le = t.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 24;
        return t;
    }

    private static ushort ReadPort(string s) =>
        ushort.TryParse(s, out var p) ? p : (ushort)7777;
}
