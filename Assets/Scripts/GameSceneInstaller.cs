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
        BindComponent(audioManager);
        BindComponent(islandManager);
        BindComponent(cameraShake);
        BindComponent(ballPlacer);
        BindComponent(loseLineGameOver);
        BindAudioService();
        BindSceneTransform(spawnedBallsParent, SpawnedBallsParentId, "SpawnedBalls");
        BindSceneTransform(cameraPoint, CameraPointId, "CameraPoint");

        Container.BindInterfacesAndSelfTo<BallRegistry>().AsSingle().NonLazy();
    }

    private void BindAudioService()
    {
        GameAudioManager resolvedAudioManager = ResolveComponent(audioManager);

        if (resolvedAudioManager != null)
            Container.Bind<IAudioService>().FromInstance(resolvedAudioManager).AsSingle();
    }

    private void BindComponent<T>(T configuredComponent)
        where T : Component
    {
        T resolvedComponent = ResolveComponent(configuredComponent);

        if (resolvedComponent != null)
            Container.Bind<T>().FromInstance(resolvedComponent).AsSingle();
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
