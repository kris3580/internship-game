using UnityEngine;

public interface IAudioService
{
    void PlayPoolStickHit(Vector3 position);
    void PlayBallPlace(Vector3 position);
    void PlayBallDisappear(Vector3 position, int sequenceIndex);
    void PlayCombo(Vector3 position, int comboCount);
}
