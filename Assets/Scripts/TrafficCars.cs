using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TrafficCars : MonoBehaviour
{
    [Header("Traffic")]
    [SerializeField] private int carsPerDirection = 12;
    [SerializeField] private float roadCenterZ = 0.7f;
    [SerializeField] private float laneOffset = 0.9f;
    [SerializeField] private float roadY = 0.28f;
    [SerializeField] private float roadHalfLength = 105f;
    [SerializeField] private Vector2 speedRange = new Vector2(23f, 31f);
    [SerializeField] private float minimumCarDistance = 7f;

    private readonly List<Car> cars = new List<Car>();
    private Material carMaterial;
    private Material lampMaterial;

    private struct Car
    {
        public Transform transform;
        public float speed;
        public int direction;
    }

private void Awake()
    {
        if (!Application.isPlaying) return;

        CreateMaterials();
        ClearCars(true);
        CreateTraffic();
        RegisterAndSpaceCars();
    }




    private void RegisterAndSpaceCars()
    {
        cars.Clear();

        List<Transform> rightLaneCars = new List<Transform>();
        List<Transform> leftLaneCars = new List<Transform>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform carTransform = transform.GetChild(i);
            if (!carTransform.name.StartsWith("Carro")) continue;

            int direction = carTransform.forward.x >= 0f ? 1 : -1;
            if (direction > 0)
                rightLaneCars.Add(carTransform);
            else
                leftLaneCars.Add(carTransform);
        }

        RegisterLane(rightLaneCars, 1, 0.5f);
        RegisterLane(leftLaneCars, -1, 0f);
    }

private void RegisterLane(List<Transform> laneCars, int direction, float phaseOffset)
    {
        if (laneCars.Count == 0) return;

        float boundary = roadHalfLength + 3f;
        float loopLength = boundary * 2f;
        float minimumGap = Mathf.Min(minimumCarDistance, loopLength / laneCars.Count * 0.75f);
        float spareDistance = loopLength - minimumGap * laneCars.Count;
        float[] weights = new float[laneCars.Count];
        float totalWeight = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = Random.Range(0.65f, 1.35f);
            totalWeight += weights[i];
        }

        float distanceAlongLoop = phaseOffset * minimumGap;
        for (int i = 0; i < laneCars.Count; i++)
        {
            Transform carTransform = laneCars[i];
            float x = -boundary + Mathf.Repeat(distanceAlongLoop, loopLength);

            carTransform.SetPositionAndRotation(
                new Vector3(x, roadY, roadCenterZ + direction * laneOffset),
                Quaternion.Euler(0f, direction > 0 ? 90f : -90f, 0f));
            carTransform.gameObject.SetActive(true);

            cars.Add(new Car
            {
                transform = carTransform,
                speed = Random.Range(speedRange.x, speedRange.y),
                direction = direction
            });

            distanceAlongLoop += minimumGap + spareDistance * (weights[i] / totalWeight);
        }
    }

    private void ClearCars(bool immediate)
    {
        cars.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (immediate)
                DestroyImmediate(child);
            else
                Destroy(child);
        }
    }

private void FixedUpdate()
    {
        if (!Application.isPlaying) return;

        float boundary = roadHalfLength + 3f;
        float loopLength = boundary * 2f;

        for (int i = 0; i < cars.Count; i++)
        {
            Car car = cars[i];
            Vector3 position = car.transform.position;
            position.x += car.direction * car.speed * Time.deltaTime;
            position.x = Mathf.Repeat(position.x + boundary, loopLength) - boundary;
            position.y = roadY;
            position.z = roadCenterZ + car.direction * laneOffset;
            car.transform.position = position;
        }

        KeepCarsSeparated(1, boundary, loopLength);
        KeepCarsSeparated(-1, boundary, loopLength);
    }

