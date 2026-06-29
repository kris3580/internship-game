using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class MenuMusicBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded += (_, _) => EnsureMusicPlayer();
        EnsureMusicPlayer();
    }

    private static void EnsureMusicPlayer()
    {
        if (MusicPlaylistPlayer.Instance != null)
            return;

        GameObject player = new("MusicPlayer");
        MusicPlaylistPlayer playlistPlayer = player.AddComponent<MusicPlaylistPlayer>();

#if UNITY_EDITOR
        SerializedObject serialized = new(playlistPlayer);
        SerializedProperty playlist = serialized.FindProperty("playlist");
        string[] clipPaths =
        {
            "Assets/Audio/Music/alexguz-funk-amp-breakbeat.wav",
            "Assets/Audio/Music/alexguz-rhythm-funk.wav",
            "Assets/Audio/Music/alexguz-vintage-funk.wav"
        };

        playlist.arraySize = clipPaths.Length;

        for (int i = 0; i < clipPaths.Length; i++)
            playlist.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPaths[i]);

        serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
    }
}
