using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float duration = 0.12f;
    [SerializeField] private float positionStrength = 0.08f;
    [SerializeField] private float rotationStrength = 1.2f;
    [SerializeField] private float frequency = 45f;
    [Header("Phone Camera Motion")]
    [SerializeField] private bool phoneMotionEnabled = true;
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private bool calibratePhoneOnEnable = true;
    [SerializeField] private Vector2 phoneInputSensitivity = Vector2.one;
    [SerializeField] private float maxPhoneTilt = 0.35f;
    [SerializeField] private float phonePositionStrength = 0.12f;
    [SerializeField] private float phoneRollStrength = 1.2f;
    [SerializeField] private float phoneSmoothSpeed = 8f;

    private Coroutine shakeRoutine;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 shakeLocalOffset;
    private Quaternion shakeLocalRotation = Quaternion.identity;
    private Vector2 neutralPhoneAcceleration;
    private Vector2 smoothedPhoneTilt;
    private bool hasBasePose;

    private void Awake()
    {
        if (target == null)
            target = transform;

        if (cameraPoint == null)
        {
            GameObject point = GameObject.Find("CameraPoint");

            if (point != null)
                cameraPoint = point.transform;
        }

        CaptureBasePose();
    }

    private void OnEnable()
    {
        CaptureBasePose();

        if (calibratePhoneOnEnable)
            CalibratePhoneMotion();
    }

    private void LateUpdate()
    {
        UpdatePhoneCameraMotion();
        ApplyCameraPose();
    }

    public void Shake()
    {
        if (target == null)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    public void CalibratePhoneMotion()
    {
        neutralPhoneAcceleration = GetPhoneAcceleration();
        smoothedPhoneTilt = Vector2.zero;
    }

    public void ApplyPhoneCameraMotion(Vector2 phoneTilt)
    {
        smoothedPhoneTilt = Vector2.ClampMagnitude(phoneTilt, maxPhoneTilt);
        ApplyCameraPose();
    }

    private void CaptureBasePose()
    {
        if (target == null)
            return;

        baseLocalPosition = target.localPosition;
        baseLocalRotation = target.localRotation;
        hasBasePose = true;
    }

    private void UpdatePhoneCameraMotion()
    {
        if (!phoneMotionEnabled)
        {
            smoothedPhoneTilt = SmoothTiltToward(Vector2.zero);
            return;
        }

        Vector2 tilt = GetPhoneAcceleration() - neutralPhoneAcceleration;
        tilt = new Vector2(tilt.x * phoneInputSensitivity.x, tilt.y * phoneInputSensitivity.y);
        tilt = Vector2.ClampMagnitude(tilt, maxPhoneTilt);
        smoothedPhoneTilt = SmoothTiltToward(tilt);
    }

    private Vector2 SmoothTiltToward(Vector2 tilt)
    {
        float t = 1f - Mathf.Exp(-phoneSmoothSpeed * Time.deltaTime);
        return Vector2.Lerp(smoothedPhoneTilt, tilt, t);
    }

    private Vector2 GetPhoneAcceleration()
    {
        Vector3 acceleration = Input.acceleration;
        return new Vector2(acceleration.x, acceleration.y);
    }

    private void ApplyCameraPose()
    {
        if (target == null || !hasBasePose)
            return;

        Vector3 phoneOffset = new(
            smoothedPhoneTilt.x * phonePositionStrength,
            smoothedPhoneTilt.y * phonePositionStrength,
            0f
        );

        Vector3 localPosition = baseLocalPosition + phoneOffset + shakeLocalOffset;
        Quaternion localRotation = GetLookAtLocalRotation(localPosition) * shakeLocalRotation;

        target.localPosition = localPosition;
        target.localRotation = localRotation;
    }

    private Quaternion GetLookAtLocalRotation(Vector3 localPosition)
    {
        if (!phoneMotionEnabled || cameraPoint == null)
            return baseLocalRotation;

        Transform parent = target.parent;
        Vector3 worldPosition = parent != null
            ? parent.TransformPoint(localPosition)
            : localPosition;

        Vector3 lookDirection = cameraPoint.position - worldPosition;

        if (lookDirection.sqrMagnitude <= 0.0001f)
            return baseLocalRotation;

        Quaternion worldRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
            * Quaternion.Euler(0f, 0f, -smoothedPhoneTilt.x * phoneRollStrength);

        return parent != null
            ? Quaternion.Inverse(parent.rotation) * worldRotation
            : worldRotation;
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float strength = 1f - elapsed / duration;
            float step = Time.time * frequency;
            Vector3 offset = new(
                Mathf.PerlinNoise(step, 0.13f) - 0.5f,
                Mathf.PerlinNoise(0.31f, step) - 0.5f,
                0f
            );

            shakeLocalOffset = offset * positionStrength * strength;
            shakeLocalRotation = Quaternion.Euler(0f, 0f, offset.x * rotationStrength * strength);

            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeLocalOffset = Vector3.zero;
        shakeLocalRotation = Quaternion.identity;
        shakeRoutine = null;
    }

    private void OnValidate()
    {
        if (target == null)
            target = transform;

        duration = Mathf.Max(0f, duration);
        positionStrength = Mathf.Max(0f, positionStrength);
        rotationStrength = Mathf.Max(0f, rotationStrength);
        frequency = Mathf.Max(1f, frequency);
        maxPhoneTilt = Mathf.Max(0.01f, maxPhoneTilt);
        phonePositionStrength = Mathf.Max(0f, phonePositionStrength);
        phoneRollStrength = Mathf.Max(0f, phoneRollStrength);
        phoneSmoothSpeed = Mathf.Max(0.01f, phoneSmoothSpeed);
    }
}
