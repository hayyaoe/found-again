using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class Shard : MonoBehaviour {
    [SerializeField] private string shardId;
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private GameObject pickupVfx;
    
    [Header("Vanish/Respawn Timing")]
    [SerializeField] private float vanishDuration = 1.0f;
    private Dissolve dissolve;

#if UNITY_EDITOR
    void OnValidate() {
        if (string.IsNullOrEmpty(shardId)) {
            shardId = System.Guid.NewGuid().ToString();
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
    void Awake()
    {
        dissolve = GetComponent<Dissolve>();
    }

    void Start() {
        var scene = SceneManager.GetActiveScene().name;
        if (SceneShardStore.IsCollected(scene, shardId))
            gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var scene = SceneManager.GetActiveScene().name;
        if (SceneShardStore.Add(scene, shardId))
        {
            StartCoroutine(HandlePickup());
        }
    }
    
    private IEnumerator HandlePickup()
    {
        // SFX
        if (pickupSfx != null && SoundFXManager.instance != null)
            SoundFXManager.instance.PlaySoundFXClip(pickupSfx, transform, 0.25f);

        // dissolve first
        if (dissolve != null)
            yield return dissolve.StartVanishRoutine(true);

        // THEN disable object
        gameObject.SetActive(false);
    }

}
