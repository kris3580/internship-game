using System;
using UnityEngine;

public interface ISkinLoader
{
    void LoadSkin(string address, Transform parent, Action<GameObject> onLoaded);
}
