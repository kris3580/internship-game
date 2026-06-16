using Zenject;

public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<ISceneLoader>().To<UnitySceneLoader>().AsSingle();
        Container.Bind<ISaveSystem>().To<PlayerPrefsSaveSystem>().AsSingle();
        Container.Bind<IAudioService>().To<NullAudioService>().AsSingle();
    }
}
