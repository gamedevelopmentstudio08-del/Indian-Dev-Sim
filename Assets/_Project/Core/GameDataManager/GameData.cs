using System;
using System.Collections.Generic;
using UnityEngine;

public enum RouteWaypointType
{
    StartPoint,
    PickupPoint,
    DropPoint
}

[Serializable]
public struct RouteWaypoint
{
    public RouteWaypointType Type;
    public Vector3 Position;

    public RouteWaypoint(RouteWaypointType type, Vector3 position)
    {
        Type = type;
        Position = position;
    }
}

public static class GameData
{
    public static string PickupSceneName = "PickupScene";
    public static string DropOffSceneName = "DropOffScene";
    public static string SelectedRouteName;
    public static Vector3 StartPosition;
    public static Vector3 PickupPosition;
    public static Vector3 EndPosition;
    public static Vector3 SceneSpawnPosition;
    public static Quaternion SceneSpawnRotation = Quaternion.identity;
    public static List<Vector3> PathPoints = new List<Vector3>();
    public static List<RouteWaypoint> RouteWaypoints = new List<RouteWaypoint>();
    public static List<string> PassengerNames = new List<string>();
    public static string CurrentWeatherLabel = "Morning / Sunny";
    public static string DropOffObjective = "Drop passengers at the destination";
    public static int Coins = 5000;
    public static float Fuel = 100f;
    public static float Sleep = 100f;
    public static float Satisfaction = 100f;
    public static bool IsDropOffPhase;
    public static event Action RouteChanged;
    public static event Action<bool> RoutePhaseChanged;

    public static void SetRoute(RouteData route)
    {
        if (route == null)
        {
            return;
        }

        IsDropOffPhase = false;
        PassengerNames = new List<string>();
        SceneSpawnPosition = route.startPosition;
        SceneSpawnRotation = Quaternion.identity;
        SelectedRouteName = route.routeName;
        StartPosition = route.startPosition;
        EndPosition = route.endPosition;
        PathPoints = route.pathPoints != null ? new List<Vector3>(route.pathPoints) : new List<Vector3>();
        PickupPosition = ResolvePickupPosition(route);
        RouteWaypoints = new List<RouteWaypoint>
        {
            new RouteWaypoint(RouteWaypointType.StartPoint, StartPosition),
            new RouteWaypoint(RouteWaypointType.PickupPoint, PickupPosition),
            new RouteWaypoint(RouteWaypointType.DropPoint, EndPosition)
        };
        DropOffObjective = string.IsNullOrWhiteSpace(route.endLocationName)
            ? "Drop passengers at the destination"
            : "Drop passengers at " + route.endLocationName;

        RaiseRouteChanged();
        RaiseRoutePhaseChanged();
    }

    private static Vector3 ResolvePickupPosition(RouteData route)
    {
        if (route == null)
        {
            return Vector3.zero;
        }

        if (route.pathPoints != null && route.pathPoints.Count >= 2)
        {
            return route.pathPoints[1];
        }

        Vector3 direction = route.endPosition - route.startPosition;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector3.forward * 50f;
        }

        return route.startPosition + direction.normalized * Mathf.Min(250f, direction.magnitude * 0.2f);
    }

    public static void BeginDropOffPhase(List<string> passengers)
    {
        IsDropOffPhase = true;
        PassengerNames = passengers != null ? new List<string>(passengers) : new List<string>();
        if (PassengerNames.Count == 0)
        {
            PassengerNames.Add("Passenger 1");
            PassengerNames.Add("Passenger 2");
            PassengerNames.Add("Passenger 3");
        }

        RaiseRoutePhaseChanged();
    }

    public static void SetSceneSpawn(Vector3 position, Quaternion rotation)
    {
        SceneSpawnPosition = position;
        SceneSpawnRotation = rotation;
    }

    public static void AddCoins(int amount)
    {
        Coins = Mathf.Max(0, Coins + amount);
    }

    public static void AdjustFuel(float delta)
    {
        Fuel = Mathf.Clamp(Fuel + delta, 0f, 100f);
    }

    public static void AdjustSleep(float delta)
    {
        Sleep = Mathf.Clamp(Sleep + delta, 0f, 100f);
    }

    public static void AdjustSatisfaction(float delta)
    {
        Satisfaction = Mathf.Clamp(Satisfaction + delta, 0f, 100f);
    }

    public static void SetWeatherLabel(string label)
    {
        if (!string.IsNullOrWhiteSpace(label))
        {
            CurrentWeatherLabel = label;
        }
    }

    public static bool HasRoute()
    {
        return !string.IsNullOrWhiteSpace(SelectedRouteName);
    }

    private static void RaiseRouteChanged()
    {
        Action handler = RouteChanged;
        if (handler != null)
        {
            handler.Invoke();
        }
    }

    private static void RaiseRoutePhaseChanged()
    {
        Action<bool> handler = RoutePhaseChanged;
        if (handler != null)
        {
            handler.Invoke(IsDropOffPhase);
        }
    }
}
