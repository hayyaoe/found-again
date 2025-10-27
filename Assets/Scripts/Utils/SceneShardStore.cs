// SceneShardStore.cs
using System.Collections.Generic;
using UnityEngine;

public static class SceneShardStore {
    static string Key(string sceneName) => $"shards_{sceneName}";

    public static HashSet<string> Load(string sceneName) {
        var set = new HashSet<string>();
        var csv = PlayerPrefs.GetString(Key(sceneName), "");
        if (!string.IsNullOrEmpty(csv)) {
            foreach (var id in csv.Split(',')) if (!string.IsNullOrWhiteSpace(id)) set.Add(id);
        }
        return set;
    }

    public static bool Add(string sceneName, string shardId) {
        var set = Load(sceneName);
        var added = set.Add(shardId);
        if (added) {
            PlayerPrefs.SetString(Key(sceneName), string.Join(",", set));
            PlayerPrefs.Save();
        }
        return added;
    }

    public static bool IsCollected(string sceneName, string shardId) =>
        Load(sceneName).Contains(shardId);

    public static int Count(string sceneName) => Load(sceneName).Count;
}
