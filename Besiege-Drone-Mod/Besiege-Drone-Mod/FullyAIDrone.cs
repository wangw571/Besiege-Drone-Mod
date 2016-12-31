using System;
using spaar.ModLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Besiege_Drone_Mod
{
    public class FullyAIDrone : DroneStandardConputingScript
    {
        int FUcounter = 0;
        int MissileGuidanceModeInt = 0;
        int size = 1;
        float 精度 = 0.02f;
        int RotatingSpeed = 1;
        float SphereSize = 15;
        float MinimumAccelerationSqrToTakeDamage;

        void FixedUpdate()
        {
            HPCalculation(MinimumAccelerationSqrToTakeDamage);


            ++FUcounter;
            if (FUcounter >= 1000)
            {
                FUcounter = 0;
                //Switch Target
            }
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
                Vector3 LocalTargetDirection = this.transform.TransformPoint(RelativeAverageOfPoints(IDS.IncomingPositions, SphereSize));

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
                    TargetDirection = (getCorrTorque(this.transform.forward, LocalTargetDirection - this.transform.position * 1, this.GetComponent<Rigidbody>(), 0.01f * size) * Mathf.Rad2Deg).normalized;
                    if (Vector3.Angle(transform.forward, LocalTargetDirection - this.transform.position * 1) < 105)
                    {
                        GetComponent<Rigidbody>().angularVelocity = (TargetDirection * RotatingSpeed);
                    }
                }
            }
            前一帧速度 = this.GetComponent<Rigidbody>().velocity;
        }
    }
}
