using System.Collections.Generic;
using UnityEngine;

public interface IBallRegistry
{
    IReadOnlyList<BallInfo> GetActiveBalls();
    void Register(GameObject candidate);
    void Unregister(GameObject candidate);
    void RefreshSceneBalls();
}
