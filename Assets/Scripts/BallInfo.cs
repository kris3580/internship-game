using UnityEngine;

public readonly struct BallInfo
{
    public BallInfo(GameObject root, Rigidbody rigidbody, Collider collider)
    {
        Root = root;
        Rigidbody = rigidbody;
        Collider = collider;
    }

    public GameObject Root { get; }
    public Rigidbody Rigidbody { get; }
    public Collider Collider { get; }
}
