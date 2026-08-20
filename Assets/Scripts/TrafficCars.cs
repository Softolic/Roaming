using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TrafficCars : MonoBehaviour
{
    [Header("Traffic")]
    [SerializeField] private int carsPerDirection = 4;
    [SerializeField] private float roadCenterZ = 0.7f;
    [SerializeField] private float laneOffset = 0.9f;
    [SerializeField] private float roadY = 0.28f;
    [SerializeField] private float roadHalfLength = 105f;
    [SerializeField] private Vector2 speedRange = new Vector2(11f, 18f);

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
        if (transform.childCount == 0)
            CreateTraffic();

        RegisterStoredCars();
    }

    private void RegisterStoredCars()
    {
        cars.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform car = transform.GetChild(i);
            if (!car.name.StartsWith("Carro")) continue;

            cars.Add(new Car
            {
                transform = car,
                speed = Random.Range(speedRange.x, speedRange.y),
                direction = car.forward.x >= 0f ? 1 : -1
            });
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

    private void Update()
    {
        if (!Application.isPlaying) return;

        for (int i = 0; i < cars.Count; i++)
        {
            Car car = cars[i];
            car.transform.position += car.transform.forward * car.speed * Time.deltaTime;

            if (Mathf.Abs(car.transform.position.x) > roadHalfLength)
            {
                Vector3 position = car.transform.position;
                position.x = -car.direction * roadHalfLength;
                car.transform.position = position;
            }
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
            lampMaterial.SetColor("_EmissionColor", new Color(1f, 0.62f, 0.1f) * 12f);;
        }
    }

    private void CreateTraffic()
    {
        for (int i = 0; i < carsPerDirection; i++)
        {
            CreateCar(1, i, new Color(0.08f, 0.32f, 0.48f));
            CreateCar(-1, i, new Color(0.32f, 0.08f, 0.1f));
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
        if (bodyCollider != null) Destroy(bodyCollider);

        CreateHeadlight(carObject.transform, -0.52f);
        CreateHeadlight(carObject.transform, 0.52f);

        cars.Add(new Car
        {
            transform = carObject.transform,
            speed = Random.Range(speedRange.x, speedRange.y),
            direction = direction
        });
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
        // Feixe amplo para recortar o asfalto, a chuva e as bordas da estrada.
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