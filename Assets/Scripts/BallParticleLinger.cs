using UnityEngine;

public static class BallParticleLinger
{
    public static void Preserve(GameObject ball, float lingerSeconds)
    {
        if (ball == null || lingerSeconds <= 0f)
            return;

        ParticleSystem[] systems = ball.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem source in systems)
            PreserveSystem(source, lingerSeconds);
    }

    private static void PreserveSystem(ParticleSystem source, float lingerSeconds)
    {
        if (source == null || !source.gameObject.activeInHierarchy)
            return;

        int particleCount = source.particleCount;
        ParticleSystem.Particle[] particles = particleCount > 0
            ? new ParticleSystem.Particle[particleCount]
            : null;

        if (particleCount > 0)
            particleCount = source.GetParticles(particles);

        GameObject clone = Object.Instantiate(source.gameObject, source.transform.position, source.transform.rotation);
        clone.transform.localScale = source.transform.lossyScale;
        clone.transform.SetParent(null, true);

        ParticleSystem cloneRoot = clone.GetComponent<ParticleSystem>();
        if (cloneRoot != null)
        {
            cloneRoot.Clear(true);

            if (particleCount > 0)
                cloneRoot.SetParticles(particles, particleCount);
        }

        ParticleSystem[] cloneSystems = clone.GetComponentsInChildren<ParticleSystem>(true);
        float longestRemainingLifetime = 0f;

        foreach (ParticleSystem cloneSystem in cloneSystems)
        {
            if (cloneSystem == null)
                continue;

            ParticleSystem.Particle[] cloneParticles = new ParticleSystem.Particle[cloneSystem.particleCount];
            int count = cloneSystem.GetParticles(cloneParticles);

            for (int i = 0; i < count; i++)
                longestRemainingLifetime = Mathf.Max(longestRemainingLifetime, cloneParticles[i].remainingLifetime);

            cloneSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Object.Destroy(clone, Mathf.Max(lingerSeconds, longestRemainingLifetime));
    }
}
