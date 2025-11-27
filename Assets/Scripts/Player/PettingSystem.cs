using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PettingSystem : MonoBehaviour
{
    [Header("Auto-found Players (do NOT assign manually)")]
    public Movement player1;
    public Movement player2;
    private PlayerInput p1Input;
    private PlayerInput p2Input;

    [Header("Settings")]
    public float requiredDistance = 1.5f;
    public float stationaryTime = 3f;

    [Header("UI Fade")]
    public CanvasGroup petCanvasGroup;
    public float fadeDuration = 0.4f;

    [Header("Tuning")]
    public float stillThreshold = 0.5f;

    private float timer;
    private bool petting;
    private Coroutine fadeCoroutine;
    public static bool PettingInProgress = false;
    
    [Header("Sound")]
    public AudioClip[] pettingRandomSFX;


    private void Start()
    {
        if (petCanvasGroup != null)
            petCanvasGroup.alpha = 0f;

        FindPlayers();
    }

    private void Update()
    {
        // Auto-refind players if missing
        if (!player1 || !player2)
        {
            FindPlayers();
            return;
        }

        if (petting)
            return;

        var p1Rb = player1.GetComponent<Rigidbody2D>();
        var p2Rb = player2.GetComponent<Rigidbody2D>();
        p1Input = player1.GetComponent<PlayerInput>();
        p2Input = player2.GetComponent<PlayerInput>();

        float distance = Vector2.Distance(player1.transform.position, player2.transform.position);
        bool closeEnough = distance <= requiredDistance;
        bool p1Still = p1Rb.linearVelocity.magnitude < stillThreshold;
        bool p2Still = p2Rb.linearVelocity.magnitude < stillThreshold;
        bool p1Grounded = Mathf.Abs(p1Rb.linearVelocity.y) < 0.2f;
        bool p2Grounded = Mathf.Abs(p2Rb.linearVelocity.y) < 0.2f;

        if (!BoatMove.AnyPlayerOnBoat && closeEnough && p1Still && p2Still && p1Grounded && p2Grounded)
        {
            timer += Time.deltaTime;
        }
        else
        {
            timer = 0f;
            FadeTo(0f);
        }

        if (timer >= stationaryTime)
        { 
            FadeTo(1f);
        }
    }

    private void FindPlayers()
    {
        var found = GameObject.FindGameObjectsWithTag("Player");

        if (found.Length < 2)
            return;

        player1 = found[0].GetComponent<Movement>();
        player2 = found[1].GetComponent<Movement>();

        // Hook up input events programmatically
        p1Input = player1.GetComponent<PlayerInput>();
        p2Input = player2.GetComponent<PlayerInput>();

        p1Input.actions["Pet"].performed -= OnPetPerformed; 
        p2Input.actions["Pet"].performed -= OnPetPerformed;

        p1Input.actions["Pet"].performed += OnPetPerformed;
        p2Input.actions["Pet"].performed += OnPetPerformed;
    }

    private void OnPetPerformed(InputAction.CallbackContext ctx)
    {
        OnPetInput();
    }

    // -------------------------
    // INPUT EVENT FROM PLAYERINPUT
    // -------------------------
    public void OnPetInput()
    {
        if (BoatMove.AnyPlayerOnBoat)   // 👈 NEW
            return;
            
        if (petting || timer < stationaryTime)
            return;

        StartCoroutine(PetRoutine());
    }

    private IEnumerator PetRoutine()
    {
        petting = true;
        PettingInProgress = true;

        // Play petting SFX once
        if (pettingRandomSFX != null && pettingRandomSFX.Length > 0)
        {
            SoundFXManager.instance.PlayRandomSoundFXClip(
                pettingRandomSFX,
                player1.transform, // or player2
                0.5f
            );
        }

        FadeTo(0f);
        float originalP1X = player1.transform.position.x;
        float originalP2X = player2.transform.position.x;

        bool p1WasLeft = originalP1X < originalP2X;

        // disable movement scripts
        player1.enabled = false;
        player2.enabled = false;

        p1Input.DeactivateInput();
        p2Input.DeactivateInput();

        var rb1 = player1.GetComponent<Rigidbody2D>();
        var rb2 = player2.GetComponent<Rigidbody2D>();

        rb1.linearVelocity = Vector2.zero;
        rb1.simulated = false;

        rb2.linearVelocity = Vector2.zero;
        rb2.simulated = false;

        // X-only reposition
        float petSpacing = 1.3f;
        float midX = (originalP1X + originalP2X) / 2f;

        if (p1WasLeft)
        {
            // player1 stays on the left
            player1.transform.position = new Vector3(midX - petSpacing / 2f, player1.transform.position.y);
            player2.transform.position = new Vector3(midX + petSpacing / 2f, player2.transform.position.y);
        }
        else
        {
            // player2 stays on the left
            player1.transform.position = new Vector3(midX + petSpacing / 2f, player1.transform.position.y);
            player2.transform.position = new Vector3(midX - petSpacing / 2f, player2.transform.position.y);
        }

        // store facing direction
        Vector3 originalScale1 = player1.transform.localScale;
        Vector3 originalScale2 = player2.transform.localScale;

        Vector3 s1 = originalScale1;
        Vector3 s2 = originalScale2;

        if (p1WasLeft)
        {
            s1.x = Mathf.Abs(s1.x);   // player1 faces right
            s2.x = -Mathf.Abs(s2.x);  // player2 faces left
        }
        else
        {
            s1.x = -Mathf.Abs(s1.x);  // player1 faces left
            s2.x = Mathf.Abs(s2.x);   // player2 faces right
        }

        player1.transform.localScale = s1;
        player2.transform.localScale = s2;

        // animation
        player1.GetComponent<Animator>()?.SetBool("petting", true);
        player2.GetComponent<Animator>()?.SetBool("petting", true);

        yield return new WaitForSeconds(2f);

        player1.GetComponent<Animator>()?.SetBool("petting", false);
        player2.GetComponent<Animator>()?.SetBool("petting", false);

        // restore facing
        player1.transform.localScale = originalScale1;
        player2.transform.localScale = originalScale2;

        rb1.simulated = true;
        rb2.simulated = true;

        // enable scripts
        player1.enabled = true;
        player2.enabled = true;

        p1Input.ActivateInput();
        p2Input.ActivateInput();

        petting = false;
        timer = 0f;
        PettingInProgress = false;
    }

    private void FadeTo(float targetAlpha)
    {
        if (!petCanvasGroup) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float start = petCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            petCanvasGroup.alpha = Mathf.Lerp(start, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        petCanvasGroup.alpha = targetAlpha;
    }
    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void UnsubscribeEvents()
    {
        if (player1)
        {
            var p1 = player1.GetComponent<PlayerInput>();
            if (p1) p1.actions["Pet"].performed -= OnPetPerformed;
        }

        if (player2)
        {
            var p2 = player2.GetComponent<PlayerInput>();
            if (p2) p2.actions["Pet"].performed -= OnPetPerformed;
        }
    }

}
