using UnityEngine;
using TMPro;

public class CharacterSelectButton : MonoBehaviour
{
    public string characterName;
    public TMP_Text labelText;

    void Start()
    {
        if (labelText != null)
            labelText.text = characterName;
    }
}
