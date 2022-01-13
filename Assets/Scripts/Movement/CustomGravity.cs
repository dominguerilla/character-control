using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomGravity
{
    static List<GravitySource> sources = new List<GravitySource>();
    public static void Register(GravitySource source)
    {
        Debug.Assert(
            !sources.Contains(source),
            $"Source {source.gameObject.name} is registered twice!");
        sources.Add(source);
    }

    public static void Unregister(GravitySource source)
    {
        Debug.Assert(
            sources.Contains(source),
            $"Source {source.gameObject.name} not registered, but is trying to be removed!");
        sources.Remove(source);
    }

    public static Vector3 GetGravity(Vector3 position)
    {
        Vector3 g = Vector3.zero;
        foreach (GravitySource source in sources)
        {
            g += source.GetGravity(position);
        }
        return g;
    }
    public static Vector3 GetGravity(Vector3 position, out Vector3 upAxis)
    {
        Vector3 g = Vector3.zero;
        foreach (GravitySource source in sources)
        {
            g += source.GetGravity(position);
        }
        upAxis = -g.normalized;
        return g;
    }

    public static Vector3 GetUpAxis(Vector3 position)
    {
        Vector3 g = Vector3.zero;
        foreach(GravitySource source in sources)
        {
            g += source.GetGravity(position);
        }
        return -g.normalized;
    }

}
