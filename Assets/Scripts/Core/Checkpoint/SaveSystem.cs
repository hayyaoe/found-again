using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class SaveSystem
{
    private const string CHECKPOINT_ID_KEY = "LastCheckpointID";

    // New position keys
    private const string CHECKPOINT_POS_X = "chk_x";
    private const string CHECKPOINT_POS_Y = "chk_y";
    private const string CHECKPOINT_POS_Z = "chk_z";

    // -----------------------------
    //         CHECKPOINT ID
    // -----------------------------
    public static void SaveCheckpointID(string checkpointID)
    {
        PlayerPrefs.SetString(CHECKPOINT_ID_KEY, checkpointID);
        PlayerPrefs.Save();
    }

    public static string LoadCheckpointID()
    {
        return PlayerPrefs.GetString(CHECKPOINT_ID_KEY, "");
    }

    public static bool HasSave()
    {
        return PlayerPrefs.HasKey(CHECKPOINT_ID_KEY);
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(CHECKPOINT_ID_KEY);

        // clear new position keys as well
        PlayerPrefs.DeleteKey(CHECKPOINT_POS_X);
        PlayerPrefs.DeleteKey(CHECKPOINT_POS_Y);
        PlayerPrefs.DeleteKey(CHECKPOINT_POS_Z);
    }

    // -----------------------------
    //       NEW VECTOR3 SAVE
    // -----------------------------
    public static void SaveCheckpointPosition(Vector3 pos)
    {
        PlayerPrefs.SetFloat(CHECKPOINT_POS_X, pos.x);
        PlayerPrefs.SetFloat(CHECKPOINT_POS_Y, pos.y);
        PlayerPrefs.SetFloat(CHECKPOINT_POS_Z, pos.z);
        PlayerPrefs.Save();
    }

    public static bool HasSavedPosition()
    {
        return PlayerPrefs.HasKey(CHECKPOINT_POS_X);
    }

    public static Vector3 LoadCheckpointPosition()
    {
        if (!HasSavedPosition())
            return Vector3.zero;

        return new Vector3(
            PlayerPrefs.GetFloat(CHECKPOINT_POS_X),
            PlayerPrefs.GetFloat(CHECKPOINT_POS_Y),
            PlayerPrefs.GetFloat(CHECKPOINT_POS_Z)
        );
    }
    public static void ClearAllShardData()
    {
        // Delete every scene shard data PlayerPref
        foreach (var key in PlayerPrefsKeysToDelete())
            PlayerPrefs.DeleteKey(key);

        PlayerPrefs.Save();
    }

    private static IEnumerable<string> PlayerPrefsKeysToDelete()
    {
        foreach (var key in PlayerPrefsKeys())
            if (key.StartsWith("shards_"))
                yield return key;
    }

    // Helper to list all keys in PlayerPrefs
    private static IEnumerable<string> PlayerPrefsKeys()
    {
        var field = typeof(PlayerPrefs).GetField("sPlayerPrefs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var dict = field.GetValue(null) as System.Collections.IDictionary;

        foreach (var key in dict.Keys)
            yield return key.ToString();
    }

}