private void KeepCarsSeparated(int direction, float boundary, float loopLength)
    {
        List<Car> laneCars = cars.FindAll(car => car.direction == direction);
        laneCars.Sort((a, b) => (direction * a.transform.position.x).CompareTo(direction * b.transform.position.x));
        if (laneCars.Count < 2) return;

        for (int i = laneCars.Count - 2; i >= 0; i--)
        {
            float behind = direction * laneCars[i].transform.position.x;
            float ahead = direction * laneCars[i + 1].transform.position.x;
            if (ahead - behind < minimumCarDistance)
                laneCars[i].transform.position = new Vector3(direction * (ahead - minimumCarDistance), roadY, laneCars[i].transform.position.z);
        }

        Car last = laneCars[laneCars.Count - 1];
        Car first = laneCars[0];
        float wrapGap = direction * first.transform.position.x + loopLength - direction * last.transform.position.x;
        if (wrapGap < minimumCarDistance)
            last.transform.position = new Vector3(direction * (direction * first.transform.position.x + loopLength - minimumCarDistance), roadY, last.transform.position.z);

        for (int i = 0; i < laneCars.Count; i++)
        {
            Vector3 position = laneCars[i].transform.position;
            position.x = Mathf.Repeat(position.x + boundary, loopLength) - boundary;
            laneCars[i].transform.position = position;
        }
    }


    private void CreateMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        carMaterial = new Material(litShader);
        carMaterial.color = new Color(0.06f, 0.16f, 0.24f);

        lampMaterial = new Material(litShader);
        lampMaterial.color = new Color(1f, 0.9f, 0.55f);
        if (lampMaterial.HasProperty("_EmissionColor"))
        {
            lampMaterial.EnableKeyword("_EMISSION");
            lampMaterial.SetColor("_EmissionColor", new Color(1f, 0.62f, 0.1f) * 12f);
        }
    }

private void CreateTraffic()
    {
        for (int i = 0; i < carsPerDirection; i++)
        {
            CreateCar(1, i, Color.HSVToRGB(Random.value, 0.65f, 0.85f));
            CreateCar(-1, i, Color.HSVToRGB(Random.value, 0.65f, 0.85f));
        }
    }

private void CreateCar(int direction, int index, Color color)
    {
        GameObject carObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        carObject.name = direction > 0 ? "Carro →" : "Carro ←";
        carObject.transform.SetParent(transform);

        float spacing = (roadHalfLength * 2f) / carsPerDirection;
        float startX = direction > 0 ? -roadHalfLength + index * spacing : roadHalfLength - index * spacing;
        float laneZ = roadCenterZ + (direction > 0 ? laneOffset : -laneOffset);

        carObject.transform.SetPositionAndRotation(
            new Vector3(startX, roadY, laneZ),
            Quaternion.Euler(0f, direction > 0 ? 90f : -90f, 0f));
        carObject.transform.localScale = new Vector3(2.3f, 0.85f, 1.25f);

        Renderer renderer = carObject.GetComponent<Renderer>();
        Material bodyMaterial = new Material(carMaterial) { color = color };
        renderer.material = bodyMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        Collider bodyCollider = carObject.GetComponent<Collider>();
        bodyCollider.isTrigger = true;

        Rigidbody body = carObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        carObject.AddComponent<CarPlayerCollision>();

        CreateHeadlight(carObject.transform, -0.52f);
        CreateHeadlight(carObject.transform, 0.52f);
    }

    private void CreateHeadlight(Transform car, float lateralOffset)
    {
        GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lamp.name = "Farol";
        lamp.transform.SetParent(car, false);
        lamp.transform.localPosition = new Vector3(lateralOffset, 0.05f, 0.66f);
        lamp.transform.localScale = new Vector3(0.24f, 0.2f, 0.08f);
        lamp.GetComponent<Renderer>().material = lampMaterial;

        Collider collider = lamp.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        GameObject lightObject = new GameObject("Luz do farol");
        lightObject.transform.SetParent(car, false);
        lightObject.transform.localPosition = new Vector3(lateralOffset, 0.05f, 0.72f);
        lightObject.transform.localRotation = Quaternion.identity;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(1f, 0.84f, 0.48f);
        light.intensity = 18f;
        light.range = 17f;
        light.spotAngle = 92f;
        light.innerSpotAngle = 58f;
        light.shadows = LightShadows.None;

        GameObject haloObject = new GameObject("Halo do farol");
        haloObject.transform.SetParent(car, false);
        haloObject.transform.localPosition = new Vector3(lateralOffset, 0.05f, 0.72f);

        Light halo = haloObject.AddComponent<Light>();
        halo.type = LightType.Point;
        halo.color = new Color(1f, 0.5f, 0.12f);
        halo.intensity = 2.5f;
        halo.range = 5f;
        halo.shadows = LightShadows.None;
    }
}
