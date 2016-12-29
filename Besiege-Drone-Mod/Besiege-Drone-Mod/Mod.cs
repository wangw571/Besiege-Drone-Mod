using System;
using spaar.ModLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
namespace Besiege_Drone_Mod
{

    // If you need documentation about any of these values or the mod loader
    // in general, take a look at https://spaar.github.io/besiege-modloader.


    /// <summary>
    /// Note: 
    /// HP: The HP will be limited as an square of an acceleration, as (a+b)^2 < a^2 + b^2
    /// Radar: The radar detective distance will change as the velocity changes. 
    /// 
    /// 
    /// Basic Behavior:
    /// 1.Exist, as decloak
    /// 2.Select Target, 
    ///     Explosives first, bombs, tracking computers first, grenade and rocket will be selected as the ammunation is possible to ignite them. 
    ///     Other aggressive second, include cannon, watercannon and flamethrower
    ///         -Mainly select the joint between aggressive block and vulnerable blocks.
    ///         -If connected by Grabber, find the chain of the end and destory that
    ///             -If multiple connected, ignore this target if possible.
    ///     Machanic blocks after, mainly focus on wheel(CogMotor), piston, suspension, flying block, hinge
    ///     Aeromatic propeller, wings and non-powered wheels, as well as structures as last. 
    ///     
    ///     -Not ignoring incoming only if HP is low enough that a crash can disable it
    ///         -Set it with make flame effect and non-fireable and explode after it crash.
    ///     -When close enough to an incoming at the front (velocity,sqrmagnitude * 3), to 5
    /// 3.Set navigation destination and attack
    ///     Attack by projectiles.
    /// 4.Maybe - Switch target every 10 second.
    /// 5.After target destoryed/close enough
    ///     - Switch if attitude is allowed(higher than velocity,sqrmagnitude * 3) and target is far enough(velocity,sqrmagnitude * 8)
    ///     - Escape 
    /// 6.Climb to sky is clear, set as direction + 500 Y, else keep curise flight and not ignoring incoming. 
    /// 
    /// 
    /// </summary>

    public class DroneMod : Mod
    {
        public override string Name { get; } = "Drone_Deployment_Block";
        public override string DisplayName { get; } = "Drone Deployment Block";
        public override string Author { get; } = "wang_w571";
        public override Version Version { get; } = new Version("0.1");

        public override void OnLoad()
        {
            // Your initialization code here
        }

        public override void OnUnload()
        {
            // Your code here
            // e.g. save configuration, destroy your objects if CanBeUnloaded is true etc
        }
    }

    public class DroneDeployBlockBehavior : BlockScript
    {
        MKey Activation;
        MKey Engage;
        MKey ForceEngage;
        MMenu DroneAIType;
        MMenu DroneSize;
        MMenu Difficulty;

        public override void SafeAwake()
        {
            Activation = new MKey("Activate Spawning Drones", "Activate", KeyCode.P);
            Activation.DisplayInMapper = false;
            Engage = new MKey("Engage", "Engage", KeyCode.T);
            ForceEngage = new MKey("Forced Engege", "FEngage", KeyCode.X);
            DroneAIType = new MMenu("AIType", 0, new List<string>() { "Assistantnce", "Aggressive", "Practice" });
        }
        protected override void BuildingUpdate()
        {


        }

    }

    public class DroneStandardConputingScript : MonoBehaviour
    {
        protected int iterativeCount = 0;
        protected GameObject currentTarget;
        protected Vector3 targetPoint;
        protected float 炮弹速度;
        protected Vector3 前一帧速度;
        protected float 目标前帧速度Mag;
        protected float MyPrecision;
        protected int MySize;
        protected float 精度;
        protected float size;
        protected GameObject IncomingDetection;
        protected IncomingDetectionScript IDS;
        protected bool IgnoreIncoming = false;


