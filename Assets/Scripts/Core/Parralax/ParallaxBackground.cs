using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DefaultExecutionOrder(50)]
public class ParallaxBackground : MonoBehaviour
{
    [Tooltip("Bisa dikosongkan. Jika kosong, akan auto-bind ke ParallaxCamera milik Main Camera.")]
    public ParallaxCamera parallaxCamera;

    private readonly List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();
    private bool _subscribed;
    private Coroutine _bindRoutine;

    void Awake()
    {
        SetLayers();
    }

    void OnEnable()
    {
        TryBindOrSchedule();
    }

    void Start()
    {
        SetLayers();
        TryBindOrSchedule();
    }

    void OnDisable()
    {
        Unsubscribe();
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void OnTransformChildrenChanged()
    {
        SetLayers();
    }

    // ===== Binding & Subscription =====

    void TryBindOrSchedule()
    {
        if (parallaxCamera != null)
        {
            ReSubscribeIfNeeded();
            return;
        }

        // 1) cari di MainCamera
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            parallaxCamera = mainCam.GetComponent<ParallaxCamera>();
        }

        // 2) fallback cari di scene
        if (parallaxCamera == null)
        {
            parallaxCamera = FindAnyObjectByType<ParallaxCamera>();
        }

        if (parallaxCamera != null)
        {
            ReSubscribeIfNeeded();
        }
        else
        {
            // 3) kalau belum ketemu, coba lagi beberapa frame
            if (_bindRoutine == null && Application.isPlaying)
                _bindRoutine = StartCoroutine(RebindUntilFound(8));
        }
    }

    IEnumerator RebindUntilFound(int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null;

            var mainCam = Camera.main;
            if (mainCam != null && parallaxCamera == null)
                parallaxCamera = mainCam.GetComponent<ParallaxCamera>();

            if (parallaxCamera == null)
                parallaxCamera = FindAnyObjectByType<ParallaxCamera>();

            if (parallaxCamera != null)
            {
                ReSubscribeIfNeeded();
                _bindRoutine = null;
                yield break;
            }
        }

        if (Application.isPlaying)
            Debug.LogWarning("[ParallaxBackground] Gagal menemukan ParallaxCamera. Pastikan Main Camera bertag 'MainCamera' atau ada komponen ParallaxCamera di scene bootstrap.");
        _bindRoutine = null;
    }

    void ReSubscribeIfNeeded()
    {
        if (parallaxCamera == null) return;

        if (_subscribed)
        {
            parallaxCamera.onCameraTranslate -= Move;
            _subscribed = false;
        }

        parallaxCamera.onCameraTranslate += Move;
        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (_subscribed && parallaxCamera != null)
            parallaxCamera.onCameraTranslate -= Move;
        _subscribed = false;
    }

    // ===== Layers & Movement =====

    void SetLayers()
    {
        parallaxLayers.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            var layer = transform.GetChild(i).GetComponent<ParallaxLayer>();
            if (layer != null)
            {
                parallaxLayers.Add(layer);
            }
        }
    }

    void Move(float delta)
    {
        for (int i = 0; i < parallaxLayers.Count; i++)
        {
            var layer = parallaxLayers[i];
            if (layer != null)
                layer.Move(delta);
        }
    }
}
