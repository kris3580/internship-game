using UnityEngine;

public interface ISceneLoader
{
    AsyncOperation LoadSceneAsync(string sceneName);
}