        protected Vector2 formulaProjectile(float X, float Y, float V, float G)
        {
            if (G == 0)
            {
                float THETA = Mathf.Atan(Y / X);
                float T = (Y / Mathf.Sin(THETA)) / V;
                return (new Vector2(THETA, T));
            }
            else
            {
                float DELTA = Mathf.Pow(V, 4) - G * (G * X * X - 2 * Y * V * V);
                if (DELTA < 0)
                {
                    return Vector2.zero;
                }
                float THETA1 = Mathf.Atan((-(V * V) + Mathf.Sqrt(DELTA)) / (G * X));
                float THETA2 = Mathf.Atan((-(V * V) - Mathf.Sqrt(DELTA)) / (G * X));
                if (THETA1 > THETA2)
                    THETA1 = THETA2;
                float T = X / (V * Mathf.Cos(THETA1));
                return new Vector2(THETA1, T);
            }
        }

        protected Vector3 formulaTarget(float VT, Vector3 PT, Vector3 DT, float TT)
        {
            Vector3 newPosition = PT + DT * (VT * TT);
            return newPosition;
        }

        protected Vector3 calculateNoneLinearTrajectory(float gunVelocity, float AirDrag, Vector3 gunPosition, float TargetVelocity, Vector3 TargetPosition, Vector3 TargetDirection, Vector3 hitPoint, float G, float accuracy, float diff)
        {
            iterativeCount++;
            if (iterativeCount > 512) { iterativeCount = 0; return hitPoint; }
            if (hitPoint == Vector3.zero || gunVelocity < 1)
            {
                return currentTarget.transform.position;
            }
            Vector3 gunDirection = new Vector3(hitPoint.x, gunPosition.y, hitPoint.z) - gunPosition;
            Quaternion gunRotation = Quaternion.FromToRotation(gunDirection, Vector3.forward);
            Vector3 localHitPoint = gunRotation * (hitPoint - gunPosition);
            float currentCalculatedDistance = (hitPoint - gunPosition).magnitude;

            float b2M4ac = gunVelocity * gunVelocity - 4 * AirDrag * currentCalculatedDistance;
            if (b2M4ac < 0) { /*Debug.Log("Nan!!!" + (gunVelocity * gunVelocity - 2 * AirDrag * currentCalculatedDistance));*/ return currentTarget.transform.position; }
            float V = (float)Math.Sqrt(b2M4ac);
            float X = localHitPoint.z;//z为前方
            float Y = localHitPoint.y;
            Vector2 TT = formulaProjectile(X, Y, V, G);
            if (TT == Vector2.zero)
            {
                iterativeCount = 0;
                return currentTarget.transform.position;
            }
            float VT = TargetVelocity;
            Vector3 PT = TargetPosition;
            Vector3 DT = TargetDirection;
            float T = TT.y;
            Vector3 newHitPoint = formulaTarget(VT, PT, DT, T);
            float diff1 = (newHitPoint - hitPoint).magnitude;
            if (diff1 > diff)
            {
                iterativeCount = 0;
                return currentTarget.transform.position;
            }
            if (diff1 < accuracy)
            {
                gunRotation = Quaternion.Inverse(gunRotation);
                Y = Mathf.Tan(TT.x) * X;
                newHitPoint = gunRotation * new Vector3(0, Y, X) + gunPosition;
                iterativeCount = 0;
                return newHitPoint;
            }
            return calculateNoneLinearTrajectory(gunVelocity, AirDrag, gunPosition, TargetVelocity, TargetPosition, TargetDirection, newHitPoint, G, accuracy, diff1);
        }
        protected Vector3 calculateNoneLinearTrajectoryWithAccelerationPrediction(float gunVelocity, float AirDrag, Vector3 gunPosition, float TargetVelocity, float targetAcceleration, Vector3 TargetPosition, Vector3 TargetDirection, Vector3 hitPoint, float G, float accuracy, float diff)
        {
            iterativeCount++;
            if (iterativeCount > 512) { iterativeCount = 0; return TargetPosition; }
            if (hitPoint == Vector3.zero || gunVelocity < 1)
            {
                return currentTarget.transform.position;
            }
            Vector3 gunDirection = new Vector3(hitPoint.x, gunPosition.y, hitPoint.z) - gunPosition;
            Quaternion gunRotation = Quaternion.FromToRotation(gunDirection, Vector3.forward);
            Vector3 localHitPoint = gunRotation * (hitPoint - gunPosition);
            float currentCalculatedDistance = (hitPoint - gunPosition).magnitude;

            float b2M4ac = gunVelocity * gunVelocity - 4 * AirDrag * currentCalculatedDistance;
            if (b2M4ac < 0) { /*Debug.Log("Nan!!!" + (gunVelocity * gunVelocity - 2 * AirDrag * currentCalculatedDistance));*/ return currentTarget.transform.position; }
            float V = (float)Math.Sqrt(b2M4ac);
            float X = localHitPoint.z;//z为前方
            float Y = localHitPoint.y;
            Vector2 TT = formulaProjectile(X, Y, V, G);
            if (TT == Vector2.zero)
            {
                iterativeCount = 0;
                return currentTarget.transform.position;
            }
            float VT = TargetVelocity + targetAcceleration * currentCalculatedDistance;
            Vector3 PT = TargetPosition;
            Vector3 DT = TargetDirection;
            float T = TT.y;
            Vector3 newHitPoint = formulaTarget(VT, PT, DT, T);
            float diff1 = (newHitPoint - hitPoint).magnitude;
            if (diff1 > diff)
            {
                iterativeCount = 0;
                return currentTarget.transform.position;
            }
            if (diff1 < accuracy)
            {
                gunRotation = Quaternion.Inverse(gunRotation);
                Y = Mathf.Tan(TT.x) * X;
                newHitPoint = gunRotation * new Vector3(0, Y, X) + gunPosition;
                iterativeCount = 0;
                return newHitPoint;
            }
            return calculateNoneLinearTrajectory(gunVelocity, AirDrag, gunPosition, TargetVelocity, TargetPosition, TargetDirection, newHitPoint, G, accuracy, diff1);
        }
        protected Vector3 calculateLinearTrajectory(float gunVelocity, Vector3 gunPosition, float TargetVelocity, Vector3 TargetPosition, Vector3 TargetDirection)
        {

            Vector3 hitPoint = Vector3.zero;

            if (TargetVelocity != 0)
            {
                Vector3 D = gunPosition - TargetPosition;
                float THETA = Vector3.Angle(D, TargetDirection) * Mathf.Deg2Rad;
                float DD = D.magnitude;

                float A = 1 - Mathf.Pow((gunVelocity / TargetVelocity), 2);
                float B = -(2 * DD * Mathf.Cos(THETA));
                float C = DD * DD;
                float DELTA = B * B - 4 * A * C;

                if (DELTA < 0)
                {
                    return Vector3.zero;
                }

                float F1 = (-B + Mathf.Sqrt(B * B - 4 * A * C)) / (2 * A);
                float F2 = (-B - Mathf.Sqrt(B * B - 4 * A * C)) / (2 * A);

                if (F1 < F2)
                    F1 = F2;
                hitPoint = TargetPosition + TargetDirection * F1;
            }
            else
            {
                hitPoint = TargetPosition;
            }
            return hitPoint;
        }
        protected Vector3 calculateLinearTrajectoryWithAccelerationPrediction(float gunVelocity, Vector3 gunPosition, float TargetVelocity, float TargetAcceleration, Vector3 TargetPosition, Vector3 TargetDirection, Vector3 PredictedPoint, float Precision)
        {

            Vector3 hitPoint = Vector3.zero;

            iterativeCount++;
            if (iterativeCount > 512) { iterativeCount = 0; return calculateLinearTrajectory(gunVelocity, gunPosition, TargetVelocity, targetPoint, TargetDirection); }

            if (TargetVelocity != 0)
            {
                Vector3 D = gunPosition - TargetPosition;
                float THETA = Vector3.Angle(D, TargetDirection) * Mathf.Deg2Rad;
                float DD = D.magnitude;

                float A = 1 - Mathf.Pow((gunVelocity / TargetVelocity + (TargetAcceleration * (PredictedPoint.magnitude / gunVelocity))), 2);
                float B = -(2 * DD * Mathf.Cos(THETA));
                float C = DD * DD;
                float DELTA = B * B - 4 * A * C;

                if (DELTA < 0)
                {
                    return Vector3.zero;
                }

                float F1 = (-B + Mathf.Sqrt(B * B - 4 * A * C)) / (2 * A);
                float F2 = (-B - Mathf.Sqrt(B * B - 4 * A * C)) / (2 * A);

                if (F1 < F2 && F1 >= 0)
                    F1 = F2;
                hitPoint = TargetPosition + TargetDirection * F1;
            }
            else
            {
                hitPoint = TargetPosition;
            }
            if ((hitPoint - PredictedPoint).sqrMagnitude < Precision * Precision)
            {
                return hitPoint;
            }
            else
            {
                return calculateLinearTrajectoryWithAccelerationPrediction(gunVelocity, gunPosition, TargetVelocity, TargetAcceleration, TargetPosition, TargetDirection, hitPoint, Precision);
            }
        }

