using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AddressableSkinLoader : ISkinLoader
{
    public void LoadSkin(string address, Transform parent, Action<GameObject> onLoaded)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            onLoaded?.Invoke(null);
            return;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
        handle.Completed += operation =>
        {
            onLoaded?.Invoke(operation.Status == AsyncOperationStatus.Succeeded ? operation.Result : null);
        };
    }
}
