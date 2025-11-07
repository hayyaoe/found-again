using UnityEngine;
using System.Collections.Generic;
using System.Linq; // We need this for .Where()

[System.Serializable]
public class DialogueDataEntry
{
    public string sceneID;
    public string cutsceneID;
    public string speakerName;
    public string dialogueLine;
}

public class DialogueDatabase : MonoBehaviour
{
    public static DialogueDatabase instance;

    [Header("CSV File")]
    [SerializeField] private string csvFileName = "DialogueCutsceneChat"; 

    private List<DialogueDataEntry> allDialogue = new List<DialogueDataEntry>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDialogueData(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadDialogueData()
    {
        TextAsset ta = Resources.Load<TextAsset>(csvFileName);

        if (ta == null)
        {
            Debug.LogError($"DIALOGUE DATABASE: Could not find CSV file at 'Resources/{csvFileName}'. Make sure it's spelled correctly and is in a Resources folder.");
            return;
        }

        string[] lines = ta.text.Split('\n');
        
        // Skip the header (row 1) and start from row 2
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] fields = line.Split(';');

            if (fields.Length < 4) continue; 
            
            // --- THIS IS THE FIX ---
            // Re-mapping the fields to match your CSV's order:
            // 0: chat, 1: name, 2: scene, 3: cutscene
            DialogueDataEntry entry = new DialogueDataEntry
            {
                dialogueLine = fields[0].Trim(), // 0: chat
                speakerName = fields[1].Trim(),  // 1: name
                sceneID = fields[2].Trim(),      // 2: scene
                cutsceneID = fields[3].Trim()    // 3: cutscene
            };
            // --- END OF FIX ---
            
            allDialogue.Add(entry);
        }
        
        Debug.Log($"Dialogue Database loaded with {allDialogue.Count} lines.");
    }

    public List<DialogueDataEntry> GetDialogueFor(string cutsceneID)
    {
        // This function will now work because the data was loaded correctly
        return allDialogue.Where(line => line.cutsceneID == cutsceneID).ToList();
    }
}