        protected Vector3 getCorrTorque(Vector3 from, Vector3 to, Rigidbody rb, float SpeedPerSecond)
        {
            try
            {
                Vector3 x = Vector3.Cross(from.normalized, to.normalized);                // axis of rotation
                float theta = Mathf.Asin(x.magnitude);                                    // angle between from & to
                Vector3 w = x.normalized * theta / SpeedPerSecond;                        // scaled angular acceleration
                Vector3 w2 = w - rb.angularVelocity;                                      // need to slow down at a point
                Quaternion q = rb.rotation * rb.inertiaTensorRotation;                    // transform inertia tensor
                return q * Vector3.Scale(rb.inertiaTensor, (Quaternion.Inverse(q) * w2)); // calculate final torque
            }
            catch { return Vector3.zero; }
        }
        protected AxialDrag AD;

        void Start()
        {
            AD = this.gameObject.AddComponent<AxialDrag>();
            AD.AxisDrag = new Vector3(0, 0.015f, 0.015f);
            AD.velocityCap = 300;
            MyPrecision = MySize * 5;
        }
        protected Vector3 CalculateTarget(Vector3 LocalTargetDirection, float FireProg)
        {
            炮弹速度 = this.GetComponent<Rigidbody>().velocity.magnitude;
            float targetVelo = currentTarget.GetComponent<Rigidbody>().velocity.magnitude;
            //Debug.Log((currentTarget.GetComponent<Rigidbody>().velocity - 前一帧速度).magnitude);
            LocalTargetDirection = calculateNoneLinearTrajectoryWithAccelerationPrediction(
                炮弹速度 + 0.001f,
                (this.GetComponent<Rigidbody>().velocity - 前一帧速度).magnitude,
                transform.position,
                targetVelo,
                目标前帧速度Mag - targetVelo,
                currentTarget.transform.position,
                currentTarget.GetComponent<Rigidbody>().velocity.normalized,
                    calculateLinearTrajectoryWithAccelerationPrediction(
                        炮弹速度,
                        transform.position,
                        targetVelo,
                        targetVelo - 目标前帧速度Mag,
                        currentTarget.transform.position,
                        currentTarget.GetComponent<Rigidbody>().velocity.normalized,
                        calculateLinearTrajectory(
                            炮弹速度,
                            transform.position,
                            targetVelo,
                            currentTarget.transform.position,
                            currentTarget.GetComponent<Rigidbody>().velocity.normalized),
                        MyPrecision
                    ),
                    Physics.gravity.y,
                    MyPrecision,
                    float.PositiveInfinity
                    );
            目标前帧速度Mag = targetVelo;

            if (LocalTargetDirection.y == float.NaN)
            {
                LocalTargetDirection = currentTarget.transform.position;
            }
            前一帧速度 = GetComponent<Rigidbody>().velocity;
            return LocalTargetDirection;
        }


