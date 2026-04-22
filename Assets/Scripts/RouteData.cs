using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RouteData", menuName = "Bus Simulator/Route Data")]
public sealed class RouteData : ScriptableObject
{
    public string routeName;
    public string startLocationName;
    public string endLocationName;
    public Vector3 startPosition;
    public Vector3 endPosition;
    public List<Vector3> pathPoints = new List<Vector3>();
}
