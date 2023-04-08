using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDraw : MonoBehaviour
{
    private static DebugDraw instance;
    public static DebugDraw Instance
    {
        get
        {
            if (instance == null)
            {
                instance = CreateInstance();
            }

            return instance;
        }
    }
    public event System.Action onDebug;

    public static DebugDraw CreateInstance() => 
        new GameObject("DebugDrawInstance").AddComponent<DebugDraw>();
    public static void Draw (System.Action d)
    {
        Instance.onDebug += d;
    }

    private void OnDrawGizmos()
    {
        if (onDebug != null)
        {
            foreach (System.Action d in onDebug.GetInvocationList())
            {
                d();
                onDebug -= d;
            }
        }
    }
}
