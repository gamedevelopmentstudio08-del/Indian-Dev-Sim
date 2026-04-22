using System.Collections.Generic;
using UnityEngine;

public static class GameData
{
    public static string SelectedRouteName;
    public static Vector3 StartPosition;
    public static Vector3 EndPosition;
    public static List<Vector3> PathPoints = new List<Vector3>();

    public static void SetRoute(RouteData route)
    {
        if (route == null)
        {
            return;
        }

        SelectedRouteName = route.routeName;
        StartPosition = route.startPosition;
        EndPosition = route.endPosition;
        PathPoints = route.pathPoints != null ? new List<Vector3>(route.pathPoints) : new List<Vector3>();
    }

    public static bool HasRoute()
    {
        return !string.IsNullOrWhiteSpace(SelectedRouteName);
    }
}
