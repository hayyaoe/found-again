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

        if (lift != null)
        {
            lift.ActivateLift(); // 🎯 Activate the lift
        }
        else
        {
            Debug.LogWarning("No lift assigned to PasscodeManager!");
        }
    }
}
