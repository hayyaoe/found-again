using UnityEngine;

[ExecuteAlways]
public class AutoDissolveController : MonoBehaviour
{
    [Range(0f, 1f)]
    public float dissolveAmount = 0f; // Animate this via Timeline

    private Renderer _renderer;
    private Material _material;

    // Auto-detected shader property name
    private string _dissolveProperty = null;

    // List of known dissolve-ish keywords
    private readonly string[] knownProps = new string[]
    {
        "_Dissolve",
        "_DissolveAmount",
        "_DissolveValue",
        "_Cutoff",
        "_Fade",
        "_AlphaCutoff",
        "_AlphaClipThreshold",
        "_Clip",
        "_Threshold",
        "_Mask",
    };

    void Awake()
    {
        TryInit();
    }

    void OnValidate()
    {
        TryInit();
    }

    void TryInit()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        if (_renderer == null)
            return;

        // Get unique material for this object
        if (_material == null)
            _material = _renderer.material;

        // Auto-detect dissolve property if not found yet
        if (_material != null && string.IsNullOrEmpty(_dissolveProperty))
            DetectDissolveProperty();
    }

    void DetectDissolveProperty()
    {
        Shader shader = _material.shader;
        int propertyCount = shader.GetPropertyCount();

        // Scan property list
        for (int i = 0; i < propertyCount; i++)
        {
            string propName = shader.GetPropertyName(i);

            // Check if name contains any dissolve-ish pattern
            foreach (string keyword in knownProps)
            {
                if (propName.ToLower().Contains(keyword.ToLower()))
                {
                    _dissolveProperty = propName;
                    Debug.Log($"[AutoDissolveController] Found dissolve property: {propName}");
                    return;
                }
            }
        }

        Debug.LogWarning($"[AutoDissolveController] No dissolve property found on shader '{shader.name}'.");
    }

    void Update()
    {
        if (_material == null || string.IsNullOrEmpty(_dissolveProperty))
            return;

        // Apply the dissolve value
        _material.SetFloat(_dissolveProperty, dissolveAmount);
    }
}
