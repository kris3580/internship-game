using UnityEngine;
using Zenject;

public class GameSceneInstaller : MonoInstaller
{
    public const string SpawnedBallsParentId = "SpawnedBallsParent";
    public const string CameraPointId = "CameraPoint";

    [SerializeField] private GameAudioManager audioManager;
    [SerializeField] private IslandManager islandManager;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private BallPlacer ballPlacer;
    [SerializeField] private LoseLineGameOver loseLineGameOver;
    [SerializeField] private Transform spawnedBallsParent;
    [SerializeField] private Transform cameraPoint;

    public override void InstallBindings()
    {
        RebindAudioService();
        BindSceneTransform(spawnedBallsParent, SpawnedBallsParentId, "SpawnedBalls");
        BindSceneTransform(cameraPoint, CameraPointId, "CameraPoint");

        Container.BindInterfacesAndSelfTo<BallRegistry>().AsSingle().NonLazy();
    }

    private void RebindAudioService()
    {
        GameAudioManager resolvedAudioManager = ResolveComponent(audioManager);

        if (resolvedAudioManager != null)
            Container.Rebind<IAudioService>().FromInstance(resolvedAudioManager).AsSingle();
    }

    private T ResolveComponent<T>(T configuredComponent)
        where T : Component
    {
        return configuredComponent != null
            ? configuredComponent
            : FindFirstObjectByType<T>();
    }

    private void BindSceneTransform(Transform configuredTransform, string id, string objectName)
    {
        Transform resolvedTransform = configuredTransform != null
            ? configuredTransform
            : FindTransformByName(objectName);

        if (resolvedTransform != null)
            Container.Bind<Transform>().WithId(id).FromInstance(resolvedTransform);
    }

    private Transform FindTransformByName(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }
}
