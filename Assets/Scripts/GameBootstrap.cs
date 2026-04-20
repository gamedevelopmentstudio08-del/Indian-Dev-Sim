using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameBootstrap : MonoBehaviour
{
    private const float CityRoadSurfaceY = 0.04f;
    private const float CityRoadWidth = 20f;
    private const float CityRoadEdgeOffset = 9.65f;
    private const float CityLaneOffset = 4.2f;
    private const float MainRoadTreeOffset = 15.8f;
    private const float CrossRoadTreeOffset = 15.8f;
    private const float BranchRoadTreeOffset = 15.8f;
    private const float ShortRoadTreeOffset = 13.2f;
    private const float TrafficLightRoadsidePadding = 2.2f;
    private const float TrafficLightCornerPadding = 1.6f;
    private const float MountainLaneOffset = 2f;
    private const float TransitionStartZ = 500f;
    private const float TransitionEndZ = 620f;
    private const float MaxFlatRoadSlopeAngle = 5f;
    private const float MaxClimbRoadSlopeAngle = 25f;
    private const float FlatRoadHeightTolerance = 0.35f;
    private const float FlatRoadSampleOffset = 5f;
    private const float MountainRoadSpacing = 12f;
    private const float MountainRoadBankLimit = 7f;
    private const float MountainRoadRerouteStep = 14f;
    private const float MountainRoadRerouteRange = 56f;
    private const float MountainRoadCrossSlopeLimit = 4.8f;
    private const float MountainRoadGuardRailHeight = 0.6f;
    private const float MountainRoadEdgeDropThreshold = 2.2f;
    private const float ConnectorBridgeDeckThickness = 0.08f;
    private const float ConnectorBridgeRailHeight = 0.9f;
    private const float ConnectorBridgeSupportSpacing = 42f;
    private const float ConnectorBridgeSupportWidth = 1.25f;
    private const float ConnectorBridgeDebugSpacing = 24f;
    private const float HillEntryFlattenRadius = 18f;
    private const float FlatEntrySlopeTolerance = 1f;

    private enum WeatherTimePreset
    {
        DayClear,
        DayCloudy,
        DayRain,
        NightClear,
        NightRain
    }

    private SimpleBusController bus;
    private Text speedText;
    private Image hudPanel;
    private Light sunLight;
    private ParticleSystem rainSystem;
    private float nextWeatherChangeTime;
    private Terrain mountainTerrain;
    private Vector3 cityBusStartPosition = new Vector3(0f, 1.2f, -430f);
    private Quaternion cityBusStartRotation = Quaternion.identity;
    private string weatherLabel = "Day / Clear";
    private string hudMessage = "Speed: 0 km/h\nGear: N\nWeather: Day / Clear";

    [Header("Weather Cycle")]
    public float weatherChangeIntervalSeconds = 7200f;

    private void Start()
    {
        BuildScene();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadGame();
            return;
        }

        UpdateWeatherCycle();

        if (bus == null || speedText == null)
        {
            return;
        }

        Rigidbody rb = bus.GetComponent<Rigidbody>();
        int speedKmh = Mathf.RoundToInt(rb.velocity.magnitude * 3.6f);
        hudMessage =
            "Speed: " + speedKmh + " km/h\n" +
            "Gear: " + bus.gearText + "\n" +
            "Weather: " + weatherLabel + "\n" +
            "W/S Drive  A/D Turn  Space Brake  R Reload";

        speedText.text = hudMessage;
    }

    private void ReloadGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    private void BuildScene()
    {
        CreateLight();
        CreateRoad();
        CreateSparseBuildings();
        CreateTrafficLights();
        CreateBus();
        CreateMovingDemoVehicle();
        SetupCamera();
        CreateWeatherSystem();
        CreateHud();
    }

    private void CreateLight()
    {
        sunLight = FindObjectOfType<Light>();
        if (sunLight != null)
        {
            return;
        }

        GameObject lightObject = new GameObject("Directional Light");
        sunLight = lightObject.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private void CreateWeatherSystem()
    {
        Camera cam = Camera.main;
        GameObject rainObject = new GameObject("Rain Weather");
        if (cam != null)
        {
            rainObject.transform.SetParent(cam.transform, false);
            rainObject.transform.localPosition = new Vector3(0f, 16f, 12f);
        }
        else
        {
            rainObject.transform.position = new Vector3(0f, 20f, 0f);
        }

        rainSystem = rainObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = rainSystem.main;
        main.loop = true;
        main.startLifetime = 1.2f;
        main.startSpeed = 28f;
        main.startSize = 0.045f;
        main.maxParticles = 1800;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = rainSystem.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = rainSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(70f, 1f, 70f);

        ParticleSystem.VelocityOverLifetimeModule velocity = rainSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(-26f);

        ParticleSystemRenderer renderer = rainSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.4f;
        renderer.velocityScale = 0.12f;

        ApplyWeatherPreset(WeatherTimePreset.DayClear);
        nextWeatherChangeTime = Time.time + weatherChangeIntervalSeconds;
    }

    private void UpdateWeatherCycle()
    {
        if (weatherChangeIntervalSeconds <= 0f || Time.time < nextWeatherChangeTime)
        {
            return;
        }

        ApplyWeatherPreset(PickWeightedWeatherPreset());
        nextWeatherChangeTime = Time.time + weatherChangeIntervalSeconds;
    }

    private WeatherTimePreset PickWeightedWeatherPreset()
    {
        WeatherTimePreset[] weightedPresets =
        {
            WeatherTimePreset.DayClear,
            WeatherTimePreset.DayClear,
            WeatherTimePreset.DayClear,
            WeatherTimePreset.DayCloudy,
            WeatherTimePreset.DayCloudy,
            WeatherTimePreset.DayRain,
            WeatherTimePreset.NightClear,
            WeatherTimePreset.NightRain
        };

        return weightedPresets[Random.Range(0, weightedPresets.Length)];
    }

    private void ApplyWeatherPreset(WeatherTimePreset preset)
    {
        bool rainEnabled = preset == WeatherTimePreset.DayRain || preset == WeatherTimePreset.NightRain;
        if (rainSystem != null)
        {
            ParticleSystem.EmissionModule emission = rainSystem.emission;
            emission.rateOverTime = rainEnabled ? 650f : 0f;
        }

        switch (preset)
        {
            case WeatherTimePreset.DayClear:
                ApplyLightAndSky("Day / Clear", 1f, new Color(0.64f, 0.78f, 0.95f), new Color(0.44f, 0.52f, 0.58f), 0.0008f, new Vector3(50f, -30f, 0f));
                break;
            case WeatherTimePreset.DayCloudy:
                ApplyLightAndSky("Day / Cloudy", 0.62f, new Color(0.50f, 0.56f, 0.60f), new Color(0.34f, 0.36f, 0.38f), 0.0020f, new Vector3(42f, -35f, 0f));
                break;
            case WeatherTimePreset.DayRain:
                ApplyLightAndSky("Day / Rain", 0.46f, new Color(0.38f, 0.43f, 0.47f), new Color(0.26f, 0.28f, 0.30f), 0.0032f, new Vector3(38f, -35f, 0f));
                break;
            case WeatherTimePreset.NightClear:
                ApplyLightAndSky("Night / Clear", 0.16f, new Color(0.06f, 0.08f, 0.14f), new Color(0.06f, 0.07f, 0.10f), 0.0014f, new Vector3(12f, -45f, 0f));
                break;
            case WeatherTimePreset.NightRain:
                ApplyLightAndSky("Night / Rain", 0.11f, new Color(0.04f, 0.05f, 0.08f), new Color(0.05f, 0.06f, 0.08f), 0.0040f, new Vector3(10f, -45f, 0f));
                break;
        }
    }

    private void ApplyLightAndSky(string label, float lightIntensity, Color skyColor, Color fogColor, float fogDensity, Vector3 lightRotation)
    {
        weatherLabel = label;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = skyColor;
        RenderSettings.ambientEquatorColor = Color.Lerp(skyColor, Color.gray, 0.45f);
        RenderSettings.ambientGroundColor = Color.Lerp(fogColor, Color.black, 0.45f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        if (sunLight != null)
        {
            sunLight.intensity = lightIntensity;
            sunLight.color = Color.Lerp(Color.white, skyColor, 0.25f);
            sunLight.transform.rotation = Quaternion.Euler(lightRotation);
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = skyColor;
        }
    }

    private void CreateRoad()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(220f, 1f, 220f);
        ground.GetComponent<Renderer>().material.color = new Color(0.56f, 0.78f, 0.42f);

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Main Road";
        road.transform.position = new Vector3(0f, CityRoadSurfaceY, 0f);
        road.transform.localScale = new Vector3(CityRoadWidth, 0.08f, 1000f);
        road.GetComponent<Renderer>().material.color = new Color(0.12f, 0.12f, 0.13f);
        ConfigureDriveableRoad(road);

        GameObject crossRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossRoad.name = "Second Road";
        crossRoad.transform.position = new Vector3(0f, 0.04f, 25f);
        crossRoad.transform.localScale = new Vector3(1000f, 0.08f, CityRoadWidth);
        crossRoad.GetComponent<Renderer>().material.color = new Color(0.12f, 0.12f, 0.13f);
        ConfigureDriveableRoad(crossRoad);

        CreateCutRoad(-300f);
        CreateCutRoad(0f);
        CreateCutRoad(300f);
        CreateBranchRoad(250f, -300f);
        CreateBranchRoad(-250f, 0f);
        CreateBranchRoad(250f, 300f);

        CreateSideRoad("Left Cut Road", new Vector3(-58f, 0.045f, -250f), new Vector3(110f, 0.08f, CityRoadWidth));
        CreateSideRoad("Right Cut Road", new Vector3(58f, 0.045f, -70f), new Vector3(110f, 0.08f, CityRoadWidth));
        CreateSideRoad("Left Cut Road 2", new Vector3(-58f, 0.045f, 260f), new Vector3(110f, 0.08f, CityRoadWidth));

        for (int i = -100; i <= 100; i++)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "Center Line";
            line.transform.position = new Vector3(0f, 0.09f, i * 5f);
            line.transform.localScale = new Vector3(0.18f, 0.04f, 2.4f);
            line.GetComponent<Renderer>().material.color = Color.yellow;
            RemoveCollider(line);
        }

        for (int i = -100; i <= 100; i++)
        {
            GameObject sideLineLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideLineLeft.name = "Left Road Line";
            sideLineLeft.transform.position = new Vector3(-CityRoadEdgeOffset, 0.1f, i * 5f);
            sideLineLeft.transform.localScale = new Vector3(0.12f, 0.04f, 2.4f);
            sideLineLeft.GetComponent<Renderer>().material.color = Color.white;
            RemoveCollider(sideLineLeft);

            GameObject sideLineRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideLineRight.name = "Right Road Line";
            sideLineRight.transform.position = new Vector3(CityRoadEdgeOffset, 0.1f, i * 5f);
            sideLineRight.transform.localScale = new Vector3(0.12f, 0.04f, 2.4f);
            sideLineRight.GetComponent<Renderer>().material.color = Color.white;
            RemoveCollider(sideLineRight);
        }

        for (int i = -100; i <= 100; i++)
        {
            GameObject crossLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossLine.name = "Second Road Center Line";
            crossLine.transform.position = new Vector3(i * 5f, 0.11f, 25f);
            crossLine.transform.localScale = new Vector3(2.4f, 0.04f, 0.18f);
            crossLine.GetComponent<Renderer>().material.color = Color.yellow;
            RemoveCollider(crossLine);

            GameObject crossSideLineA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossSideLineA.name = "Second Road Side Line";
            crossSideLineA.transform.position = new Vector3(i * 5f, 0.12f, 25f - CityRoadEdgeOffset);
            crossSideLineA.transform.localScale = new Vector3(2.4f, 0.04f, 0.12f);
            crossSideLineA.GetComponent<Renderer>().material.color = Color.white;
            RemoveCollider(crossSideLineA);

            GameObject crossSideLineB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossSideLineB.name = "Second Road Side Line";
            crossSideLineB.transform.position = new Vector3(i * 5f, 0.12f, 25f + CityRoadEdgeOffset);
            crossSideLineB.transform.localScale = new Vector3(2.4f, 0.04f, 0.12f);
            crossSideLineB.GetComponent<Renderer>().material.color = Color.white;
            RemoveCollider(crossSideLineB);
        }

        CreateSideRoadLines(-250f, -1);
        CreateSideRoadLines(-70f, 1);
        CreateSideRoadLines(260f, -1);
        CreateRoadsideTrees();
        CreateMountainRoute();
    }

    private void CreateRoadsideTrees()
    {
        Random.InitState(4307);

        for (float z = -455f; z <= 455f; z += 34f)
        {
            CreateTreeIfClear(new Vector3(-MainRoadTreeOffset, 0f, z), 0.95f);
            CreateTreeIfClear(new Vector3(MainRoadTreeOffset, 0f, z + 11f), 0.9f);
        }

        float[] horizontalRoads = { -300f, 0f, 25f, 300f };
        foreach (float roadZ in horizontalRoads)
        {
            for (float x = -455f; x <= 455f; x += 40f)
            {
                CreateTreeIfClear(new Vector3(x, 0f, roadZ - CrossRoadTreeOffset), 0.85f);
                CreateTreeIfClear(new Vector3(x + 16f, 0f, roadZ + CrossRoadTreeOffset), 0.85f);
            }
        }

        CreateRoadsideTreesForBranch(250f, -300f);
        CreateRoadsideTreesForBranch(-250f, 0f);
        CreateRoadsideTreesForBranch(250f, 300f);

        CreateRoadsideTreesForShortRoad(-58f, -250f, -105f, -8f);
        CreateRoadsideTreesForShortRoad(58f, -70f, 8f, 105f);
        CreateRoadsideTreesForShortRoad(-58f, 260f, -105f, -8f);
    }

    private void CreateRoadsideTreesForBranch(float xCenter, float zCenter)
    {
        for (float z = zCenter - 455f; z <= zCenter + 455f; z += 40f)
        {
            CreateTreeIfClear(new Vector3(xCenter - BranchRoadTreeOffset, 0f, z), 0.8f);
            CreateTreeIfClear(new Vector3(xCenter + BranchRoadTreeOffset, 0f, z + 14f), 0.8f);
        }
    }

    private void CreateRoadsideTreesForShortRoad(float xCenter, float zCenter, float minX, float maxX)
    {
        for (float x = minX; x <= maxX; x += 28f)
        {
            CreateTreeIfClear(new Vector3(x, 0f, zCenter - ShortRoadTreeOffset), 0.75f);
            CreateTreeIfClear(new Vector3(x + 12f, 0f, zCenter + ShortRoadTreeOffset), 0.75f);
        }
    }

    private void CreateTreeIfClear(Vector3 position, float scale)
    {
        if (IsBuildingOnRoad(position.x, position.z, 2.4f * scale, 2.4f * scale, 1.5f))
        {
            return;
        }

        CreateTree(position, scale * Random.Range(0.88f, 1.18f));
    }

    private void CreateTree(Vector3 position, float scale)
    {
        GameObject root = new GameObject("Roadside Tree");
        root.transform.position = position;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Tree Trunk";
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 1.15f * scale, 0f);
        trunk.transform.localScale = new Vector3(0.22f * scale, 1.15f * scale, 0.22f * scale);
        trunk.GetComponent<Renderer>().material.color = new Color(0.36f, 0.22f, 0.11f);
        RemoveCollider(trunk);

        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        top.name = "Tree Top";
        top.transform.SetParent(root.transform, false);
        top.transform.localPosition = new Vector3(0f, 2.65f * scale, 0f);
        top.transform.localScale = new Vector3(1.65f * scale, 1.35f * scale, 1.65f * scale);
        top.GetComponent<Renderer>().material.color = new Color(0.13f, 0.48f, 0.18f);
        RemoveCollider(top);
    }

    private void CreateMountainRoute()
    {
        Random.InitState(8124);

        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = 257;
        terrainData.size = new Vector3(1200f, 260f, 1400f);
        TerrainLayer mountainLayer = new TerrainLayer();
        mountainLayer.diffuseTexture = CreateSolidTexture(new Color(0.42f, 0.58f, 0.36f));
        mountainLayer.tileSize = new Vector2(18f, 18f);
        terrainData.terrainLayers = new TerrainLayer[] { mountainLayer };

        float[,] heights = GenerateMountainHeights(terrainData.heightmapResolution);
        SmoothHeights(heights, 4);
        terrainData.SetHeights(0, 0, heights);

        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "Perlin Mountain Terrain";
        terrainObject.transform.position = new Vector3(-600f, 0f, 500f);
        mountainTerrain = terrainObject.GetComponent<Terrain>();
        mountainTerrain.drawInstanced = true;
        mountainTerrain.heightmapPixelError = 12f;
        terrainObject.GetComponent<TerrainCollider>().terrainData = terrainData;
        LowerMountainTerrainForBridge(new Vector3(0f, 0f, TransitionEndZ + 40f), CityRoadSurfaceY - 3.2f);

        RemoveSlopeRoadConnections();
        Vector3 hillEntryPoint = FindFlatHillEntryPoint(new Vector3(0f, 0f, TransitionEndZ + 40f));
        List<Vector3> bridgePath = ConnectCityRoadToHillBase(hillEntryPoint);
        CreateMountainEnvironmentDetails(bridgePath);
    }

    private List<Vector3> ConnectCityRoadToHillBase(Vector3 hillEntryPoint)
    {
        Vector3 cityRoadEnd = new Vector3(0f, CityRoadSurfaceY, 492f);
        FlattenTerrainPatch(hillEntryPoint, HillEntryFlattenRadius, CityRoadSurfaceY);
        Vector3 flatHillEntryPoint = new Vector3(hillEntryPoint.x, CityRoadSurfaceY, hillEntryPoint.z);
        List<Vector3> connectorPoints = BuildFlatBridgePoints(cityRoadEnd, flatHillEntryPoint, MountainRoadSpacing);
        CreateConnectorBridge(connectorPoints);
        HighlightBridgePath(connectorPoints);
        Debug.Log("Bridge created instead of slope road");

        Vector3 signLeft = Vector3.Lerp(cityRoadEnd, flatHillEntryPoint, 0.35f) + new Vector3(-6.5f, 0f, 0f);
        Vector3 signRight = Vector3.Lerp(cityRoadEnd, flatHillEntryPoint, 0.55f) + new Vector3(6.5f, 0f, 0f);
        CreateMountainSign(signLeft, "MOUNTAIN BRIDGE");
        CreateMountainSign(signRight, "HILL ENTRY");
        return connectorPoints;
    }

    private float[,] GenerateMountainHeights(int resolution)
    {
        float[,] heights = new float[resolution, resolution];
        float seedX = 37.3f;
        float seedZ = 91.7f;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = x / (float)(resolution - 1);
                float nz = z / (float)(resolution - 1);
                float ridge = Mathf.Pow(Mathf.Clamp01(nz), 1.18f);
                float large = Mathf.PerlinNoise(seedX + nx * 2.2f, seedZ + nz * 2.2f);
                float medium = Mathf.PerlinNoise(seedX + nx * 6.0f, seedZ + nz * 6.0f) * 0.45f;
                float fine = Mathf.PerlinNoise(seedX + nx * 15.0f, seedZ + nz * 15.0f) * 0.12f;
                float valley = Mathf.Abs(nx - 0.5f) * 0.22f;
                heights[z, x] = Mathf.Clamp01(0.06f + ridge * (large * 0.62f + medium + fine) + valley);
            }
        }

        return heights;
    }

    private void SmoothHeights(float[,] heights, int passes)
    {
        int rows = heights.GetLength(0);
        int cols = heights.GetLength(1);

        for (int pass = 0; pass < passes; pass++)
        {
            float[,] copy = (float[,])heights.Clone();
            for (int z = 1; z < rows - 1; z++)
            {
                for (int x = 1; x < cols - 1; x++)
                {
                    float sum = 0f;
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            sum += copy[z + dz, x + dx];
                        }
                    }
                    heights[z, x] = sum / 9f;
                }
            }
        }
    }

    private List<Vector3> BuildTerrainRoadPoints(Vector3[] waypoints, float spacing)
    {
        List<Vector3> points = new List<Vector3>();

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector3 start = waypoints[i];
            Vector3 end = waypoints[i + 1];
            Vector3 guideDirection = new Vector3(end.x - start.x, 0f, end.z - start.z).normalized;
            float length = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(end.x, end.z));
            int steps = Mathf.Max(2, Mathf.CeilToInt(length / spacing));

            for (int step = 0; step < steps; step++)
            {
                if (i > 0 && step == 0)
                {
                    continue;
                }

                float t = step / (float)steps;
                Vector3 candidate = Vector3.Lerp(start, end, t);
                if (!TryGetTerrainRoadPoint(candidate, guideDirection, out Vector3 roadPoint, out float slopeAngle))
                {
                    if (points.Count > 0 && candidate.z > TransitionEndZ)
                    {
                        return points;
                    }

                    continue;
                }

                if (points.Count > 0)
                {
                    float segmentLength = Vector3.Distance(points[points.Count - 1], roadPoint);
                    if (segmentLength < spacing * 0.45f)
                    {
                        points[points.Count - 1] = roadPoint;
                        continue;
                    }

                    if (slopeAngle > MaxFlatRoadSlopeAngle && i > 0 && points.Count > 2)
                    {
                        Vector3 previous = points[points.Count - 1];
                        roadPoint.x = Mathf.Lerp(previous.x, roadPoint.x, 0.92f);
                        roadPoint.z = Mathf.Lerp(previous.z, roadPoint.z, 0.92f);
                        roadPoint = ComputeRoadSurfacePoint(roadPoint);
                    }
                }

                points.Add(roadPoint);
            }
        }

        if (TryGetTerrainRoadPoint(
            waypoints[waypoints.Length - 1],
            new Vector3(
                waypoints[waypoints.Length - 1].x - waypoints[waypoints.Length - 2].x,
                0f,
                waypoints[waypoints.Length - 1].z - waypoints[waypoints.Length - 2].z).normalized,
            out Vector3 endPoint,
            out float endSlope))
        {
            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], endPoint) > spacing * 0.45f)
            {
                points.Add(endPoint);
            }
            else if (endSlope <= MaxClimbRoadSlopeAngle)
            {
                points[points.Count - 1] = endPoint;
            }
        }

        return points;
    }

    private List<Vector3> BuildFlatBridgePoints(Vector3 start, Vector3 end, float spacing)
    {
        List<Vector3> points = new List<Vector3>();
        Vector3 flatDirection = new Vector3(end.x - start.x, 0f, end.z - start.z);
        if (flatDirection.sqrMagnitude < 0.01f)
        {
            points.Add(start);
            points.Add(end);
            return points;
        }

        flatDirection.Normalize();
        int steps = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(start, end) / spacing));

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 candidate = Vector3.Lerp(start, end, t);
            Vector3 roadPoint = new Vector3(candidate.x, CityRoadSurfaceY, candidate.z);
            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], roadPoint) > spacing * 0.45f)
            {
                points.Add(roadPoint);
            }
        }

        if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], end) > 0.5f)
        {
            points.Add(end);
        }

        return points;
    }

    private Vector3 FindFlatHillEntryPoint(Vector3 preferredWorldPoint)
    {
        Vector3 bestPoint = preferredWorldPoint;
        float bestScore = float.MaxValue;

        for (float z = TransitionEndZ + 18f; z <= TransitionEndZ + 180f; z += 8f)
        {
            for (float x = -180f; x <= 180f; x += 8f)
            {
                Vector3 candidate = new Vector3(x, 0f, z);
                if (!TryGetTerrainNormalizedPosition(candidate, out _, out _))
                {
                    continue;
                }

                float slope = GetTerrainSlopeAngle(candidate);
                float height = GetTerrainHeight(candidate);
                float score =
                    Mathf.Abs(x) * 0.05f +
                    Mathf.Abs(z - preferredWorldPoint.z) * 0.08f +
                    slope * 2.5f +
                    Mathf.Abs(height - CityRoadSurfaceY) * 4f;

                if (slope <= FlatEntrySlopeTolerance)
                {
                    score -= 3f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPoint = new Vector3(candidate.x, CityRoadSurfaceY, candidate.z);
                }
            }
        }

        return bestPoint;
    }

    private void FlattenTerrainPatch(Vector3 center, float radius, float targetHeight)
    {
        if (mountainTerrain == null)
        {
            return;
        }

        TerrainData terrainData = mountainTerrain.terrainData;
        int resolution = terrainData.heightmapResolution;
        float[,] heights = terrainData.GetHeights(0, 0, resolution, resolution);
        Vector3 terrainPosition = mountainTerrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        float blendRadius = radius + 8f;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector3 world = new Vector3(
                    terrainPosition.x + (x / (float)(resolution - 1)) * terrainSize.x,
                    0f,
                    terrainPosition.z + (z / (float)(resolution - 1)) * terrainSize.z);

                float distance = Vector2.Distance(new Vector2(world.x, world.z), new Vector2(center.x, center.z));
                if (distance > blendRadius)
                {
                    continue;
                }

                float normalizedTargetHeight = Mathf.Clamp01((targetHeight - terrainPosition.y) / terrainSize.y);
                float blend = distance <= radius ? 1f : 1f - Mathf.InverseLerp(radius, blendRadius, distance);
                heights[z, x] = Mathf.Lerp(heights[z, x], normalizedTargetHeight, blend);
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    private void RemoveSlopeRoadConnections()
    {
        string[] slopeRoadNames =
        {
            "Mountain Climb Road",
            "City To Mountain Connector",
            "Mountain Road Center Line",
            "Mountain Road Edge Line",
            "Mountain Guard Rail",
            "City Bridge Rail",
            "City Bridge Support",
            "City Bridge Support Cap",
            "City Bridge Debug Marker",
            "City To Mountain Bridge"
        };

        GameObject[] sceneObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject sceneObject in sceneObjects)
        {
            if (sceneObject == null)
            {
                continue;
            }

            bool matchesKnownName = false;
            for (int i = 0; i < slopeRoadNames.Length; i++)
            {
                if (sceneObject.name == slopeRoadNames[i])
                {
                    matchesKnownName = true;
                    break;
                }
            }

            bool isSlopeRoad =
                sceneObject.name.Contains("Road") &&
                Vector3.Angle(sceneObject.transform.up, Vector3.up) > 0.1f;
            if (!matchesKnownName && !isSlopeRoad)
            {
                continue;
            }

            Destroy(sceneObject);
        }
    }

    private void HighlightBridgePath(List<Vector3> bridgePoints)
    {
        if (bridgePoints == null || bridgePoints.Count == 0)
        {
            return;
        }

        float travelled = 0f;
        for (int i = 0; i < bridgePoints.Count; i++)
        {
            if (i > 0)
            {
                travelled += Vector3.Distance(bridgePoints[i - 1], bridgePoints[i]);
            }

            if (i != 0 && i != bridgePoints.Count - 1 && travelled < ConnectorBridgeDebugSpacing)
            {
                continue;
            }

            travelled = 0f;
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "City Bridge Debug Marker";
            marker.transform.position = bridgePoints[i] + Vector3.up * 0.45f;
            marker.transform.localScale = Vector3.one * 1.2f;
            marker.GetComponent<Renderer>().material.color = new Color(0.1f, 0.9f, 1f);
            RemoveCollider(marker);
        }
    }

    private void LowerMountainTerrainForBridge(Vector3 samplePoint, float targetGroundHeight)
    {
        if (mountainTerrain == null)
        {
            return;
        }

        float currentHeight = mountainTerrain.SampleHeight(samplePoint) + mountainTerrain.transform.position.y;
        float delta = targetGroundHeight - currentHeight;
        mountainTerrain.transform.position += Vector3.up * delta;
    }

    private void CreateConnectorBridge(List<Vector3> connectorPoints)
    {
        if (connectorPoints == null || connectorPoints.Count < 2)
        {
            return;
        }

        Vector3 start = connectorPoints[0];
        Vector3 end = connectorPoints[connectorPoints.Count - 1];
        CreateBridgeDeckSegment(start, end, "City To Mountain Bridge");
        CreateBridgeRail(start, end, -CityRoadEdgeOffset);
        CreateBridgeRail(start, end, CityRoadEdgeOffset);

        float totalLength = Vector3.Distance(start, end);
        int supportCount = Mathf.Max(1, Mathf.FloorToInt(totalLength / ConnectorBridgeSupportSpacing));
        for (int supportIndex = 1; supportIndex <= supportCount; supportIndex++)
        {
            float t = supportIndex / (float)(supportCount + 1);
            CreateBridgeSupport(Vector3.Lerp(start, end, t), (end - start).normalized);
        }

        CreateBridgeStripes(connectorPoints);
    }

    private void CreateBridgeDeckSegment(Vector3 start, Vector3 end, string name)
    {
        if (!TryGetBridgeSegmentFrame(start, end, out Vector3 midpoint, out Quaternion rotation, out _, out float length))
        {
            return;
        }

        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = name;
        deck.transform.position = midpoint;
        deck.transform.rotation = rotation;
        deck.transform.localScale = new Vector3(CityRoadWidth, ConnectorBridgeDeckThickness, length + 0.8f);
        deck.GetComponent<Renderer>().material.color = new Color(0.08f, 0.08f, 0.09f);
        ConfigureDriveableRoad(deck);
    }

    private void CreateBridgeStripe(Vector3 start, Vector3 end)
    {
        if (!TryGetBridgeSegmentFrame(start, end, out Vector3 midpoint, out Quaternion rotation, out _, out float length))
        {
            return;
        }

        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = "City Bridge Center Line";
        stripe.transform.position = midpoint + Vector3.up * (ConnectorBridgeDeckThickness * 0.5f + 0.03f);
        stripe.transform.rotation = rotation;
        stripe.transform.localScale = new Vector3(0.18f, 0.04f, Mathf.Min(7f, length * 0.7f));
        stripe.GetComponent<Renderer>().material.color = Color.yellow;
        RemoveCollider(stripe);
    }

    private void CreateBridgeStripes(List<Vector3> bridgePoints)
    {
        for (int i = 0; i < bridgePoints.Count - 1; i++)
        {
            CreateBridgeStripe(bridgePoints[i], bridgePoints[i + 1]);
        }
    }

    private void CreateBridgeRail(Vector3 start, Vector3 end, float lateralOffset)
    {
        if (!TryGetBridgeSegmentFrame(start, end, out Vector3 midpoint, out Quaternion rotation, out Vector3 roadRight, out float length))
        {
            return;
        }

        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = "City Bridge Rail";
        rail.transform.position = midpoint + roadRight * lateralOffset + Vector3.up * ConnectorBridgeRailHeight;
        rail.transform.rotation = rotation;
        rail.transform.localScale = new Vector3(0.22f, 0.22f, length + 0.4f);
        rail.GetComponent<Renderer>().material.color = new Color(0.78f, 0.8f, 0.84f);
        RemoveCollider(rail);
    }

    private void CreateBridgeSupport(Vector3 bridgePoint, Vector3 forward)
    {
        float groundHeight = GetTerrainHeight(bridgePoint);
        float supportHeight = (bridgePoint.y - ConnectorBridgeDeckThickness * 0.5f) - groundHeight;
        if (supportHeight < 2.5f)
        {
            return;
        }

        Vector3 flatForward = new Vector3(forward.x, 0f, forward.z).normalized;
        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = Vector3.forward;
        }

        Vector3 roadRight = new Vector3(-flatForward.z, 0f, flatForward.x);
        Vector3 leftColumnPosition = new Vector3(bridgePoint.x, groundHeight + supportHeight * 0.5f, bridgePoint.z) - roadRight * 4.6f;
        Vector3 rightColumnPosition = new Vector3(bridgePoint.x, groundHeight + supportHeight * 0.5f, bridgePoint.z) + roadRight * 4.6f;

        CreateBridgeSupportColumn(leftColumnPosition, supportHeight);
        CreateBridgeSupportColumn(rightColumnPosition, supportHeight);

        GameObject capBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        capBeam.name = "City Bridge Support Cap";
        capBeam.transform.position = new Vector3(bridgePoint.x, bridgePoint.y - ConnectorBridgeDeckThickness * 0.5f, bridgePoint.z);
        capBeam.transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        capBeam.transform.localScale = new Vector3(CityRoadWidth - 3.4f, 0.42f, 1.6f);
        capBeam.GetComponent<Renderer>().material.color = new Color(0.42f, 0.46f, 0.52f);
        RemoveCollider(capBeam);
    }

    private void CreateBridgeSupportColumn(Vector3 position, float height)
    {
        GameObject support = GameObject.CreatePrimitive(PrimitiveType.Cube);
        support.name = "City Bridge Support";
        support.transform.position = position;
        support.transform.localScale = new Vector3(ConnectorBridgeSupportWidth, height, 1.4f);
        support.GetComponent<Renderer>().material.color = new Color(0.34f, 0.38f, 0.44f);
        RemoveCollider(support);
    }

    private bool TryGetBridgeSegmentFrame(Vector3 start, Vector3 end, out Vector3 midpoint, out Quaternion rotation, out Vector3 roadRight, out float length)
    {
        midpoint = (start + end) * 0.5f;
        rotation = Quaternion.identity;
        roadRight = Vector3.right;

        Vector3 direction = end - start;
        length = direction.magnitude;
        if (length < 0.5f)
        {
            return false;
        }

        rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        roadRight = rotation * Vector3.right;
        return true;
    }

    private Vector3 SampleTerrainPoint(Vector3 worldPoint)
    {
        if (mountainTerrain == null)
        {
            return worldPoint;
        }

        worldPoint.y = mountainTerrain.SampleHeight(worldPoint) + mountainTerrain.transform.position.y + 0.18f;
        return worldPoint;
    }

    private Vector3 ComputeRoadSurfacePoint(Vector3 worldPoint)
    {
        Vector3 snappedPoint = SnapPointToSurface(worldPoint);

        if (worldPoint.z <= TransitionStartZ)
        {
            worldPoint.y = CityRoadSurfaceY;
            return worldPoint;
        }

        if (worldPoint.z < TransitionEndZ)
        {
            float transitionT = Mathf.InverseLerp(TransitionStartZ, TransitionEndZ, worldPoint.z);
            worldPoint.y = Mathf.SmoothStep(CityRoadSurfaceY, snappedPoint.y, transitionT);
            return worldPoint;
        }

        worldPoint.y = snappedPoint.y;
        return worldPoint;
    }

    private bool TryGetTerrainRoadPoint(Vector3 worldPoint, Vector3 guideDirection, out Vector3 roadPoint, out float slopeAngle)
    {
        roadPoint = ComputeRoadSurfacePoint(worldPoint);
        slopeAngle = 0f;
        if (worldPoint.z <= TransitionStartZ)
        {
            return true;
        }

        if (mountainTerrain == null)
        {
            return false;
        }

        if (!TryResolveRoadPlacement(worldPoint, guideDirection, out roadPoint, out slopeAngle))
        {
            return false;
        }

        return true;
    }

    private bool TryResolveRoadPlacement(Vector3 worldPoint, Vector3 guideDirection, out Vector3 roadPoint, out float slopeAngle)
    {
        roadPoint = worldPoint;
        slopeAngle = 90f;

        Vector3 flatDirection = guideDirection.sqrMagnitude > 0.001f
            ? guideDirection.normalized
            : Vector3.forward;
        Vector3 side = new Vector3(-flatDirection.z, 0f, flatDirection.x);

        bool found = false;
        float bestCost = float.MaxValue;

        for (float lateral = 0f; lateral <= MountainRoadRerouteRange; lateral += MountainRoadRerouteStep)
        {
            for (int signIndex = 0; signIndex < 2; signIndex++)
            {
                float signedLateral = lateral * (signIndex == 0 ? 1f : -1f);
                if (Mathf.Approximately(lateral, 0f) && signIndex == 1)
                {
                    continue;
                }

                for (int forwardStep = -1; forwardStep <= 1; forwardStep++)
                {
                    Vector3 probe = worldPoint + side * signedLateral + flatDirection * (forwardStep * (MountainRoadRerouteStep * 0.65f));
                    if (!TrySampleTerrainRoadData(probe, out Vector3 probePoint, out float probeSlope))
                    {
                        continue;
                    }

                    if (probeSlope > MaxClimbRoadSlopeAngle)
                    {
                        continue;
                    }

                    float crossSlope = GetCrossRoadHeightDelta(probe, side, 4.5f);
                    if (crossSlope > MountainRoadCrossSlopeLimit)
                    {
                        continue;
                    }

                    float roughness = GetRoadRoughness(probe, flatDirection, side, 6f);
                    float cost =
                        Mathf.Abs(signedLateral) * 0.04f +
                        Mathf.Abs(forwardStep) * 0.2f +
                        Mathf.Max(0f, probeSlope - MaxFlatRoadSlopeAngle) * 0.3f +
                        crossSlope * 0.35f +
                        roughness * 0.45f;

                    if (probeSlope < MaxFlatRoadSlopeAngle)
                    {
                        cost -= 0.4f;
                    }

                    if (cost >= bestCost)
                    {
                        continue;
                    }

                    bestCost = cost;
                    roadPoint = ComputeRoadSurfacePoint(probePoint);
                    slopeAngle = probeSlope;
                    found = true;
                }
            }
        }

        return found;
    }

    private bool TrySampleTerrainRoadData(Vector3 worldPoint, out Vector3 snappedPoint, out float slopeAngle)
    {
        snappedPoint = worldPoint;
        slopeAngle = 90f;
        if (!TryGetTerrainNormalizedPosition(worldPoint, out float normalizedX, out float normalizedZ))
        {
            return false;
        }

        TerrainData terrainData = mountainTerrain.terrainData;
        Vector3 surfaceNormal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        slopeAngle = Vector3.Angle(surfaceNormal, Vector3.up);
        snappedPoint = SampleTerrainPoint(worldPoint);
        return true;
    }

    private bool TryGetTerrainNormalizedPosition(Vector3 worldPoint, out float normalizedX, out float normalizedZ)
    {
        normalizedX = 0f;
        normalizedZ = 0f;
        if (mountainTerrain == null)
        {
            return false;
        }

        TerrainData terrainData = mountainTerrain.terrainData;
        Vector3 terrainPosition = mountainTerrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldPoint.x);
        normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldPoint.z);
        return normalizedX > 0f && normalizedX < 1f && normalizedZ > 0f && normalizedZ < 1f;
    }

    private bool IsTerrainFlatForRoad(Vector3 worldPoint)
    {
        if (mountainTerrain == null)
        {
            return false;
        }

        if (!TryGetTerrainNormalizedPosition(worldPoint, out float normalizedX, out float normalizedZ))
        {
            return false;
        }

        TerrainData terrainData = mountainTerrain.terrainData;
        Vector3 terrainPosition = mountainTerrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        Vector3 centerNormal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        if (Vector3.Angle(centerNormal, Vector3.up) >= MaxFlatRoadSlopeAngle)
        {
            return false;
        }

        float centerHeight = mountainTerrain.SampleHeight(worldPoint) + mountainTerrain.transform.position.y;
        Vector3[] offsets =
        {
            new Vector3(FlatRoadSampleOffset, 0f, 0f),
            new Vector3(-FlatRoadSampleOffset, 0f, 0f),
            new Vector3(0f, 0f, FlatRoadSampleOffset),
            new Vector3(0f, 0f, -FlatRoadSampleOffset)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 samplePoint = worldPoint + offsets[i];
            float sampleX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, samplePoint.x);
            float sampleZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, samplePoint.z);
            if (sampleX <= 0f || sampleX >= 1f || sampleZ <= 0f || sampleZ >= 1f)
            {
                return false;
            }

            Vector3 sampleNormal = terrainData.GetInterpolatedNormal(sampleX, sampleZ);
            if (Vector3.Angle(sampleNormal, Vector3.up) >= MaxFlatRoadSlopeAngle)
            {
                return false;
            }

            float sampleHeight = mountainTerrain.SampleHeight(samplePoint) + mountainTerrain.transform.position.y;
            if (Mathf.Abs(sampleHeight - centerHeight) > FlatRoadHeightTolerance)
            {
                return false;
            }
        }

        return true;
    }

    private Vector3 SnapPointToSurface(Vector3 worldPoint)
    {
        RaycastHit hit;
        if (Physics.Raycast(worldPoint + Vector3.up * 250f, Vector3.down, out hit, 500f))
        {
            worldPoint.y = hit.point.y + 0.18f;
            return worldPoint;
        }

        return SampleTerrainPoint(worldPoint);
    }

    private float GetTerrainHeight(Vector3 worldPoint)
    {
        if (mountainTerrain == null)
        {
            return worldPoint.y;
        }

        return mountainTerrain.SampleHeight(worldPoint) + mountainTerrain.transform.position.y;
    }

    private float GetTerrainSlopeAngle(Vector3 worldPoint)
    {
        if (!TryGetTerrainNormalizedPosition(worldPoint, out float normalizedX, out float normalizedZ))
        {
            return 0f;
        }

        return Vector3.Angle(mountainTerrain.terrainData.GetInterpolatedNormal(normalizedX, normalizedZ), Vector3.up);
    }

    private Vector3 GetTerrainSurfaceNormal(Vector3 worldPoint)
    {
        if (!TryGetTerrainNormalizedPosition(worldPoint, out float normalizedX, out float normalizedZ))
        {
            return Vector3.up;
        }

        return mountainTerrain.terrainData.GetInterpolatedNormal(normalizedX, normalizedZ).normalized;
    }

    private float GetCrossRoadHeightDelta(Vector3 worldPoint, Vector3 side, float sampleOffset)
    {
        Vector3 leftSample = worldPoint - side * sampleOffset;
        Vector3 rightSample = worldPoint + side * sampleOffset;
        return Mathf.Abs(GetTerrainHeight(leftSample) - GetTerrainHeight(rightSample));
    }

    private float GetRoadRoughness(Vector3 worldPoint, Vector3 forward, Vector3 side, float sampleOffset)
    {
        float centerHeight = GetTerrainHeight(worldPoint);
        float forwardHeight = GetTerrainHeight(worldPoint + forward * sampleOffset);
        float backwardHeight = GetTerrainHeight(worldPoint - forward * sampleOffset);
        float leftHeight = GetTerrainHeight(worldPoint - side * sampleOffset);
        float rightHeight = GetTerrainHeight(worldPoint + side * sampleOffset);

        return
            Mathf.Abs(centerHeight - forwardHeight) +
            Mathf.Abs(centerHeight - backwardHeight) +
            Mathf.Abs(leftHeight - rightHeight) * 0.5f;
    }

    private void CarveRoadIntoTerrain(TerrainData terrainData, List<Vector3> roadPoints, float roadHalfWidth, float shoulderWidth)
    {
        int resolution = terrainData.heightmapResolution;
        float[,] heights = terrainData.GetHeights(0, 0, resolution, resolution);
        Vector3 terrainPosition = mountainTerrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector3 world = new Vector3(
                    terrainPosition.x + (x / (float)(resolution - 1)) * terrainSize.x,
                    0f,
                    terrainPosition.z + (z / (float)(resolution - 1)) * terrainSize.z
                );

                float roadHeight;
                float distance = DistanceToRoad(world, roadPoints, out roadHeight);
                if (distance > shoulderWidth)
                {
                    continue;
                }

                float target = Mathf.Clamp01((roadHeight - terrainPosition.y) / terrainSize.y);
                float blend = distance <= roadHalfWidth ? 1f : 1f - Mathf.InverseLerp(roadHalfWidth, shoulderWidth, distance);
                heights[z, x] = Mathf.Lerp(heights[z, x], target, blend);
            }
        }

        SmoothHeights(heights, 1);
        terrainData.SetHeights(0, 0, heights);
    }

    private void CreateMountainRoadVisuals(List<Vector3> roadPoints, string roadName, bool addGuardRails)
    {
        if (roadPoints == null || roadPoints.Count < 2)
        {
            return;
        }

        for (int i = 0; i < roadPoints.Count - 1; i++)
        {
            Vector3 start = roadPoints[i];
            Vector3 end = roadPoints[i + 1];
            if (Vector3.Distance(start, end) < 1f)
            {
                continue;
            }

            Vector3 previous = i > 0 ? roadPoints[i - 1] : start - (end - start);
            Vector3 next = i < roadPoints.Count - 2 ? roadPoints[i + 2] : end + (end - start);

            CreateMountainRoadSegment(start, end, CityRoadWidth, roadName, previous, next);
            CreateMountainRoadEdgeLine(start, end, -CityRoadEdgeOffset, previous, next);
            CreateMountainRoadEdgeLine(start, end, CityRoadEdgeOffset, previous, next);
            CreateMountainRoadStripe(start, end, previous, next);

            if (addGuardRails)
            {
                CreateGuardRailsForSegment(start, end, previous, next);
            }
        }
    }

    private float DistanceToRoad(Vector3 point, List<Vector3> roadPoints, out float roadHeight)
    {
        float bestDistance = float.MaxValue;
        roadHeight = 0f;

        for (int i = 0; i < roadPoints.Count - 1; i++)
        {
            Vector2 a = new Vector2(roadPoints[i].x, roadPoints[i].z);
            Vector2 b = new Vector2(roadPoints[i + 1].x, roadPoints[i + 1].z);
            Vector2 p = new Vector2(point.x, point.z);
            Vector2 segment = b - a;
            float t = segment.sqrMagnitude > 0.01f ? Mathf.Clamp01(Vector2.Dot(p - a, segment) / segment.sqrMagnitude) : 0f;
            Vector2 closest = a + segment * t;
            float distance = Vector2.Distance(p, closest);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                roadHeight = Mathf.Lerp(roadPoints[i].y, roadPoints[i + 1].y, t);
            }
        }

        return bestDistance;
    }

    private Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(2, 2);
        texture.SetPixel(0, 0, color);
        texture.SetPixel(1, 0, color);
        texture.SetPixel(0, 1, color);
        texture.SetPixel(1, 1, color);
        texture.Apply();
        return texture;
    }

    private void CreateMountainRoadSegment(Vector3 start, Vector3 end, float width, string name, Vector3 previous, Vector3 next)
    {
        if (!TryGetRoadSegmentFrame(start, end, previous, next, out Vector3 midpoint, out Quaternion rotation, out Vector3 roadUp, out Vector3 roadRight, out float length))
        {
            return;
        }

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = name;
        road.transform.position = midpoint + roadUp * 0.06f;
        road.transform.rotation = rotation;
        road.transform.localScale = new Vector3(width, 0.12f, length + 0.8f);
        road.GetComponent<Renderer>().material.color = new Color(0.12f, 0.12f, 0.13f);
        ConfigureDriveableRoad(road);
    }

    private void CreateMountainRoadStripe(Vector3 start, Vector3 end)
    {
        CreateMountainRoadStripe(start, end, start - (end - start), end + (end - start));
    }

    private void CreateMountainRoadStripe(Vector3 start, Vector3 end, Vector3 previous, Vector3 next)
    {
        if (!TryGetRoadSegmentFrame(start, end, previous, next, out Vector3 midpoint, out Quaternion rotation, out Vector3 roadUp, out Vector3 roadRight, out float length))
        {
            return;
        }

        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = "Mountain Road Center Line";
        stripe.transform.position = midpoint + roadUp * 0.14f;
        stripe.transform.rotation = rotation;
        stripe.transform.localScale = new Vector3(0.18f, 0.04f, Mathf.Min(7f, length * 0.65f));
        stripe.GetComponent<Renderer>().material.color = Color.yellow;
        RemoveCollider(stripe);
    }

    private void CreateMountainRoadEdgeLine(Vector3 start, Vector3 end, float lateralOffset, Vector3 previous, Vector3 next)
    {
        if (!TryGetRoadSegmentFrame(start, end, previous, next, out Vector3 midpoint, out Quaternion rotation, out Vector3 roadUp, out Vector3 roadRight, out float length))
        {
            return;
        }

        GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        edge.name = "Mountain Road Edge Line";
        edge.transform.position = midpoint + roadRight * lateralOffset + roadUp * 0.11f;
        edge.transform.rotation = rotation;
        edge.transform.localScale = new Vector3(0.14f, 0.04f, length + 0.4f);
        edge.GetComponent<Renderer>().material.color = Color.white;
        RemoveCollider(edge);
    }

    private bool TryGetRoadSegmentFrame(Vector3 start, Vector3 end, Vector3 previous, Vector3 next, out Vector3 midpoint, out Quaternion rotation, out Vector3 roadUp, out Vector3 roadRight, out float length)
    {
        midpoint = (start + end) * 0.5f;
        rotation = Quaternion.identity;
        roadUp = Vector3.up;
        roadRight = Vector3.right;
        Vector3 direction = end - start;
        length = direction.magnitude;
        if (length < 0.5f)
        {
            return false;
        }

        roadUp = (GetTerrainSurfaceNormal(start) + GetTerrainSurfaceNormal(midpoint) + GetTerrainSurfaceNormal(end)).normalized;
        if (roadUp.sqrMagnitude < 0.001f)
        {
            roadUp = Vector3.up;
        }

        rotation = Quaternion.LookRotation(direction.normalized, roadUp);
        float bankAngle = GetRoadBankAngle(previous, start, end, next, roadUp);
        rotation *= Quaternion.AngleAxis(bankAngle, Vector3.forward);
        roadUp = rotation * Vector3.up;
        roadRight = rotation * Vector3.right;
        return true;
    }

    private float GetRoadBankAngle(Vector3 previous, Vector3 start, Vector3 end, Vector3 next, Vector3 upAxis)
    {
        Vector3 incoming = start - previous;
        Vector3 outgoing = next - end;
        incoming.y = 0f;
        outgoing.y = 0f;

        if (incoming.sqrMagnitude < 0.01f)
        {
            incoming = end - start;
            incoming.y = 0f;
        }

        if (outgoing.sqrMagnitude < 0.01f)
        {
            outgoing = end - start;
            outgoing.y = 0f;
        }

        float signedTurn = Vector3.SignedAngle(incoming.normalized, outgoing.normalized, upAxis);
        return Mathf.Clamp(-signedTurn * 0.18f, -MountainRoadBankLimit, MountainRoadBankLimit);
    }

    private void CreateGuardRailsForSegment(Vector3 start, Vector3 end, Vector3 previous, Vector3 next)
    {
        if (!TryGetRoadSegmentFrame(start, end, previous, next, out Vector3 midpoint, out Quaternion rotation, out Vector3 roadUp, out Vector3 roadRight, out float length))
        {
            return;
        }

        if (length < 6f)
        {
            return;
        }

        float outerOffset = CityRoadWidth * 0.5f + 4f;
        float innerOffset = CityRoadWidth * 0.5f - 1.5f;
        Vector3 leftInside = midpoint - roadRight * innerOffset;
        Vector3 leftOutside = midpoint - roadRight * outerOffset;
        Vector3 rightInside = midpoint + roadRight * innerOffset;
        Vector3 rightOutside = midpoint + roadRight * outerOffset;

        if (IsMountainEdgeRisk(leftInside, leftOutside))
        {
            CreateGuardRailSegment(midpoint, rotation, roadUp, roadRight, length, -CityRoadEdgeOffset);
        }

        if (IsMountainEdgeRisk(rightInside, rightOutside))
        {
            CreateGuardRailSegment(midpoint, rotation, roadUp, roadRight, length, CityRoadEdgeOffset);
        }
    }

    private bool IsMountainEdgeRisk(Vector3 innerSample, Vector3 outerSample)
    {
        if (outerSample.z <= TransitionEndZ || mountainTerrain == null)
        {
            return false;
        }

        if (!TryGetTerrainNormalizedPosition(outerSample, out _, out _))
        {
            return false;
        }

        float innerHeight = GetTerrainHeight(innerSample);
        float outerHeight = GetTerrainHeight(outerSample);
        float dropAmount = innerHeight - outerHeight;
        float outsideSlope = GetTerrainSlopeAngle(outerSample);
        return dropAmount > MountainRoadEdgeDropThreshold || outsideSlope > 19f;
    }

    private void CreateGuardRailSegment(Vector3 midpoint, Quaternion rotation, Vector3 roadUp, Vector3 roadRight, float length, float lateralOffset)
    {
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = "Mountain Guard Rail";
        beam.transform.position = midpoint + roadRight * lateralOffset + roadUp * MountainRoadGuardRailHeight;
        beam.transform.rotation = rotation;
        beam.transform.localScale = new Vector3(0.16f, 0.18f, length + 0.25f);
        beam.GetComponent<Renderer>().material.color = new Color(0.74f, 0.78f, 0.82f);
        RemoveCollider(beam);
    }

    private void CreateMountainRoadTrees(Vector3 start, Vector3 end)
    {
        Vector3 flatDirection = new Vector3(end.x - start.x, 0f, end.z - start.z).normalized;
        Vector3 side = new Vector3(-flatDirection.z, 0f, flatDirection.x);
        Vector3 center = (start + end) * 0.5f;

        CreateTree(SampleTerrainPoint(center + side * 16f), 0.82f);
        CreateTree(SampleTerrainPoint(center - side * 18f), 0.78f);
    }

    private void CreateMountainEnvironmentDetails(List<Vector3> roadPoints)
    {
        for (int i = 0; i < 90; i++)
        {
            Vector3 point = new Vector3(Random.Range(-520f, 520f), 0f, Random.Range(560f, 1860f));
            float roadHeight;
            if (DistanceToRoad(point, roadPoints, out roadHeight) < 22f)
            {
                continue;
            }

            point = SampleTerrainPoint(point);
            if (i % 3 == 0)
            {
                CreateRock(point, Random.Range(1.0f, 3.2f));
            }
            else
            {
                CreateTree(point, Random.Range(0.68f, 1.05f));
            }
        }
    }

    private void CreateMountainDemoVehicles(List<Vector3> roadPoints)
    {
        if (roadPoints == null || roadPoints.Count < 8)
        {
            return;
        }

        Color[] colors =
        {
            new Color(0.92f, 0.24f, 0.18f),
            new Color(0.14f, 0.48f, 0.92f),
            new Color(0.96f, 0.78f, 0.18f),
            new Color(0.18f, 0.72f, 0.46f)
        };

        CreateMountainDemoVehicle("Mountain Demo Vehicle 1", roadPoints, 2, 1, 8.5f, MountainLaneOffset, colors[0]);
        CreateMountainDemoVehicle("Mountain Demo Vehicle 2", roadPoints, roadPoints.Count / 2, -1, 7.8f, -MountainLaneOffset, colors[1]);
        CreateMountainDemoVehicle("Mountain Demo Vehicle 3", roadPoints, roadPoints.Count / 3, 1, 9.2f, MountainLaneOffset, colors[2]);
        CreateMountainDemoVehicle("Mountain Demo Vehicle 4", roadPoints, roadPoints.Count - 4, -1, 8.1f, -MountainLaneOffset, colors[3]);
    }

    private void CreateMountainDemoVehicle(string name, List<Vector3> roadPoints, int startIndex, int direction, float speed, float laneOffset, Color color)
    {
        GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicle.name = name;
        vehicle.transform.localScale = new Vector3(1.7f, 1f, 3.4f);
        vehicle.GetComponent<Renderer>().material.color = color;

        BoxCollider collider = vehicle.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        MountainDemoVehicleMover mover = vehicle.AddComponent<MountainDemoVehicleMover>();
        mover.speed = speed;
        mover.startIndex = Mathf.Clamp(startIndex, 0, roadPoints.Count - 1);
        mover.moveDirection = direction >= 0 ? 1 : -1;
        mover.laneOffset = laneOffset;
        mover.waypoints = roadPoints.ToArray();
    }

    private void CreateRock(Vector3 position, float scale)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Mountain Rock";
        rock.transform.position = position + Vector3.up * (0.28f * scale);
        rock.transform.localScale = new Vector3(1.25f * scale, 0.55f * scale, scale);
        rock.GetComponent<Renderer>().material.color = new Color(0.30f, 0.31f, 0.29f);
        RemoveCollider(rock);
    }

    private void CreateMountainSign(Vector3 position, string labelText)
    {
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = labelText + " Pole";
        pole.transform.position = new Vector3(position.x, position.y + 1.1f, position.z);
        pole.transform.localScale = new Vector3(0.08f, 1.1f, 0.08f);
        pole.GetComponent<Renderer>().material.color = Color.gray;
        RemoveCollider(pole);

        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = labelText + " Sign";
        board.transform.position = new Vector3(position.x, position.y + 2.35f, position.z);
        board.transform.localScale = new Vector3(3.4f, 0.9f, 0.1f);
        board.GetComponent<Renderer>().material.color = new Color(0.96f, 0.92f, 0.62f);
        RemoveCollider(board);

        GameObject label = new GameObject(labelText + " Text");
        label.transform.position = new Vector3(position.x, position.y + 2.36f, position.z - 0.08f);
        label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        TextMesh text = label.AddComponent<TextMesh>();
        text.text = labelText;
        text.fontSize = 32;
        text.characterSize = 0.16f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.black;
    }

    private void CreateSideRoad(string name, Vector3 position, Vector3 scale)
    {
        GameObject sideRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sideRoad.name = name;
        sideRoad.transform.position = position;
        sideRoad.transform.localScale = scale;
        sideRoad.GetComponent<Renderer>().material.color = new Color(0.12f, 0.12f, 0.13f);
        ConfigureDriveableRoad(sideRoad);
    }

    private void CreateSideRoadLines(float zPosition, int direction)
    {
        int start = direction < 0 ? -22 : 1;
        int end = direction < 0 ? -1 : 22;
        float shortRoadEdgeOffset = CityRoadWidth * 0.5f - 0.35f;

        for (int i = start; i <= end; i++)
        {
            GameObject centerLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine.name = "Cut Road Center Line";
            centerLine.transform.position = new Vector3(i * 5f, 0.13f, zPosition);
            centerLine.transform.localScale = new Vector3(2.3f, 0.04f, 0.16f);
            centerLine.GetComponent<Renderer>().material.color = Color.yellow;
            RemoveCollider(centerLine);

            GameObject sideA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideA.name = "Cut Road Side Line";
            sideA.transform.position = new Vector3(i * 5f, 0.13f, zPosition - shortRoadEdgeOffset);
            sideA.transform.localScale = new Vector3(2.3f, 0.04f, 0.1f);
            sideA.GetComponent<Renderer>().material.color = Color.white;
            RemoveCollider(sideA);

            GameObject sideB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideB.name = "Cut Road Side Line";
            sideB.transform.position = new Vector3(i * 5f, 0.13f, zPosition + shortRoadEdgeOffset);
            sideB.transform.localScale = new Vector3(2.3f, 0.04f, 0.1f);
            sideB.GetComponent<Renderer>().material.color = Color.white;
            RemoveCollider(sideB);
        }
    }

    private void CreateCutRoad(float zPosition)
    {
        GameObject cutRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cutRoad.name = "Cut Road";
        cutRoad.transform.position = new Vector3(0f, 0.045f, zPosition);
        cutRoad.transform.localScale = new Vector3(1000f, 0.07f, CityRoadWidth);
        cutRoad.GetComponent<Renderer>().material.color = new Color(0.12f, 0.12f, 0.13f);
        ConfigureDriveableRoad(cutRoad);

        for (int i = -100; i <= 100; i++)
        {
            GameObject centerLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine.name = "Cut Road Center Line";
            centerLine.transform.position = new Vector3(i * 5f, 0.12f, zPosition);
            centerLine.transform.localScale = new Vector3(2.2f, 0.035f, 0.16f);
            centerLine.GetComponent<Renderer>().material.color = Color.yellow;
            RemoveCollider(centerLine);

            GameObject sideLineA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideLineA.name = "Cut Road Side Line";
            sideLineA.transform.position = new Vector3(i * 5f, 0.13f, zPosition - CityRoadEdgeOffset);
            sideLineA.transform.localScale = new Vector3(2.2f, 0.035f, 0.1f);
            sideLineA.GetComponent<Renderer>().material.color = Color.white;
            RemoveCollider(sideLineA);

            GameObject sideLineB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideLineB.name = "Cut Road Side Line";
            sideLineB.transform.position = new Vector3(i * 5f, 0.13f, zPosition + CityRoadEdgeOffset);
            sideLineB.transform.localScale = new Vector3(2.2f, 0.035f, 0.1f);
            sideLineB.GetComponent<Renderer>().material.color = Color.white;
            RemoveCollider(sideLineB);
        }
    }

    private void CreateBranchRoad(float xPosition, float zCenter)
    {
        GameObject branchRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        branchRoad.name = "Branch Road";
        branchRoad.transform.position = new Vector3(xPosition, 0.05f, zCenter);
        branchRoad.transform.localScale = new Vector3(CityRoadWidth, 0.07f, 1000f);
        branchRoad.GetComponent<Renderer>().material.color = new Color(0.12f, 0.12f, 0.13f);
        ConfigureDriveableRoad(branchRoad);

        for (int i = -100; i <= 100; i++)
        {
            GameObject centerLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine.name = "Branch Road Center Line";
            centerLine.transform.position = new Vector3(xPosition, 0.13f, zCenter + i * 5f);
            centerLine.transform.localScale = new Vector3(0.16f, 0.035f, 2.2f);
            centerLine.GetComponent<Renderer>().material.color = Color.yellow;
            RemoveCollider(centerLine);

            GameObject sideLineA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideLineA.name = "Branch Road Side Line";
            sideLineA.transform.position = new Vector3(xPosition - CityRoadEdgeOffset, 0.14f, zCenter + i * 5f);
            sideLineA.transform.localScale = new Vector3(0.1f, 0.035f, 2.2f);
            sideLineA.GetComponent<Renderer>().material.color = Color.white;
            RemoveCollider(sideLineA);

            GameObject sideLineB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideLineB.name = "Branch Road Side Line";
            sideLineB.transform.position = new Vector3(xPosition + CityRoadEdgeOffset, 0.14f, zCenter + i * 5f);
            sideLineB.transform.localScale = new Vector3(0.1f, 0.035f, 2.2f);
            sideLineB.GetComponent<Renderer>().material.color = Color.white;
            RemoveCollider(sideLineB);
        }
    }

    private void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private void ConfigureDriveableRoad(GameObject road)
    {
        road.layer = LayerMask.NameToLayer("Default");
        BoxCollider collider = road.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = road.AddComponent<BoxCollider>();
        }

        collider.isTrigger = false;
    }

    private void CreateTrafficLights()
    {
        CreateTrafficLightSet(25f);
        CreateTrafficLightSet(-300f);
        CreateTrafficLightSet(0f);
        CreateTrafficLightSet(300f);

        float[] branchXs = { -250f, 250f };
        float[] roadZs = { -300f, 0f, 25f, 300f };

        foreach (float x in branchXs)
        {
            foreach (float z in roadZs)
            {
                CreateTrafficLightSetAt(x, z);
            }
        }
    }

    private void CreateTrafficLightSet(float zPosition)
    {
        CreateTrafficLightSetAt(0f, zPosition);
    }

    private void CreateTrafficLightSetAt(float xPosition, float zPosition)
    {
        float roadsideOffset = CityRoadWidth * 0.5f + TrafficLightRoadsidePadding;
        float cornerOffset = CityRoadWidth * 0.5f + TrafficLightCornerPadding;
        CreateTrafficLight(new Vector3(xPosition - cornerOffset, 0f, zPosition - roadsideOffset), 0f, 0f);
        CreateTrafficLight(new Vector3(xPosition + cornerOffset, 0f, zPosition + roadsideOffset), 180f, 0f);
        CreateTrafficLight(new Vector3(xPosition - roadsideOffset, 0f, zPosition + cornerOffset), 90f, 6.5f);
        CreateTrafficLight(new Vector3(xPosition + roadsideOffset, 0f, zPosition - cornerOffset), -90f, 6.5f);
    }

    private void CreateSparseBuildings()
    {
        for (int z = -400; z <= 400; z += 200)
        {
            CreateBuildingGroup(-35f, z, z + 1000);
            CreateBuildingGroup(35f, z + 55f, z + 2000);
        }
    }

    private bool IsBuildingOnRoad(float xPosition, float zPosition, float width, float depth, float clearance)
    {
        float halfWidth = width / 2f + clearance;
        float halfDepth = depth / 2f + clearance;
        float[] verticalRoads = { 0f, -250f, 250f };
        float[] horizontalRoads = { -300f, -250f, -70f, 0f, 25f, 260f, 300f };
        float roadHalfWidth = CityRoadWidth * 0.5f;

        foreach (float roadX in verticalRoads)
        {
            if (Mathf.Abs(xPosition - roadX) <= roadHalfWidth + halfWidth)
            {
                return true;
            }
        }

        foreach (float roadZ in horizontalRoads)
        {
            if (Mathf.Abs(zPosition - roadZ) <= roadHalfWidth + halfDepth)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateBuildingGroup(float xPosition, float zPosition, int seed)
    {
        Random.InitState(seed);

        int count = Random.Range(2, 4);

        for (int i = 0; i < count; i++)
        {
            float width = Random.Range(6f, 10f);
            int floors = Random.Range(8, 11);
            float height = floors * 3f;
            float depth = Random.Range(6f, 10f);
            float zOffset = i * Random.Range(8f, 12f);
            float finalZ = zPosition + zOffset;

            if (IsBuildingOnRoad(xPosition, finalZ, width, depth, 5f))
            {
                continue;
            }

            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Sparse Building";
            building.transform.position = new Vector3(xPosition, height / 2f, finalZ);
            building.transform.localScale = new Vector3(width, height, depth);
            building.GetComponent<Renderer>().material.color = Random.ColorHSV(0.52f, 0.70f, 0.15f, 0.35f, 0.45f, 0.85f);
        }
    }

    private void CreateTrafficLight(Vector3 position, float yRotation, float startOffset)
    {
        GameObject root = new GameObject("Traffic Light");
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        pole.transform.localScale = new Vector3(0.08f, 1.4f, 0.08f);
        pole.GetComponent<Renderer>().material.color = new Color(0.22f, 0.22f, 0.22f);

        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Light Box";
        box.transform.SetParent(root.transform, false);
        box.transform.localPosition = new Vector3(0f, 3f, 0f);
        box.transform.localScale = new Vector3(0.55f, 1.35f, 0.28f);
        box.GetComponent<Renderer>().material.color = new Color(0.05f, 0.05f, 0.05f);

        Renderer red = CreateLightBulb(root.transform, new Vector3(0f, 3.38f, -0.16f), Color.red);
        Renderer yellow = CreateLightBulb(root.transform, new Vector3(0f, 3f, -0.16f), Color.yellow);
        Renderer green = CreateLightBulb(root.transform, new Vector3(0f, 2.62f, -0.16f), Color.green);

        TrafficLightController controller = root.AddComponent<TrafficLightController>();
        controller.redLight = red;
        controller.yellowLight = yellow;
        controller.greenLight = green;
        controller.startOffset = startOffset;
    }

    private Renderer CreateLightBulb(Transform parent, Vector3 localPosition, Color color)
    {
        GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = "Light Bulb";
        bulb.transform.SetParent(parent, false);
        bulb.transform.localPosition = localPosition;
        bulb.transform.localScale = new Vector3(0.23f, 0.23f, 0.08f);

        Renderer renderer = bulb.GetComponent<Renderer>();
        renderer.material.color = color;
        return renderer;
    }

    private void CreateCityBuildings()
    {
        for (int i = -8; i <= 8; i++)
        {
            CreateBuildingRow(-11f, i * 10f, i);
            CreateBuildingRow(11f, i * 10f, i + 50);
        }

        for (int i = -5; i <= 5; i++)
        {
            if (Mathf.Abs(i) < 2)
            {
                continue;
            }

            CreateCrossRoadBuilding(i * 10f, 16f, i + 100);
            CreateCrossRoadBuilding(i * 10f, 34f, i + 150);
        }
    }

    private void CreateCrossRoadBuilding(float xPosition, float zPosition, int seed)
    {
        Random.InitState(seed * 37 + 11);

        float width = Random.Range(3f, 6f);
        float height = Random.Range(4f, 11f);
        float depth = Random.Range(2.5f, 4.5f);

        GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = "Second Road Building";
        building.transform.position = new Vector3(xPosition, height / 2f, zPosition);
        building.transform.localScale = new Vector3(width, height, depth);
        building.GetComponent<Renderer>().material.color = Random.ColorHSV(0.55f, 0.70f, 0.18f, 0.42f, 0.52f, 0.9f);
    }

    private void CreateBuildingRow(float xPosition, float zPosition, int seed)
    {
        Random.InitState(seed * 31 + 7);

        float width = Random.Range(2.5f, 4.5f);
        float height = Random.Range(4f, 12f);
        float depth = Random.Range(3f, 6f);

        GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = "City Building";
        building.transform.position = new Vector3(xPosition, height / 2f, zPosition);
        building.transform.localScale = new Vector3(width, height, depth);

        Renderer renderer = building.GetComponent<Renderer>();
        renderer.material.color = Random.ColorHSV(0.50f, 0.68f, 0.18f, 0.45f, 0.55f, 0.9f);

        CreateWindows(building.transform, width, height, depth, xPosition < 0f);
    }

    private void CreateWindows(Transform building, float width, float height, float depth, bool faceRight)
    {
        int floors = Mathf.Max(2, Mathf.FloorToInt(height / 1.6f));
        int columns = 2;
        float frontX = building.position.x + (faceRight ? width / 2f + 0.03f : -width / 2f - 0.03f);
        float windowZOffset = depth / 4f;

        for (int floor = 1; floor < floors; floor++)
        {
            for (int col = 0; col < columns; col++)
            {
                GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
                window.name = "Window";
                window.transform.SetParent(building, true);

                float localZ = col == 0 ? -windowZOffset : windowZOffset;
                window.transform.position = new Vector3(
                    frontX,
                    floor * 1.4f,
                    building.position.z + localZ
                );
                window.transform.localScale = new Vector3(0.04f, 0.55f, 0.6f);
                window.GetComponent<Renderer>().material.color = new Color(0.95f, 0.85f, 0.35f);
            }
        }

        GameObject shopBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shopBoard.name = "Shop Board";
        shopBoard.transform.position = new Vector3(frontX, 1.2f, building.position.z);
        shopBoard.transform.localScale = new Vector3(0.05f, 0.55f, depth * 0.75f);
        shopBoard.GetComponent<Renderer>().material.color = faceRight ? new Color(0.95f, 0.2f, 0.16f) : new Color(0.12f, 0.55f, 0.95f);
    }

    private void CreateKilometerBoards()
    {
        int kilometerNumber = 1;

        for (int z = -75; z <= 75; z += 25)
        {
            CreateKilometerBoard(-4.9f, z, kilometerNumber, true);
            CreateKilometerBoard(4.9f, z, kilometerNumber, false);
            kilometerNumber++;
        }
    }

    private void CreateKilometerBoard(float xPosition, float zPosition, int kilometerNumber, bool leftSide)
    {
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "KM Board Pole";
        pole.transform.position = new Vector3(xPosition, 0.8f, zPosition);
        pole.transform.localScale = new Vector3(0.08f, 0.8f, 0.08f);
        pole.GetComponent<Renderer>().material.color = Color.gray;

        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "KM Board";
        board.transform.position = new Vector3(xPosition, 1.75f, zPosition);
        board.transform.localScale = new Vector3(1.4f, 0.7f, 0.08f);
        board.GetComponent<Renderer>().material.color = Color.white;

        GameObject label = new GameObject("KM Text");
        label.transform.position = new Vector3(xPosition, 1.76f, zPosition - 0.07f);
        label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        TextMesh text = label.AddComponent<TextMesh>();
        text.text = kilometerNumber + " KM";
        text.fontSize = 38;
        text.characterSize = 0.16f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.black;

        if (!leftSide)
        {
            label.transform.rotation = Quaternion.Euler(90f, 180f, 0f);
        }
    }

    private void CreateBus()
    {
        GameObject busObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        busObject.name = "Player Bus";
        busObject.transform.position = cityBusStartPosition;
        busObject.transform.rotation = cityBusStartRotation;
        busObject.transform.localScale = new Vector3(2.2f, 1.4f, 4.8f);
        busObject.GetComponent<Renderer>().material.color = new Color(1f, 0.62f, 0.05f);

        busObject.AddComponent<Rigidbody>();
        AddBusWheelColliders(busObject);
        bus = busObject.AddComponent<SimpleBusController>();
    }

    private void AddBusWheelColliders(GameObject busObject)
    {
        Vector3[] wheelPositions =
        {
            new Vector3(-1.05f, -0.42f, 1.65f),
            new Vector3(1.05f, -0.42f, 1.65f),
            new Vector3(-1.05f, -0.42f, -1.65f),
            new Vector3(1.05f, -0.42f, -1.65f)
        };

        for (int i = 0; i < wheelPositions.Length; i++)
        {
            GameObject wheel = new GameObject(i < 2 ? "Front Wheel Collider" : "Rear Wheel Collider");
            wheel.transform.SetParent(busObject.transform, false);
            wheel.transform.localPosition = wheelPositions[i];

            WheelCollider collider = wheel.AddComponent<WheelCollider>();
            collider.radius = 0.45f;
            collider.suspensionDistance = 0.28f;
            collider.mass = 55f;
            collider.wheelDampingRate = 0.65f;

            JointSpring spring = collider.suspensionSpring;
            spring.spring = 22000f;
            spring.damper = 3600f;
            spring.targetPosition = 0.52f;
            collider.suspensionSpring = spring;

            WheelFrictionCurve forwardFriction = collider.forwardFriction;
            forwardFriction.extremumSlip = 0.35f;
            forwardFriction.extremumValue = 1.4f;
            forwardFriction.asymptoteSlip = 0.75f;
            forwardFriction.asymptoteValue = 1f;
            forwardFriction.stiffness = 2.2f;
            collider.forwardFriction = forwardFriction;

            WheelFrictionCurve sidewaysFriction = collider.sidewaysFriction;
            sidewaysFriction.extremumSlip = 0.22f;
            sidewaysFriction.extremumValue = 1.6f;
            sidewaysFriction.asymptoteSlip = 0.5f;
            sidewaysFriction.asymptoteValue = 1.25f;
            sidewaysFriction.stiffness = 3.2f;
            collider.sidewaysFriction = sidewaysFriction;
        }
    }

    private void CreateMovingDemoVehicle()
    {
        Color[] colors =
        {
            new Color(0.10f, 0.45f, 0.95f),
            new Color(0.90f, 0.12f, 0.10f),
            new Color(0.90f, 0.90f, 0.86f),
            new Color(0.45f, 0.18f, 0.85f),
            new Color(0.05f, 0.75f, 0.42f),
            new Color(0.95f, 0.72f, 0.10f),
            new Color(0.05f, 0.78f, 0.86f),
            new Color(0.95f, 0.28f, 0.55f),
            new Color(0.95f, 0.45f, 0.12f),
            new Color(0.25f, 0.85f, 0.25f)
        };

        for (int i = 0; i < 10; i++)
        {
            int direction = i % 2 == 0 ? 1 : -1;
            float laneX = direction > 0 ? -CityLaneOffset : CityLaneOffset;
            float startZ = -470f + i * 82f;
            float speed = 8.5f + (i % 4) * 0.8f;
            CreateMainRoadDemoVehicle("Main Road Demo Vehicle " + (i + 1), laneX, startZ, speed, colors[i % colors.Length], direction);
        }

        float[] cutRoadCenters = { -300f, 0f, 25f, 300f };

        for (int road = 0; road < cutRoadCenters.Length; road++)
        {
            for (int i = 0; i < 10; i++)
            {
                int direction = i % 2 == 0 ? 1 : -1;
                float laneZ = cutRoadCenters[road] + (direction > 0 ? CityLaneOffset : -CityLaneOffset);
                float startX = -470f + i * 78f;
                float speed = 7.5f + (i % 4) * 0.7f;
                Color color = colors[(road + i) % colors.Length];
                CreateCrossRoadDemoBus("Cut Road " + (road + 1) + " Demo Bus " + (i + 1), laneZ, startX, speed, color, direction);
            }
        }

        CreateBranchRoadDemoVehicles(250f, -300f, "Branch Road A", colors);
        CreateBranchRoadDemoVehicles(-250f, 0f, "Branch Road B", colors);
        CreateBranchRoadDemoVehicles(250f, 300f, "Branch Road C", colors);
    }

    private void CreateBranchRoadDemoVehicles(float xCenter, float zCenter, string roadName, Color[] colors)
    {
        for (int i = 0; i < 10; i++)
        {
            int direction = i % 2 == 0 ? 1 : -1;
            float laneX = xCenter + (direction > 0 ? -CityLaneOffset : CityLaneOffset);
            float startZ = zCenter - 470f + i * 78f;
            float speed = 7.8f + (i % 4) * 0.65f;
            Color color = colors[(i + 3) % colors.Length];
            CreateMainRoadDemoVehicle(roadName + " Demo Vehicle " + (i + 1), laneX, startZ, speed, color, direction);
        }
    }

    private void CreateMainRoadDemoVehicle(string name, float laneX, float startZ, float speed, Color color, int direction)
    {
        GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicle.name = name;
        vehicle.transform.position = new Vector3(laneX, 0.65f, startZ);
        vehicle.transform.localScale = new Vector3(1.6f, 1f, 3.2f);
        vehicle.GetComponent<Renderer>().material.color = color;

        BoxCollider collider = vehicle.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        DemoVehicleMover mover = vehicle.AddComponent<DemoVehicleMover>();
        mover.speed = speed;
        mover.startZ = startZ;
        mover.endZ = 470f;
        mover.laneX = laneX;
        mover.moveDirection = direction;
    }

    private void CreateCrossRoadDemoBus(string name, float laneZ, float startX, float speed, Color color, int direction)
    {
        GameObject busObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        busObject.name = name;
        busObject.transform.position = new Vector3(startX, 0.8f, laneZ);
        busObject.transform.localScale = new Vector3(3.6f, 1.3f, 1.8f);
        busObject.GetComponent<Renderer>().material.color = color;

        BoxCollider collider = busObject.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        DemoVehicleMover mover = busObject.AddComponent<DemoVehicleMover>();
        mover.moveOnXAxis = true;
        mover.speed = speed;
        mover.startX = startX;
        mover.endX = 470f;
        mover.laneZ = laneZ;
        mover.moveDirection = direction;
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cam = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1200f;

        SimpleCameraFollow follow = cam.GetComponent<SimpleCameraFollow>();
        if (follow == null)
        {
            follow = cam.gameObject.AddComponent<SimpleCameraFollow>();
        }

        follow.target = bus.transform;
        follow.offset = new Vector3(0f, 3.6f, -8.5f);
        follow.lookOffset = new Vector3(0f, 1.8f, 4.5f);
        follow.smoothness = 7.5f;
        follow.rotateWithBus = true;
        follow.SnapToTarget();
    }

    private void CreateHud()
    {
        GameObject canvasObject = new GameObject("HUD Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("Top Left Speed Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        hudPanel = panelObject.AddComponent<Image>();
        hudPanel.color = new Color(0f, 0f, 0f, 0.62f);

        RectTransform panelRect = hudPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(18f, -18f);
        panelRect.sizeDelta = new Vector2(360f, 125f);

        GameObject textObject = new GameObject("Speed Text");
        textObject.transform.SetParent(panelObject.transform, false);

        speedText = textObject.AddComponent<Text>();
        speedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        speedText.fontSize = 26;
        speedText.color = Color.white;
        speedText.text = "Speed: 0 km/h";

        RectTransform rect = speedText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(16f, -12f);
        rect.sizeDelta = new Vector2(330f, 110f);
    }
}
