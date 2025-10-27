using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SceneGate : MonoBehaviour {
    [SerializeField] private string nextSceneName;    // scene tujuan (opsional)
    [SerializeField] private GameObject blockedUI;    // panel "belum lengkap" (opsional)
    [SerializeField] private GameObject unlockedUI;   // panel "boleh lanjut" (opsional)

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;

        string scene = SceneManager.GetActiveScene().name;
        int total = FindObjectsOfType<Shard>(true).Length;
        int have = SceneShardStore.Count(scene);
        bool complete = total > 0 && have >= total;

        if (complete) {
            Debug.Log("[Gate] Semua shard udah diambil! 🎉");
            if (unlockedUI) unlockedUI.SetActive(true);
            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        } else {
            int sisa = total - have;
            Debug.Log($"[Gate] Masih kurang {sisa} shard di scene ini ({have}/{total})");
            if (blockedUI) blockedUI.SetActive(true);
        }
    }
}
