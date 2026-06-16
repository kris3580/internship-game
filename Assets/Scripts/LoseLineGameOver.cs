using System.Collections.Generic;
using UnityEngine;

public class LoseLineGameOver : MonoBehaviour
{
    [SerializeField] private LayerMask ballLayer;
    [SerializeField] private int requiredBallCount = 3;
    [SerializeField] private float checkInterval = 0.25f;
    [SerializeField] private float heightOffset;
    [SerializeField] private float halfDepthX = 0.5f;
    [SerializeField] private float halfWidthZ = 4f;
    [SerializeField] private float maximumCountedSpeed = 0.35f;
    [SerializeField] private Color gizmoColor = new(1f, 0.1f, 0.1f, 0.35f);

    private readonly HashSet<GameObject> countedBalls = new();
    private float nextCheckTime;
    private bool gameOverLogged;

    private float Height => transform.position.y + heightOffset;

    private void Update()
    {
        if (gameOverLogged || Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + checkInterval;

        if (CountBallsAtLine() >= requiredBallCount)
        {
            gameOverLogged = true;
            Debug.Log("Game over", this);
        }

        print("balls going against line: " + countedBalls.Count);
    }

    private int CountBallsAtLine()
    {
        countedBalls.Clear();
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);

        foreach (Collider col in colliders)
        {
            if (!TryGetBall(col, out GameObject ball))
                continue;

            if (Mathf.Abs(ball.transform.position.x - transform.position.x) > halfDepthX)
                continue;

            if (Mathf.Abs(ball.transform.position.z - transform.position.z) > halfWidthZ)
                continue;

            Rigidbody body = ball.GetComponent<Rigidbody>();

            if (body != null && body.linearVelocity.magnitude > maximumCountedSpeed)
                continue;

            if (col.bounds.max.y < Height)
                continue;

            countedBalls.Add(ball);
        }

        return countedBalls.Count;
    }

    private bool TryGetBall(Collider col, out GameObject ball)
    {
        ball = null;

        if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
            return false;

        Transform current = col.transform;

        while (current != null)
        {
            if (int.TryParse(current.tag, out _))
            {
                ball = current.gameObject;
                break;
            }

            current = current.parent;
        }

        if (ball == null)
            return false;

        return ballLayer.value == 0 || IsInMask(ball.layer, ballLayer.value) || IsInMask(col.gameObject.layer, ballLayer.value);
    }

    private bool IsInMask(int layer, int mask)
    {
        return (mask & (1 << layer)) != 0;
    }

    private void OnDrawGizmos()
    {
        Color oldColor = Gizmos.color;
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(
            new Vector3(transform.position.x, Height, transform.position.z),
            new Vector3(halfDepthX * 2f, 0.05f, halfWidthZ * 2f)
        );
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            new Vector3(transform.position.x, Height, transform.position.z),
            new Vector3(halfDepthX * 2f, 0.05f, halfWidthZ * 2f)
        );
        Gizmos.color = oldColor;
    }

    private void OnValidate()
    {
        requiredBallCount = Mathf.Max(1, requiredBallCount);
        checkInterval = Mathf.Max(0.02f, checkInterval);
        halfDepthX = Mathf.Max(0.01f, halfDepthX);
        halfWidthZ = Mathf.Max(0.01f, halfWidthZ);
        maximumCountedSpeed = Mathf.Max(0f, maximumCountedSpeed);
    }
}
