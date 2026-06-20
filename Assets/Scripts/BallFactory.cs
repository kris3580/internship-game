using System.Collections.Generic;
using UnityEngine;
using Zenject;

public sealed class BallFactory : IBallFactory
{
    private readonly DiContainer container;
    private readonly Dictionary<string, GameObject> prefabsByTag = new();
    private readonly Dictionary<string, Queue<GameObject>> poolsByTag = new();
    private readonly string[] normalTags = { "2", "3", "4", "5", "6", "7", "8" };

    public BallFactory(DiContainer container)
    {
        this.container = container;
    }

    public void RegisterPrefabs(params GameObject[] prefabs)
    {
        if (prefabs == null)
            return;

        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null || string.IsNullOrWhiteSpace(prefab.tag))
                continue;

            prefabsByTag[prefab.tag] = prefab;

            if (!poolsByTag.ContainsKey(prefab.tag))
                poolsByTag[prefab.tag] = new Queue<GameObject>();
        }
    }

    public GameObject Spawn(string ballTag, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (string.IsNullOrWhiteSpace(ballTag) || !prefabsByTag.TryGetValue(ballTag, out GameObject prefab))
            return null;

        Queue<GameObject> pool = poolsByTag[ballTag];
        GameObject ball = null;

        while (pool.Count > 0 && ball == null)
            ball = pool.Dequeue();

        if (ball == null)
            ball = container.InstantiatePrefab(prefab);

        Transform ballTransform = ball.transform;
        ballTransform.SetParent(parent, false);
        ballTransform.SetPositionAndRotation(position, rotation);
        ball.SetActive(true);
        ResetPhysics(ball);

        return ball;
    }

    public void Despawn(GameObject ball)
    {
        if (ball == null)
            return;

        string ballTag = ball.tag;

        if (!poolsByTag.ContainsKey(ballTag))
            poolsByTag[ballTag] = new Queue<GameObject>();

        ResetPhysics(ball);
        ball.SetActive(false);
        ball.transform.SetParent(null, false);
        poolsByTag[ballTag].Enqueue(ball);
    }

    public string GetRandomNormalTag()
    {
        return normalTags[Random.Range(0, normalTags.Length)];
    }

    public bool HasBall(string ballTag)
    {
        return !string.IsNullOrWhiteSpace(ballTag) && prefabsByTag.ContainsKey(ballTag);
    }

    private static void ResetPhysics(GameObject ball)
    {
        Rigidbody body = ball.GetComponent<Rigidbody>();

        if (body == null)
            return;

        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.useGravity = false;
        body.isKinematic = true;
        body.detectCollisions = false;
    }
}
