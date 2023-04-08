using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PhysicsTools
{
    [System.Serializable]
    public struct RaycastHitPlus
    {
        public bool hasHit;
        public RaycastHit hit;
        public Vector3 sourcePoint;
        public Vector3 sourceDirection;
        public float sourceDistance;
        public float hitDistance;

        public void Set (bool hasHit, RaycastHit hit, Vector3 sourcePoint, Vector3 sourceDirection, float sourceDistance)
        {
            this.hasHit = hasHit;
            this.hit = hit;
            this.hitDistance = hit.distance;
            this.sourceDistance = sourceDistance;
            this.sourcePoint = sourcePoint;
            this.sourceDirection = sourceDirection;
        }
        public void Clear ()
        {
            this.hasHit = false;
            this.sourceDistance = 0f;
            this.sourcePoint = 
            this.sourceDirection = Vector3.zero;
        }
    }
    public static int SphereCast (Vector3 origin, Vector3 direction, float radius, float maxDistance, int layerMask, int sphereResolution, int distanceResolution, RaycastHitPlus[] hitBuffer)
    {
        int hitAmount = 0;
        for (int d = 0; d < distanceResolution; d++) //distance
        {
            float nd = d / ((float)distanceResolution - 1);
            Vector3 point = origin + (direction * maxDistance) * nd;

            for (int r = 0; r < sphereResolution; r++) //ring
            {
                float nr = r / ((float)sphereResolution - 1);

                Vector3 ringDirection = Quaternion.AngleAxis(nr * 360f, Vector3.up) * Vector3.forward;
                Vector3 ringPerpendicular = Vector3.Cross(ringDirection, Vector3.up);

                for (int l = 0; l < sphereResolution; l++) //lap
                {
                    float lr = l / ((float)sphereResolution - 1);
                    Vector3 lapDirection = Quaternion.AngleAxis(lr * 360f, ringPerpendicular) * ringDirection;

                    //DebugDraw.Draw(() =>
                    //{
                    //    Gizmos.DrawLine(point, point + lapDirection * radius);
                    //});

                    RaycastHit hit;
                    if (Physics.Raycast(point, lapDirection, out hit, radius, layerMask))
                    {
                        if (hitBuffer.Length <= hitAmount)
                            break;

                        hitBuffer[hitAmount].Set(true, hit, point, lapDirection, radius);
                        hitAmount++;
                    }
                }
            }
        }
        
        return hitAmount;
    }
    public static int SphereCastDownwards (Vector3 origin, float radius, float maxDistance, int layerMask, int ringAmount, int ringResolution, RaycastHitPlus[] hitBuffer, bool debug = false)
    {
        int hitAmount = 0;
        float ringSizes = radius / ringAmount;
        float distance = (radius + maxDistance);

        Vector3 firstPoint = Vector3.zero;
        firstPoint += origin;

        RaycastHit hit;

        if (debug)
        {
            DebugDraw.Draw(() =>
            {
                Gizmos.DrawLine(firstPoint, firstPoint + Vector3.up * -distance);
            });
        }

        if (Physics.Raycast(firstPoint, Vector3.down, out hit, distance, layerMask))
        {
            hitBuffer[hitAmount].Set(true, hit, firstPoint, Vector3.down, distance);
            hitAmount++;
        }

        for (int r = 0; r < ringAmount; r++) //ring
        {
            float ringSize = ringSizes * (r + 1);
            Vector3 ringVector = Vector3.forward * ringSize;

            for (int l = 0; l < ringResolution; l++) //lap of ring or ring resolution
            {
                float lr = l / ((float)ringResolution);
                Vector3 point = Quaternion.AngleAxis(lr * 360f, Vector3.up) * ringVector;
                point += origin;

                if (debug)
                {
                    DebugDraw.Draw(() =>
                    {
                        Gizmos.DrawLine(point, point + Vector3.up * -distance);
                    });
                }

                if (Physics.Raycast(point, Vector3.down, out hit, distance, layerMask))
                {
                    if (hitBuffer.Length <= hitAmount)
                        break;

                    hitBuffer[hitAmount].Set(true, hit, point, Vector3.down, distance);
                    hitAmount++;
                }
            }
        }

        return hitAmount;
    }
}
