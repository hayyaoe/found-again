using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AreaStreamer2D : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform fokus untuk streaming, biasanya kamera atau target kamera (Cinemachine Target).")]
    public Transform cameraFocus;

    [Header("Distances (world units)")]
    public float preloadDistance = 60f;
    public float activateDistance = 25f;

    [Tooltip("Area yang sudah TERLEWAT bakal di-unload jika jarak melebihi nilai ini dari tepi area.")]
    public float unloadAfterPassDistance = 40f;

    [Tooltip("Margin tambahan untuk area yang jauh (fallback protection).")]
    public float unloadMargin = 120f;

    [Header("Keep Neighbors")]
    [Tooltip("Pertahankan area tetangga dekat agar tetap loaded (mis. 1 = current ±1).")]
    public int keepNeighborCount = 1;

    [Header("Performance")]
    public int backgroundLoadPriority = -1;

    [Header("Debug")]
    public bool logEvents = false;
    public bool drawGizmos = true;

    [Serializable]
    public class Area
    {
        public string sceneName;
        public float startX;
        public float endX;

        public bool Contains(float x) => x >= Left && x <= Right;
        public float Left => Mathf.Min(startX, endX);
        public float Right => Mathf.Max(startX, endX);
        public float Center => (Left + Right) * 0.5f;
    }

    public List<Area> areas = new();

    private int _currentIndex = -1;
    private readonly HashSet<int> _loaded = new();
    private readonly Dictionary<int, AsyncOperation> _loadingOps = new();

    // tracking arah gerak fokus (kamera)
    private float _lastFocusX;
    private int _moveDir; // -1 = kiri, 0 = diam, 1 = kanan

    void Reset()
    {
        preloadDistance = 60f;
        activateDistance = 25f;
        unloadAfterPassDistance = 40f;
        unloadMargin = 120f;
        keepNeighborCount = 1;
        backgroundLoadPriority = -1;
        logEvents = false;
        drawGizmos = true;
    }

    void Awake()
    {
        // fallback otomatis ke Camera.main kalau cameraFocus belum di-assign
        if (cameraFocus == null && Camera.main != null)
            cameraFocus = Camera.main.transform;

        if (areas.Count == 0)
            Debug.LogWarning("[AreaStreamer2D] Daftar Areas kosong. Isi di Inspector.");

        if (cameraFocus != null) _lastFocusX = cameraFocus.position.x;
    }

    void Update()
    {
        if (cameraFocus == null || areas.Count == 0) return;

        float px = cameraFocus.position.x;

        // arah gerak (berdasar fokus kamera)
        float dx = px - _lastFocusX;
        _moveDir = Mathf.Abs(dx) < 0.0001f ? 0 : (dx > 0f ? 1 : -1);
        _lastFocusX = px;

        int newIndex = FindAreaIndex(px);
        if (newIndex != _currentIndex)
        {
            _currentIndex = newIndex;
            if (logEvents)
                Debug.Log($"[AreaStreamer2D] Current -> {_currentIndex} ({GetAreaName(_currentIndex)})");
        }

        MaintainLoads(px);
        UnloadPassedAreas(px);
    }

    int FindAreaIndex(float x)
    {
        for (int i = 0; i < areas.Count; i++)
            if (areas[i].Contains(x)) return i;

        if (x < areas[0].Left) return 0;
        if (x > areas[^1].Right) return areas.Count - 1;

        int closest = 0;
        float best = Mathf.Abs(x - areas[0].Center);
        for (int i = 1; i < areas.Count; i++)
        {
            float d = Mathf.Abs(x - areas[i].Center);
            if (d < best) { best = d; closest = i; }
        }
        return closest;
    }

    void MaintainLoads(float px)
    {
        if (_currentIndex < 0) return;

        // 1) current always loaded
        EnsureLoaded(_currentIndex, immediateActivate: true);

        // 2) preload neighbors
        TryPreloadNeighbor(px, _currentIndex - 1, goingLeft: true);
        TryPreloadNeighbor(px, _currentIndex + 1, goingLeft: false);

        // 3) fallback unload (jika terlalu jauh dari fokus)
        for (int i = 0; i < areas.Count; i++)
        {
            if (!ShouldKeepAsNeighbor(i) && _loaded.Contains(i) && IsFarFromFocus(i, _currentIndex))
                StartCoroutineSafeUnload(i);
        }
    }

    void TryPreloadNeighbor(float px, int idx, bool goingLeft)
    {
        if (idx < 0 || idx >= areas.Count) return;

        var a = areas[idx];
        float edge = goingLeft ? a.Right : a.Left;
        float dist = Mathf.Abs(px - edge);

        if (!_loaded.Contains(idx) && !_loadingOps.ContainsKey(idx) && dist <= preloadDistance)
        {
            BeginPreload(idx);
        }

        if (_loadingOps.TryGetValue(idx, out var op) && !op.allowSceneActivation && dist <= activateDistance)
        {
            if (logEvents) Debug.Log($"[AreaStreamer2D] Activate scene {areas[idx].sceneName}");
            op.allowSceneActivation = true;
        }
    }

    void EnsureLoaded(int idx, bool immediateActivate)
    {
        if (idx < 0 || idx >= areas.Count) return;

        var name = areas[idx].sceneName;
        var sc = SceneManager.GetSceneByName(name);
        if (sc.IsValid() && sc.isLoaded)
        {
            _loaded.Add(idx);
            return;
        }

        if (_loadingOps.ContainsKey(idx)) return;

        var op = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"[AreaStreamer2D] Gagal load: {name}");
            return;
        }

        if (logEvents) Debug.Log($"[AreaStreamer2D] Load (current) {name}");
        op.priority = backgroundLoadPriority;
        op.allowSceneActivation = immediateActivate;

        _loadingOps[idx] = op;
        op.completed += _ =>
        {
            _loadingOps.Remove(idx);
            var sc2 = SceneManager.GetSceneByName(name);
            if (sc2.IsValid() && sc2.isLoaded)
            {
                _loaded.Add(idx);
                if (logEvents) Debug.Log($"[AreaStreamer2D] Loaded {name}");
            }
        };
    }

    void BeginPreload(int idx)
    {
        var name = areas[idx].sceneName;

        var sc = SceneManager.GetSceneByName(name);
        if (sc.IsValid() && sc.isLoaded)
        {
            _loaded.Add(idx);
            return;
        }

        if (logEvents) Debug.Log($"[AreaStreamer2D] Preload {name} (allowSceneActivation=false)");
        var op = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
        op.priority = backgroundLoadPriority;
        op.allowSceneActivation = false;
        _loadingOps[idx] = op;

        op.completed += _ =>
        {
            _loadingOps.Remove(idx);
            var sc2 = SceneManager.GetSceneByName(name);
            if (sc2.IsValid() && sc2.isLoaded)
            {
                _loaded.Add(idx);
                if (logEvents) Debug.Log($"[AreaStreamer2D] Preloaded {name}");
            }
        };
    }

    bool IsFarFromFocus(int idx, int focusIdx)
    {
        if (Mathf.Abs(idx - focusIdx) >= keepNeighborCount + 1)
        {
            float d = Mathf.Abs(areas[idx].Center - areas[focusIdx].Center);
            return d > unloadMargin;
        }
        return false;
    }

    bool ShouldKeepAsNeighbor(int idx)
    {
        if (_currentIndex < 0) return false;
        return Mathf.Abs(idx - _currentIndex) <= keepNeighborCount;
    }

    void UnloadPassedAreas(float px)
    {
        if (_currentIndex < 0) return;

        for (int i = 0; i < areas.Count; i++)
        {
            if (!_loaded.Contains(i)) continue;
            if (ShouldKeepAsNeighbor(i)) continue; // jaga current ± neighbors

            var a = areas[i];

            // Bergerak ke kanan: area dianggap "terlewat" jika fokus sudah melewati Right + buffer
            if (_moveDir >= 0 && px > a.Right + unloadAfterPassDistance)
            {
                StartCoroutineSafeUnload(i);
                continue;
            }

            // Bergerak ke kiri: area dianggap "terlewat" jika fokus sudah melewati Left - buffer
            if (_moveDir <= 0 && px < a.Left - unloadAfterPassDistance)
            {
                StartCoroutineSafeUnload(i);
                continue;
            }
        }
    }

    void StartCoroutineSafeUnload(int idx)
    {
        if (idx < 0 || idx >= areas.Count) return;
        var name = areas[idx].sceneName;

        // bersihkan op loading tersisa
        if (_loadingOps.ContainsKey(idx)) _loadingOps.Remove(idx);

        var sc = SceneManager.GetSceneByName(name);
        if (!sc.IsValid() || !sc.isLoaded) return;

        if (logEvents) Debug.Log($"[AreaStreamer2D] Unload {name}");
        _loaded.Remove(idx);

        var op = SceneManager.UnloadSceneAsync(name);
        if (op != null && logEvents)
            op.completed += _ => Debug.Log($"[AreaStreamer2D] Unloaded {name}");
    }

    string GetAreaName(int idx) => (idx >= 0 && idx < areas.Count) ? areas[idx].sceneName : "(none)";

    void OnDrawGizmos()
    {
        if (!drawGizmos || areas == null) return;

        for (int i = 0; i < areas.Count; i++)
        {
            var a = areas[i];
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
            Vector3 p1 = new Vector3(a.Left, -10f, 0);
            Vector3 p2 = new Vector3(a.Right, 10f, 0);
            Gizmos.DrawCube((p1 + p2) * 0.5f, new Vector3(Mathf.Abs(a.Right - a.Left), 20f, 0.1f));

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(new Vector3(a.Center, 11f, 0), $"{i}: {a.sceneName}");
            if (cameraFocus != null)
            {
                // Discs untuk preload/activate di sekitar fokus kamera
                Vector3 c = new Vector3(cameraFocus.position.x, 0f, 0f);
                UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.4f);
                UnityEditor.Handles.DrawWireDisc(c, Vector3.forward, preloadDistance);
                UnityEditor.Handles.color = new Color(0.3f, 1f, 0.3f, 0.5f);
                UnityEditor.Handles.DrawWireDisc(c, Vector3.forward, activateDistance);
            }
#endif
        }
    }
}
