using UnityEngine;

public sealed class RouteManager : MonoBehaviour
{
    private RouteData activeRoute;

    public RouteData ActiveRoute => activeRoute;

    public void StartRoute(RouteData route)
    {
        activeRoute = route;
        if (activeRoute == null)
        {
            Debug.LogWarning("RouteManager: StartRoute called with null route.");
            return;
        }

        Debug.Log($"Route Started: {activeRoute.routeName}");
    }
}
