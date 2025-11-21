using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ParallaxBackground : MonoBehaviour
{
    List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();

    void Start()
    {
        SetLayers();
        
        // Subscribe once
        ParallaxCamera.OnCameraTranslate += Move;
    }

    void OnDestroy()
    {
        // Avoid memory leak
        ParallaxCamera.OnCameraTranslate -= Move;
    }

    void SetLayers()
    {
        parallaxLayers.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            ParallaxLayer layer = transform.GetChild(i).GetComponent<ParallaxLayer>();
            if (layer != null)
                parallaxLayers.Add(layer);
        }
    }

    void Move(float delta)
    {
        foreach (var layer in parallaxLayers)
            layer.Move(delta);
    }
}
