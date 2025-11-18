using UnityEngine;

public static class SaveSystem
{
    private const string CHECKPOINT_KEY = "LastCheckpointID";

    public static void SaveCheckpoint(string checkpointID)
    {
        PlayerPrefs.SetString(CHECKPOINT_KEY, checkpointID);
        PlayerPrefs.Save();
    }

    public static string LoadCheckpoint()
    {
        return PlayerPrefs.GetString(CHECKPOINT_KEY, ""); // returns "" if no save
    }

    public static bool HasSave()
    {
        return PlayerPrefs.HasKey(CHECKPOINT_KEY);
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(CHECKPOINT_KEY);
    }
}
