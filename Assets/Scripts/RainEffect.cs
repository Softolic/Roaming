using UnityEngine;

[DisallowMultipleComponent]
public class RainEffect : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(1f)] private float heightAboveTarget = 9f;
    [SerializeField] private Material rainMaterial;

    private ParticleSystem rain;

    private void Awake()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        CreateRain();
        FollowTarget();
    }

    private void LateUpdate() => FollowTarget();

    private void FollowTarget()
    {
        if (target != null)
            transform.position = target.position + Vector3.up * heightAboveTarget;
    }

private void CreateRain()
    {
        rain = GetComponent<ParticleSystem>();
        if (rain == null) rain = gameObject.AddComponent<ParticleSystem>();

        var main = rain.main;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = 1400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.25f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.035f);
        main.startColor = Color.white;

        var emission = rain.emission;
        emission.enabled = true;
        emission.rateOverTime = 900f;

        var shape = rain.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(70f, 0.1f, 50f);

        var velocity = rain.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.7f, 0.7f);
        velocity.y = new ParticleSystem.MinMaxCurve(-15f, -19f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.7f, 0.7f);

        var renderer = rain.GetComponent<ParticleSystemRenderer>();
        if (rainMaterial != null)
            renderer.sharedMaterial = rainMaterial;

        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.08f;
        renderer.lengthScale = 0.4f;
        renderer.minParticleSize = 0.001f;

        rain.Play(true);
    }
}