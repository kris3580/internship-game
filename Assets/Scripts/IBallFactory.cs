using UnityEngine;

public interface IBallFactory
{
    void RegisterPrefabs(params GameObject[] prefabs);
    GameObject Spawn(string ballTag, Vector3 position, Quaternion rotation, Transform parent = null);
    void Despawn(GameObject ball);
    string GetRandomNormalTag();
    bool HasBall(string ballTag);
}