        protected Vector3 DroneDirectionIndicator(Vector3 LocalTargetDirection)
        {
            炮弹速度 = this.GetComponent<Rigidbody>().velocity.magnitude;
            float targetVelo = currentTarget.GetComponent<Rigidbody>().velocity.magnitude;
            //Debug.Log((currentTarget.GetComponent<Rigidbody>().velocity - 前一帧速度).magnitude);
            LocalTargetDirection = calculateNoneLinearTrajectoryWithAccelerationPrediction(
                炮弹速度 + 0.001f,
                (this.GetComponent<Rigidbody>().velocity - 前一帧速度).magnitude,
                transform.position,
                targetVelo,
                目标前帧速度Mag - targetVelo,
                currentTarget.transform.position,
                currentTarget.GetComponent<Rigidbody>().velocity.normalized,
                    calculateLinearTrajectoryWithAccelerationPrediction(
                        炮弹速度,
                        transform.position,
                        targetVelo,
                        targetVelo - 目标前帧速度Mag,
                        currentTarget.transform.position,
                        currentTarget.GetComponent<Rigidbody>().velocity.normalized,
                        calculateLinearTrajectory(
                            炮弹速度,
                            transform.position,
                            targetVelo,
                            currentTarget.transform.position,
                            currentTarget.GetComponent<Rigidbody>().velocity.normalized),
                        size * 精度 + 10 * size
                    ),
                    Physics.gravity.y,
                    size * 精度 + 10 * size,
                    float.PositiveInfinity
                    );
            目标前帧速度Mag = targetVelo;

            if (LocalTargetDirection.y == float.NaN)
            {
                LocalTargetDirection = currentTarget.transform.position;
            }
            前一帧速度 = GetComponent<Rigidbody>().velocity;
            return LocalTargetDirection;
        }
        protected Vector3 RelativeAvenueOfPoints(List<Vector3> Vector3s, float SphereSize)
        {
            Vector3 V3 = this.transform.forward;
            if (Vector3s.Count > 0)
            {
                foreach (Vector3 VT3 in Vector3s)
                {
                    Vector3 RealVT3 = (this.transform.position - VT3);
                    RealVT3 = RealVT3.normalized * (SphereSize - Vector3.Distance(this.transform.position, VT3));
                    V3 = Vector3.Lerp(V3, RealVT3, 0.5f);
                }
            }
            return V3;
        }
    }

