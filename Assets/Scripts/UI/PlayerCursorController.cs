using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class PlayerCursorController : MonoBehaviour
{
    public string playerName; 
    public RectTransform selectorBoxPrefab; 

    private RectTransform selectorBox;
    private RectTransform marieSpot;
    private RectTransform mimiSpot;
    private RectTransform playSpot;
    private RectTransform centerSpot;
    private RectTransform backSpot;

    private Coroutine moveRoutine;
    private float playerYOffset;
    public Sprite p1Sprite;
    public Sprite p2Sprite;
    private Image image1;  
    private Image image2;   

    private enum CursorPosition { Marie, Center, Mimi, Play, Back } 
    private CursorPosition currentPosition = CursorPosition.Center;

    private LobbyManager lobbyManager;

    void Start()
    {
        lobbyManager = FindFirstObjectByType<LobbyManager>();
        var canvas = GameObject.FindGameObjectWithTag("MainUICanvas");
        if (canvas == null) return;

        selectorBox = Instantiate(selectorBoxPrefab, canvas.transform);
        Image img = selectorBox.GetComponentInChildren<Image>();
        if (img != null) img.sprite = (playerName == "P1") ? p1Sprite : p2Sprite;

        selectorBox.GetComponentInChildren<TMPro.TMP_Text>().text = playerName;
        var text = selectorBox.GetComponentInChildren<TMPro.TMP_Text>();
        if (playerName == "P2")
        {
            Color hexColor;
            ColorUtility.TryParseHtmlString("#386082", out hexColor);  
            text.color = hexColor;
        }
        else text.color = Color.white;

        image1 = selectorBox.Find("Image (1)")?.GetComponent<Image>();
        image2 = selectorBox.Find("Image (2)")?.GetComponent<Image>();

        if (playerName == "P1") selectorBox.anchoredPosition = new Vector2(0, 100f);
        else if (playerName == "P2") selectorBox.anchoredPosition = new Vector2(0, -50f);

        playerYOffset = selectorBox.anchoredPosition.y;

        centerSpot = new GameObject($"{playerName}_CenterSpot").AddComponent<RectTransform>();
        centerSpot.SetParent(canvas.transform);
        centerSpot.sizeDelta = Vector2.zero;
        centerSpot.anchorMin = centerSpot.anchorMax = new Vector2(0.5f, 0.5f);
        centerSpot.anchoredPosition = selectorBox.anchoredPosition;

        marieSpot = GameObject.Find("MarieSpot")?.GetComponent<RectTransform>();
        mimiSpot = GameObject.Find("MimiSpot")?.GetComponent<RectTransform>();
        playSpot = GameObject.Find("PlaySpot")?.GetComponent<RectTransform>();

        GameObject backObj = GameObject.Find("Back");
        if (backObj != null) backSpot = backObj.GetComponent<RectTransform>();

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
        if (currentPosition == CursorPosition.Back) return;

        if (currentPosition == CursorPosition.Mimi)
        {
            currentPosition = CursorPosition.Center;
            if (centerSpot != null) SnapTo(centerSpot);
        }
        else if (currentPosition == CursorPosition.Center)
        {
            currentPosition = CursorPosition.Marie;
            if (marieSpot != null) SnapTo(marieSpot);
        }
        else if (currentPosition == CursorPosition.Play)
        {
            lobbyManager?.UnhighlightPlayButton(playerName);
            currentPosition = CursorPosition.Marie;
            if (marieSpot != null) SnapTo(marieSpot);
        }
        UpdateSelection();
    }

    private void MoveRight()
    {
        if (currentPosition == CursorPosition.Back) return;

        if (currentPosition == CursorPosition.Marie)
        {
            currentPosition = CursorPosition.Center;
            if (centerSpot != null) SnapTo(centerSpot);
        }
        else if (currentPosition == CursorPosition.Center)
        {
            currentPosition = CursorPosition.Mimi;
            if (mimiSpot != null) SnapTo(mimiSpot);
        }
        else if (currentPosition == CursorPosition.Play)
        {
            lobbyManager?.UnhighlightPlayButton(playerName);
            currentPosition = CursorPosition.Mimi;
            if (mimiSpot != null) SnapTo(mimiSpot);
        }
        UpdateSelection();
    }

    private void MoveDown()
    {
        // Back -> Characters
        if (currentPosition == CursorPosition.Back)
        {
            lobbyManager?.UnhighlightBackButton(); 
            if (playerName == "P1") 
            {
                currentPosition = CursorPosition.Marie;
                if (marieSpot != null) SnapTo(marieSpot);
            }
            else 
            {
                currentPosition = CursorPosition.Mimi;
                if (mimiSpot != null) SnapTo(mimiSpot);
            }
            UpdateSelection();
            return;
        }

        // Check Play Button
        if (lobbyManager != null && lobbyManager.playButton != null)
        {
            if (!lobbyManager.playButton.interactable) return;
        }

        // Characters -> Play
        if (currentPosition != CursorPosition.Play)
        {
            currentPosition = CursorPosition.Play;
            lobbyManager?.HighlightPlayButton(playerName);
            // Note: No SnapTo here, so cursor stays on character
        }
        UpdateSelection();
    }

    private void MoveUp()
    {
        // Play -> Center
        if (currentPosition == CursorPosition.Play)
        {
            lobbyManager?.UnhighlightPlayButton(playerName);
            currentPosition = CursorPosition.Center;
            SnapTo(centerSpot);
        }
        // Characters -> Back
        else if (currentPosition == CursorPosition.Marie || currentPosition == CursorPosition.Mimi || currentPosition == CursorPosition.Center)
        {
            if (backSpot != null)
            {
                currentPosition = CursorPosition.Back;
                lobbyManager?.HighlightBackButton(); 
                
                // 🟢 FIX: Removed SnapTo(backSpot)
                // Cursor visual now stays on Marie/Mimi/Center
            }
        }
        UpdateSelection();
    }

    private void SnapTo(RectTransform target)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        if (target == null) return;

        Vector2 targetPos = target.anchoredPosition;
        if (playerName == "P2" && currentPosition != CursorPosition.Play && currentPosition != CursorPosition.Back)
        {
            targetPos.y = playerYOffset;
        }
        moveRoutine = StartCoroutine(MoveToTarget(targetPos));
    }

    private IEnumerator MoveToTarget(Vector2 targetPos)
    {
        float duration = 0.25f;
        float elapsed = 0f;
        Vector2 startPos = selectorBox.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            selectorBox.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        selectorBox.anchoredPosition = targetPos;
    }

    private void UpdateSelection()
    {
        if (!lobbyManager) return;

        if (currentPosition == CursorPosition.Marie)
            lobbyManager.UpdatePlayerSelection(playerName, "Marie");
        else if (currentPosition == CursorPosition.Mimi)
            lobbyManager.UpdatePlayerSelection(playerName, "Mimi");

        if (currentPosition != CursorPosition.Play && currentPosition != CursorPosition.Back)
        {
            lobbyManager.UpdatePlayerPosition(playerName, currentPosition.ToString());
        }
        
        UpdateSideIndicators();
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (!context.performed || !EnsureLobbyManager()) return;

        if (currentPosition == CursorPosition.Back)
        {
            lobbyManager.BackToMainMenu();
            return;
        }

        if (currentPosition == CursorPosition.Marie)
            lobbyManager.UpdatePlayerSelection(playerName, "Marie");
        else if (currentPosition == CursorPosition.Mimi)
            lobbyManager.UpdatePlayerSelection(playerName, "Mimi");

        if (currentPosition == CursorPosition.Play)
        {
            lobbyManager.OnPlayerConfirm(playerName);
        }
    }

    private bool EnsureLobbyManager()
    {
        if (lobbyManager != null) return true;
        lobbyManager = FindFirstObjectByType<LobbyManager>();
        return lobbyManager != null;
    }

    public string GetCurrentCharacter()
    {
        if (currentPosition == CursorPosition.Marie) return "Marie";
        if (currentPosition == CursorPosition.Mimi) return "Mimi";
        return null;
    }

    private void UpdateSideIndicators()
    {
        if (image1 == null || image2 == null) return;

        if (currentPosition == CursorPosition.Marie) {
            image1.gameObject.SetActive(false);
            image2.gameObject.SetActive(true);
        } else if (currentPosition == CursorPosition.Mimi) {
            image1.gameObject.SetActive(true);
            image2.gameObject.SetActive(false);
        } else if (currentPosition == CursorPosition.Center) {
            image1.gameObject.SetActive(true);
            image2.gameObject.SetActive(true);
        } else {
            image1.gameObject.SetActive(false);
            image2.gameObject.SetActive(false);
        }
    }
}