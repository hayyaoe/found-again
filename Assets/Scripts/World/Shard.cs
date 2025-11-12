// Shard.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class Shard : MonoBehaviour {
    [SerializeField] private string shardId;
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private GameObject pickupVfx;

#if UNITY_EDITOR
    void OnValidate() {
        if (string.IsNullOrEmpty(shardId)) {
            shardId = System.Guid.NewGuid().ToString(); // auto sekali
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    void Start() {
        var scene = SceneManager.GetActiveScene().name;
        if (SceneShardStore.IsCollected(scene, shardId))
            gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;
        var scene = SceneManager.GetActiveScene().name;
        if (SceneShardStore.Add(scene, shardId)) {
            if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position);
            if (pickupVfx) Instantiate(pickupVfx, transform.position, Quaternion.identity);
            gameObject.SetActive(false);
        }
    }
}
