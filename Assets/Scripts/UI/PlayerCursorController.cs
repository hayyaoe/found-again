using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class PlayerCursorController : MonoBehaviour
{
    public string playerName; // "P1" or "P2"
    public RectTransform selectorBoxPrefab; // prefab reference for the UI box (P1/P2)

    private RectTransform selectorBox;
    private RectTransform marieSpot;
    private RectTransform mimiSpot;
    private RectTransform playSpot;
    private RectTransform centerSpot;
    private Coroutine moveRoutine;
    private float playerYOffset;


    // automatic Y offset for Player 2
    private float yOffset = 0f;

    private enum CursorPosition { Marie, Center, Mimi, Play }
    private CursorPosition currentPosition = CursorPosition.Marie;

    private LobbyManager lobbyManager;

    void Start()
    {
        lobbyManager = FindFirstObjectByType<LobbyManager>();

        // 🔍 Automatically find the Canvas in the scene
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ No Canvas found in scene!");
            return;
        }

        // 🧱 Instantiate the P1/P2 selector box
        selectorBox = Instantiate(selectorBoxPrefab, canvas.transform);
        selectorBox.GetComponentInChildren<TMPro.TMP_Text>().text = playerName;

        // 🧩 Determine initial Y offset dynamically
        if (playerName == "P1")
            selectorBox.anchoredPosition = new Vector2(0, 200f);
        else if (playerName == "P2")
            selectorBox.anchoredPosition = new Vector2(0, -60f);
        else
            selectorBox.anchoredPosition = Vector2.zero;

        // 💾 Store Player's initial Y offset (used later in SnapTo)
        playerYOffset = selectorBox.anchoredPosition.y;

        // 🌀 Create centerSpot after selectorBox exists
        centerSpot = new GameObject($"{playerName}_CenterSpot").AddComponent<RectTransform>();
        centerSpot.SetParent(canvas.transform);
        centerSpot.sizeDelta = Vector2.zero;
        centerSpot.anchorMin = centerSpot.anchorMax = new Vector2(0.5f, 0.5f);
        centerSpot.anchoredPosition = selectorBox.anchoredPosition;

        // 🎯 Find other spots (must match scene names exactly)
        marieSpot = GameObject.Find("MarieSpot")?.GetComponent<RectTransform>();
        mimiSpot = GameObject.Find("MimiSpot")?.GetComponent<RectTransform>();
        playSpot = GameObject.Find("PlaySpot")?.GetComponent<RectTransform>();

        currentPosition = CursorPosition.Center;
        UpdateSelection();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Vector2 move = context.ReadValue<Vector2>();

        if (Mathf.Abs(move.x) > 0.5f)
        {
            if (move.x > 0.5f) MoveRight();
            else if (move.x < -0.5f) MoveLeft();
        }

        if (Mathf.Abs(move.y) > 0.5f)
        {
            if (move.y < -0.5f) MoveDown();
            else if (move.y > 0.5f) MoveUp();
        }
    }

    private void MoveLeft()
    {
        if (currentPosition == CursorPosition.Mimi)
        {
            currentPosition = CursorPosition.Center;
            if (centerSpot != null)
                SnapTo(centerSpot);
        }
        else if (currentPosition == CursorPosition.Center)
        {
            currentPosition = CursorPosition.Marie;
            if (marieSpot != null)
                SnapTo(marieSpot);
        }
        UpdateSelection();
    }

    private void MoveRight()
    {
        if (currentPosition == CursorPosition.Marie)
        {
            currentPosition = CursorPosition.Center;
            if (centerSpot != null)
                SnapTo(centerSpot);
        }
        else if (currentPosition == CursorPosition.Center)
        {
            currentPosition = CursorPosition.Mimi;
            if (mimiSpot != null)
                SnapTo(mimiSpot);
        }
        UpdateSelection();
    }


    private void MoveDown()
    {
        // only allow moving down if the play button is active/interactable
        if (lobbyManager != null && lobbyManager.playButton != null)
        {
            if (!lobbyManager.playButton.interactable)
            {
                Debug.Log($"⚠️ {playerName} tried to move down, but Play button not available yet!");
                return; // do nothing if play button is disabled
            }
        }

        if (currentPosition != CursorPosition.Play)
        {
            currentPosition = CursorPosition.Play;

            // highlight play button for feedback
            if (lobbyManager != null && lobbyManager.playButton != null)
            {
                lobbyManager.HighlightPlayButton(playerName);
            }
        }
    }

    private void MoveUp()
    {
        if (currentPosition == CursorPosition.Play)
        {
            currentPosition = CursorPosition.Center;
            SnapTo(centerSpot);
            UpdateSelection();
        }
    }

    private void SnapTo(RectTransform target)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        if (target == null)
        {
            Debug.LogWarning($"⚠️ {playerName} tried to move to a null target!");
            return;
        }

        Vector2 targetPos = target.anchoredPosition;

        if (playerName == "P2" && currentPosition != CursorPosition.Play)
        {
            targetPos.y = playerYOffset; // use saved starting Y position
        }

        moveRoutine = StartCoroutine(MoveToTarget(targetPos));
    }


    // MoveToTarget accepts a Vector2 position (fixed)
    private IEnumerator MoveToTarget(Vector2 targetPos)
    {
        float duration = 0.25f;
        float elapsed = 0f;

        Vector2 startPos = selectorBox.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // ease-out
            selectorBox.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        selectorBox.anchoredPosition = targetPos;
    }

    private void UpdateSelection()
    {
        if (!lobbyManager) return;

        string currentPosName = currentPosition.ToString();

        // Update LobbyManager with current cursor position
        lobbyManager.UpdatePlayerPosition(playerName, currentPosName);

        // Only send selection updates when actually on a character
        if (currentPosition == CursorPosition.Marie)
            lobbyManager.UpdatePlayerSelection(playerName, "Marie");
        else if (currentPosition == CursorPosition.Mimi)
            lobbyManager.UpdatePlayerSelection(playerName, "Mimi");
    }
}
