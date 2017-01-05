using System;
using spaar.ModLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Blocks
{
    public class IncomingDetectionScript : MonoBehaviour
    {
        public Collider Main;
        public DroneStandardConputingScript MainMain;
        public List<Vector3> IncomingPositions = new List<Vector3>();
        public float SphereSize;
        public float VerticalPrecisionInDegree = 15;
        public float HorizontalPrecisionInDegree = 15;
        public bool UseRadarDetection = false;
        public LineRenderer LR;
        public int VertexCount;
        void Start()
        {
            LR = this.gameObject.AddComponent<LineRenderer>();
            LR.material = new Material(Shader.Find("Particles/Additive"));
            LR.SetWidth(0.05f, 0.05f);
            LR.SetColors(Color.red, Color.red);
            VertexCount = 3500;
        }

        void FakeFixedUpdate()
        {
            DebugShowingLines();

            if (!StatMaster.isSimulating)
            {
                Destroy(this.gameObject);
            }
            if (IncomingPositions.Count != 0)
            {
                MainMain.IncomingVectors = IncomingPositions.ToArray();
                IncomingPositions.Clear();
            }

            List<Vector3> collidingPoints = RegularSphereScan(MainMain.transform.position, VerticalPrecisionInDegree, HorizontalPrecisionInDegree, SphereSize);
            if (collidingPoints.Count != 0)
            {
                IncomingPositions.AddRange(collidingPoints);
            }
            Debug.Log(IncomingPositions.Count);
        }
        void FixedUpdate()
        {
            DebugShowingLines();
            MainMain.IncomingVectors = IncomingPositions.ToArray();
            IncomingPositions.Clear();

            if (!StatMaster.isSimulating)
            {
                Destroy(this.gameObject);
            }

            List<Vector3> collidingPoints = RegularSphereScan(MainMain.transform.position, VerticalPrecisionInDegree, HorizontalPrecisionInDegree, SphereSize);
            if (collidingPoints.Count != 0)
            {
                IncomingPositions.AddRange(collidingPoints);
            }
            //Debug.Log(MainMain.IncomingVectors.Length);
        }
        void OnTriggerEnter(Collider coll)
        {
            OnTriggerStay(coll);
        }
        void OnTriggerStay(Collider coll)
        {
            if (coll == Main || coll.isTrigger)
            {
                return;
            }
            Vector3 ClosestTemp = coll.ClosestPointOnBounds(this.transform.position);
            float LongestGasp = SphereSize * Mathf.Sin(Mathf.Deg2Rad * Mathf.Max(VerticalPrecisionInDegree, HorizontalPrecisionInDegree));
            RaycastHit hito;
            if (//Small enough
                (
                (coll is SphereCollider && coll.gameObject.GetComponent<SphereCollider>().radius <= LongestGasp / 2) ||
                (coll is BoxCollider && coll.gameObject.GetComponent<BoxCollider>().size.sqrMagnitude <= LongestGasp) ||
                (coll is CapsuleCollider && coll.gameObject.GetComponent<CapsuleCollider>().radius <= LongestGasp / 2 && coll.gameObject.GetComponent<CapsuleCollider>().height <= LongestGasp)
                )
                && coll.transform.lossyScale.sqrMagnitude <= 1
                )
            {
                IncomingPositions.Add(coll.transform.position);
            }
            else if (coll.Raycast(new Ray(this.transform.position, this.transform.InverseTransformPoint(ClosestTemp).normalized * SphereSize), out hito, SphereSize))
            {
                if (hito.collider == coll)
                {
                    IncomingPositions.Add(ClosestTemp);
                    return;
                }
            }
            else
            {
                UseRadarDetection = true;
            }
            //if(coll.GetComponent<MeshFilter>())
            //{
            //    Vector3[] Vertics = coll.GetComponent<MeshFilter>().mesh.vertices;
            //}

        }

        List<Vector3> RegularSphereScan(Vector3 StartPoint, float HorizontalDegreePrecision, float VerticalDegreePrecision, float Distance)
        {
            List<Vector3> HitPoints = new List<Vector3>();
            RaycastHit hito;
            for (float Vi = VerticalDegreePrecision; Vi < 360; Vi += VerticalDegreePrecision)
            {
                for (float Hi = 0; Hi < 360; Hi += HorizontalDegreePrecision)
                {
                    //float elevation = Hi * Mathf.Deg2Rad;
                    //float heading = Vi * Mathf.Deg2Rad;
                    Ray rayray = new Ray(StartPoint, EulerToDirection(Hi, Vi));
                    RaycastHit[] RHs = Physics.RaycastAll(rayray, SphereSize);
                    foreach (RaycastHit RH in RHs)
                    {
                        //Debug.Log(RH.collider.gameObject.name);
                        if (RH.collider.isTrigger == false && RH.collider != Main && RH.collider != this.gameObject.GetComponent<Collider>())
                        {
                            HitPoints.Add(RH.point);
                            //Gizmos.DrawRay(rayray);
                        }
                    }
                }
            }
            return HitPoints;
        }
        Vector3[] ClosestVecctor3(List<Vector3> WorldVectors, Vector3 StartPoint, int Counts)
        {
            Vector3[] Returner = new Vector3[Counts];
            for (int i = 0; i < Returner.Length; ++i)
            {
                Returner[i] = Vector3.one * Mathf.Infinity;
            }
            foreach (Vector3 Point in WorldVectors)
            {
                WorldVectors.Sort();
            }
            foreach (Vector3 SinglePoint in WorldVectors)
            {
                for (int a = 0; a < Returner.Length; ++a)
                {
                    if (Vector3.SqrMagnitude(StartPoint - SinglePoint) < Vector3.SqrMagnitude(StartPoint - Returner[a]))
                    {
                        for (int i = Returner.Length - 1; i > a; --i)
                        {
                            Returner[i] = Returner[i - 1];
                        }
                        Returner[a] = SinglePoint;
                        break;
                    }
                }
            }
            return Returner;
        }

        Vector3 EulerToDirection(float Elevation, float Heading)
        {
            float elevation = Elevation * Mathf.Deg2Rad;
            float heading = Heading * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(elevation) * Mathf.Sin(heading), Mathf.Sin(elevation), Mathf.Cos(elevation) * Mathf.Cos(heading));
        }
        void DebugShowingLines()
        {
            VertexCount = IncomingPositions.Count * 2 + 1;
            LR.SetVertexCount(VertexCount);
            int i = 0;
            foreach (Vector3 cp in IncomingPositions)
            {
                LR.SetPosition(i, this.transform.position);
                ++i;
                LR.SetPosition(i, cp);
                ++i;
            }
        }
        protected void LogHo()
        {
            Debug.Log("ho");
        }
    }
}
