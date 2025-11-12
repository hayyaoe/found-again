using UnityEngine;
using UnityEngine.SceneManagement;

public class PasscodeManager : MonoBehaviour
{
    [Header("Target Passcode")]
    [Tooltip("The correct passcode sequence for this scene (length = number of boxes)")]
    public int[] correctCode;

    [Header("Boxes in this puzzle")]
    [Tooltip("Assign all PasscodeBox objects for this puzzle")]
    public PasscodeBox[] passcodeBoxes;

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
            bool isCorrect = passcodeBoxes[i].GetValue() == correctCode[i];
            passcodeBoxes[i].SetLightState(isCorrect);

            if (!isCorrect)
                allCorrect = false;
        }

        if (allCorrect)
        {
            Debug.Log("✅ Correct passcode! Puzzle solved!");
            OnPasscodeSolved();
        }
        else
        {
            Debug.Log("❌ Incorrect passcode.");
        }
    }


    private void OnPasscodeSolved()
    {
        // TODO: Add your unlock logic here
        // e.g. open a door, trigger animation, load next scene, etc.
        SceneManager.LoadScene("MainMenu");
    }
}
