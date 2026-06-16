using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class UnitySceneLoader : ISceneLoader
{
    public AsyncOperation LoadSceneAsync(string sceneName)
    {
        return SceneManager.LoadSceneAsync(sceneName);
    }
}