    public class IncomingDetectionScript : MonoBehaviour
    {
        public Collider Main;
        public List<Vector3> IncomingPositions;
        public bool SomethingInMyRange = false;
        public float SphereSize;
        public float VerticalPrecisionInDegree = 15;
        public float HorizontalPrecisionInDegree = 15;
        public bool UseRadarDetection = false;
        void FixedUpdate()
        {
            IncomingPositions = new List<Vector3>();
            if(UseRadarDetection)
            {
                List<Vector3> collidingPoints = RegularSphereScan(this.transform.position, VerticalPrecisionInDegree, HorizontalPrecisionInDegree, SphereSize, this.gameObject.layer);
                if (collidingPoints.Count != 0)
                {
                    IncomingPositions.AddRange(collidingPoints);
                }
                UseRadarDetection = false;
            }
            SomethingInMyRange = false;
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
            if(
                (coll.GetType() == typeof(SphereCollider) && coll.gameObject.GetComponent<SphereCollider>().radius <= LongestGasp / 2) ||
                (coll.GetType() == typeof(BoxCollider) && coll.gameObject.GetComponent<BoxCollider>().size.sqrMagnitude <= LongestGasp) ||
                (coll.GetType() == typeof(CapsuleCollider) && coll.gameObject.GetComponent<CapsuleCollider>().radius <= LongestGasp / 2 && coll.gameObject.GetComponent<CapsuleCollider>().height <= LongestGasp)
                ) {

            }
            else if (this.GetComponent<Collider>().Raycast(new Ray(this.transform.position, this.transform.InverseTransformPoint(ClosestTemp).normalized * SphereSize), out hito, SphereSize))
            {
                if (hito.collider == coll)
                {
                    IncomingPositions.Add(ClosestTemp);
                    SomethingInMyRange = true;
                    return;
                }
            }
            //if(coll.GetComponent<MeshFilter>())
            //{
            //    Vector3[] Vertics = coll.GetComponent<MeshFilter>().mesh.vertices;
            //}
            SomethingInMyRange = true;
        }

