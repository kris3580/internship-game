using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MetaGameBootstrap : MonoBehaviour
{
    private static MetaGameBootstrap instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        EnsureInstance();
        SceneManager.sceneLoaded += (_, _) => EnsureInstance().InstallSceneControllers();
        EnsureInstance().InstallSceneControllers();
    }

    private static MetaGameBootstrap EnsureInstance()
    {
        if (instance != null)
        {
            if (instance.GetComponent<FpsTextController>() == null)
                instance.gameObject.AddComponent<FpsTextController>();

            if (FindFirstObjectByType<FPSUnlock>() == null)
                instance.gameObject.AddComponent<FPSUnlock>();

            return instance;
        }

        GameObject root = new("__MetaGameBootstrap");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<MetaGameBootstrap>();
        root.AddComponent<MetaGameButtonSfx>();
        root.AddComponent<FpsTextController>();
        if (FindFirstObjectByType<FPSUnlock>() == null)
            root.AddComponent<FPSUnlock>();
        return instance;
    }

    private void InstallSceneControllers()
    {
        MetaGameSave.EnsureCurrencyDefaults();

        MetaGameSceneController controller = FindFirstObjectByType<MetaGameSceneController>();

        if (controller == null)
            controller = new GameObject("__MetaGameSceneController").AddComponent<MetaGameSceneController>();

        GameSkinApplier skinApplier = FindFirstObjectByType<GameSkinApplier>();

        if (skinApplier == null)
            new GameObject("__GameSkinApplier").AddComponent<GameSkinApplier>();
    }
}
