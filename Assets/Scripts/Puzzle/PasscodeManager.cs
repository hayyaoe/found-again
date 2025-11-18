using UnityEngine;
using UnityEngine.SceneManagement;

public class PasscodeManager : MonoBehaviour
{
    [Header("Target Passcode")]
    [Tooltip("The correct passcode sequence for this scene (length = number of boxes)")]
    public int[] correctCode;

    [Header("Boxes in this puzzle")]
    [Tooltip("Assign all PasscodeBox objects for this puzzle")]

    [Header("Linked Objects")]
    public AutoElevator2D lift;

    public PasscodeBox[] passcodeBoxes;
    public bool IsSolved { get; private set; } = false;
    
    [Header("UI Objects")]
    public GameObject interactUIZone;
    public GameObject jumpUIZone;

    private void Start()
    {
        // Let each box know who the manager is
        foreach (var box in passcodeBoxes)
        {
            box.AssignManager(this);
        }
    }

    public void CheckPasscode()
    {
        if (passcodeBoxes.Length != correctCode.Length)
        {
            Debug.LogWarning("Passcode length mismatch!");
            return;
        }

        bool allCorrect = true;

        for (int i = 0; i < passcodeBoxes.Length; i++)
        {
            if (passcodeBoxes[i].GetValue() != correctCode[i])
            {
                allCorrect = false;
            }
        }

        // Apply light state AFTER full evaluation
        foreach (var box in passcodeBoxes)
        {
            box.SetLightState(allCorrect);
        }

        if (allCorrect)
        {
            Debug.Log("✅ Correct passcode! Puzzle solved!");

            // Auto-stop all player interactions with boxes
            foreach(var box in passcodeBoxes)
                box.ForceEndInteraction();

            OnPasscodeSolved();
        }
        else
        {
            Debug.Log("❌ Incorrect passcode.");
        }
    }

    private void OnPasscodeSolved()
    {
        IsSolved = true;

        Debug.Log("✅ Correct passcode! Puzzle solved!");

        ToggleUI(true);

        if (lift != null)
        {
            lift.ActivateLift(); // 🎯 Activate the lift
        }
        else
        {
            Debug.LogWarning("No lift assigned to PasscodeManager!");
        }
    }

    private void ToggleUI(bool solved)
    {
        if (interactUIZone != null)
            interactUIZone.SetActive(!solved);

        if (jumpUIZone != null)
            jumpUIZone.SetActive(solved);
    }
}