        List<Vector3> RegularSphereScan(Vector3 StartPoint, float HorizontalDegreePrecision, float VerticalDegreePrecision, float Distance, int IgnoreLayer)
        {
            List<Vector3> HitPoints = new List<Vector3>();
            RaycastHit hito;
            for (float Vi = VerticalDegreePrecision; Vi < 360; Vi += VerticalDegreePrecision)
            {
                for (float Hi = 0; Hi < 360; Hi += HorizontalDegreePrecision)
                {
                    //float elevation = Hi * Mathf.Deg2Rad;
                    //float heading = Vi * Mathf.Deg2Rad;
                    Ray rayray = new Ray(StartPoint, EulerToDirection(Hi,Vi));
                    if (Physics.Raycast(rayray, out hito, SphereSize,IgnoreLayer))
                    {
                        HitPoints.Add(hito.point);
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
    }

    public class FullyAIDrone : DroneStandardConputingScript
    {
        int MissileGuidanceModeInt = 0;
        int size = 1;
        float 精度 = 0.02f;
        int RotatingSpeed = 1;
        float SphereSize = 15;

        void FixedUpdate()
        {
            if (IncomingDetection == null)
            {
                IncomingDetection = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                IncomingDetection.layer = 15;
                
                Destroy(IncomingDetection.GetComponent<MeshRenderer>());
                Destroy(IncomingDetection.GetComponent<Rigidbody>());
                IDS = IncomingDetection.AddComponent<IncomingDetectionScript>();
            }
            Vector3 TargetDirection;

            if (IDS.SomethingInMyRange && !IgnoreIncoming)
            {
                Vector3 LocalTargetDirection = this.transform.TransformPoint(RelativeAvenueOfPoints(IDS.IncomingPositions, SphereSize));

                if (MissileGuidanceModeInt == 0)
                {
                    LocalTargetDirection = currentTarget.transform.position;
                    LocalTargetDirection = DroneDirectionIndicator(LocalTargetDirection);
                }

                //this.transform.rotation.SetFromToRotation(this.transform.forward, LocalTargetDirection);
                //Vector3 rooo = Vector3.RotateTowards(this.transform.forward, LocalTargetDirection - this.transform.position, RotatingSpeed * size, RotatingSpeed * size);
                //Debug.Log(LocalTargetDirection + "and" + this.transform.up + "and" + rooo);
                //this.transform.rotation = Quaternion.LookRotation(rooo);
                //LocalTargetDirection = new Vector3(LocalTargetDirection.x, LocalTargetDirection.y - this.transform.position.y, LocalTargetDirection.z);
                //float mag = (LocalTargetDirection.normalized - transform.forward.normalized).magnitude;
                TargetDirection = (getCorrTorque(this.transform.forward, LocalTargetDirection - this.transform.position * 1, this.GetComponent<Rigidbody>(), 0.01f * size) * Mathf.Rad2Deg).normalized;

                GetComponent<Rigidbody>().angularVelocity = (TargetDirection * RotatingSpeed);

            }
            else if (currentTarget)
            {
                if (currentTarget.GetComponent<Rigidbody>() && currentTarget.transform.position != this.transform.position)
                {
                    Vector3 LocalTargetDirection = currentTarget.transform.position;

                    if (MissileGuidanceModeInt == 0)
                    {
                        LocalTargetDirection = currentTarget.transform.position;
                        LocalTargetDirection = DroneDirectionIndicator(LocalTargetDirection);
                    }

                    //this.transform.rotation.SetFromToRotation(this.transform.forward, LocalTargetDirection);
                    //Vector3 rooo = Vector3.RotateTowards(this.transform.forward, LocalTargetDirection - this.transform.position, RotatingSpeed * size, RotatingSpeed * size);
                    //Debug.Log(LocalTargetDirection + "and" + this.transform.up + "and" + rooo);
                    //this.transform.rotation = Quaternion.LookRotation(rooo);
                    //LocalTargetDirection = new Vector3(LocalTargetDirection.x, LocalTargetDirection.y - this.transform.position.y, LocalTargetDirection.z);
                    //float mag = (LocalTargetDirection.normalized - transform.forward.normalized).magnitude;
                    TargetDirection = (getCorrTorque(this.transform.forward, LocalTargetDirection - this.transform.position * 1, this.GetComponent<Rigidbody>(), 0.01f * size) * Mathf.Rad2Deg).normalized;
                    if (Vector3.Angle(transform.forward, LocalTargetDirection - this.transform.position * 1) < 105)
                    {
                        GetComponent<Rigidbody>().angularVelocity = (TargetDirection * RotatingSpeed);
                    }
                    else { Debug.Log("Target Lost!"); }
                }
            }
            前一帧速度 = this.GetComponent<Rigidbody>().velocity;
        }
    }
}
