using System;
using spaar.ModLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Blocks
{
    public class FullyAIDrone : DroneStandardConputingScript
    {
        bool IAmSwitching;
        bool IAmEscaping;
        int FUcounter = 0;
        int MissileGuidanceModeInt = 0;
        int RotatingSpeed = 1;
        float SphereSize = 15;
        float MinimumAccelerationSqrToTakeDamage = 0.2f;
        int AIDifficultyValue;

        MKey Activation;
        MKey Engage;
        MKey ForceEngage;
        MMenu DroneAIType;
        MMenu DroneSize;
        MMenu Difficulty;
        MSlider OrbitRadius;
        MMenu DroneWeapon;
        MSlider DroneAmount;
        MToggle ContinousSpawn;
        MSlider DroneTag;
        GameObject PositionIndicator;
        public override void SafeAwake()
        {
            Activation = new MKey("Activate Spawning Drones", "Activate", KeyCode.P);
            Activation.DisplayInMapper = false;
            //Engage = new MKey("Engage", "Engage", KeyCode.T);
            //ForceEngage = new MKey("Forced Engege", "FEngage", KeyCode.X);
            DroneAIType = new MMenu("AIType", 0, new List<string>() { "Assistantnce", "Computer Controlled" });
            DroneSize = new MMenu("SizeType", 0, new List<string>() { "Heavt", "Medium", "Light" });
            Difficulty = new MMenu("Difficulty", 0, new List<string>() { "Aggressive", "Defensive", "For Practice" });
            //Aggressive: To all moving items|Defensive: Only to aggressive blocks|For Practice: Flying around, keeping radar function, 
            OrbitRadius = new MSlider("Orbit Radius", "OrbitRadius", 15, 5, 200);
            DroneAmount = new MSlider("Drone Amount", "Amount", 3, 1, 15);
            ContinousSpawn = new MToggle("Spawn Drones\r\n after losing", "CSpawn", false);
            DroneTag = new MSlider("Drone Tag", "Tag", 0, 0, 100);
        }
        protected override void BuildingUpdate()
        {
            /*DroneTag.Value = (int)DroneTag.Value;
            if (DroneAIType.Value == 0)
            {
                Activation.DisplayInMapper = false;
                Engage.DisplayInMapper = true;
                ForceEngage.DisplayInMapper = true;
                OrbitRadius.DisplayInMapper = true;
                Difficulty.DisplayInMapper = false;
            }
            else
            {
                Activation.DisplayInMapper = true;
                Engage.DisplayInMapper = false;
                ForceEngage.DisplayInMapper = false;
                OrbitRadius.DisplayInMapper = false;
                Difficulty.DisplayInMapper = true;
            }*/
        }

        protected override void OnSimulateFixedStart()
        {
            IAmSwitching = true;
            TargetSelector();
            Shooter = Instantiate(PrefabMaster.BlockPrefabs[11].gameObject);
            Shooter.transform.parent = this.transform;
            Shooter.transform.position = this.transform.position;
            Shooter.transform.rotation = this.transform.rotation;
            Destroy(Shooter.GetComponent<ConfigurableJoint>());
            Shooter.GetComponent<Rigidbody>().isKinematic = true;
            Shooter.GetComponent<Rigidbody>().mass = 0;
            Destroy(Shooter.GetComponent<Collider>());
            炮弹速度 = 5 * 58;

            MeshCollider MC = this.transform.GetComponentInChildren<MeshCollider>();
            MC.material.dynamicFriction = 0;
            MC.material.staticFriction = 0;

            MyPrecision = 0.25f;
            MySize = 1;
            精度 = 0.25f;
            size = 1;
            SetUpHP(1350);
            RotatingSpeed = 5;
            PositionIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(PositionIndicator.GetComponent<Rigidbody>());
            DestroyImmediate(PositionIndicator.GetComponent<Collider>());
        }

        protected override void OnSimulateFixedUpdate()
        {
            if (HitPoints <= 0)
            {
                return;
            }
            Shooter.transform.localEulerAngles = Vector3.right * 270;
            Shooter.transform.localPosition = Vector3.up * 0.5f;
            /*Shooter.GetComponent<Rigidbody>().mass = 0;
            Shooter.GetComponent<Rigidbody>().isKinematic = true;
            Shooter.GetComponent<Collider>().isTrigger = true;*/
            CB = Shooter.GetComponent<CanonBlock>();
            CB.Sliders[0].Value = 5;

            Vector3 RelativeVelo = this.transform.InverseTransformDirection(this.rigidBody.velocity);
            this.rigidBody.AddRelativeForce(new Vector3(RelativeVelo.x * -15, RelativeVelo.y * -15, 0));
            HPCalculation(MinimumAccelerationSqrToTakeDamage);
            PositionIndicator.transform.position = targetPoint;
            this.rigidbody.AddForce(this.transform.forward * 40 * this.rigidbody.mass);//Need calculate size

            if (HitPoints <= (this.rigidBody.velocity - targetVeloAveraged).sqrMagnitude)
            {
                IgnoreIncoming = true;
            }

            ++FUcounter;
            if (!IAmEscaping)
            {
                if (FUcounter >= 1000)
                {
                    FUcounter = 0;
                    IAmSwitching = true;
                    TargetSelector();
                }
                else if (FUcounter % 50 == 0)
                {
                    Vector3 velo = currentTarget.GetComponent<Rigidbody>().velocity;
                    targetVeloAveraged = Vector3.Lerp(targetVeloRecorder, velo, 0.5f);
                    targetVeloRecorder = velo;
                }
            }
            else if (this.transform.InverseTransformPoint(targetPoint).sqrMagnitude <= 100)
            {
                IAmEscaping = false;
                FUcounter = 0;
                IAmSwitching = false;
                TargetSelector();
            }

            if(FUcounter % 350 == 0)
            {
                if((PreviousPosition - this.transform.position).sqrMagnitude <= 425)
                {

                    IAmEscaping = false;
                    FUcounter = 0;
                    IAmSwitching = false;
                    TargetSelector();
                    this.rigidBody.AddRelativeForce(new Vector3(UnityEngine.Random.value * 1400 - 700, 500, UnityEngine.Random.value * 1400 - 700));
                }
                PreviousPosition = this.transform.position;
            }
            if (IncomingDetection == null)
            {
                IncomingDetection = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                IncomingDetection.layer = 15;

                Destroy(IncomingDetection.GetComponent<MeshRenderer>());
                Destroy(IncomingDetection.GetComponent<Rigidbody>());
                IDS = IncomingDetection.AddComponent<IncomingDetectionScript>();
                IDS.Main = this.GetComponent<Collider>();

            }
            Vector3 TargetDirection;

            if (IDS.SomethingInMyRange && !(IgnoreIncoming && !IAmEscaping))
            {
                Vector3 LocalTargetDirection = this.transform.TransformPoint(RelativeAverageOfPoints(IDS.IncomingPositions, SphereSize));
                PositionIndicator.transform.position = this.transform.TransformPoint(LocalTargetDirection);
                if (MissileGuidanceModeInt == 0)
                {
                    LocalTargetDirection = currentTarget.transform.position;
                    LocalTargetDirection = DroneDirectionIndicator(LocalTargetDirection,0);
                }

                //this.transform.rotation.SetFromToRotation(this.transform.forward, LocalTargetDirection);
                //Vector3 rooo = Vector3.RotateTowards(this.transform.forward, LocalTargetDirection - this.transform.position, RotatingSpeed * size, RotatingSpeed * size);
                //Debug.Log(LocalTargetDirection + "and" + this.transform.up + "and" + rooo);
                //this.transform.rotation = Quaternion.LookRotation(rooo);
                //LocalTargetDirection = new Vector3(LocalTargetDirection.x, LocalTargetDirection.y - this.transform.position.y, LocalTargetDirection.z);
                //float mag = (LocalTargetDirection.normalized - transform.forward.normalized).magnitude;

                TargetDirection = (getCorrTorque(this.transform.forward, LocalTargetDirection - this.transform.position * 1, this.GetComponent<Rigidbody>(), 0.01f * size) * Mathf.Rad2Deg).normalized;

                rigidBody.angularVelocity = (TargetDirection * RotatingSpeed);

            }
            else if (currentTarget != null)
            {
                if (currentTarget.GetComponent<Rigidbody>() && currentTarget.transform.position != this.transform.position)
                {
                    Vector3 LocalTargetDirection = currentTarget.transform.position;
                    targetPoint = currentTarget.transform.position;
                    PositionIndicator.transform.position = targetPoint;

                    if (MissileGuidanceModeInt == 0)
                    {
                        LocalTargetDirection = currentTarget.transform.position;
                        LocalTargetDirection = DroneDirectionIndicator(LocalTargetDirection,炮弹速度 + this.rigidBody.velocity.magnitude);
                    }
                    TargetDirection = (getCorrTorque(this.transform.forward, LocalTargetDirection - this.transform.position * 1, this.GetComponent<Rigidbody>(), 0.01f * size) * Mathf.Rad2Deg).normalized;
                    if (Vector3.Angle(transform.forward, LocalTargetDirection - this.transform.position * 1) < 105)
                    {
                        GetComponent<Rigidbody>().angularVelocity = (TargetDirection * RotatingSpeed);
                    }
                    if (FUcounter % 30 == 0)
                    {
                        CB.Shoot();
                        CB.alreadyShot = false;
                        if (!NotEvenHavingAJoint)
                        {
                            if (!currentTarget.GetComponent<ConfigurableJoint>())
                            {
                                currentTarget = null;
                            }
                        }
                        if (!NotEvenHavingAFireTag)
                        {
                            if (currentTarget.GetComponent<FireTag>().burning)
                            {
                                currentTarget = null;
                            }
                        }
                    }
                }
            }
            else if (IAmEscaping)
            {
                Vector3 LocalTargetDirection = targetPoint;
                PositionIndicator.transform.position = targetPoint;
                foreach (RaycastHit RH in Physics.RaycastAll(this.transform.position, targetPoint, targetPoint.magnitude))
                {
                    if (!RH.collider.isTrigger)
                    {
                        LocalTargetDirection = new Vector3(targetPoint.x, this.transform.position.y, targetPoint.z);
                        break;
                    }
                }
                TargetDirection = (getCorrTorque(this.transform.forward, LocalTargetDirection - this.transform.position * 1, this.GetComponent<Rigidbody>(), 0.01f * size) * Mathf.Rad2Deg).normalized;
                if (Vector3.Angle(transform.forward, LocalTargetDirection - this.transform.position * 1) < 105)
                {
                    GetComponent<Rigidbody>().angularVelocity = (TargetDirection * RotatingSpeed);
                }

            }
            else
            {
                IAmSwitching = true;
                TargetSelector();
            }

            前一帧速度 = this.GetComponent<Rigidbody>().velocity;
        }
        void TargetSelector()
        {
            List<MachineTrackerMyId> BBList = new List<MachineTrackerMyId>();
            List<int> ImportanceMultiplier = new List<int>();
            foreach (MachineTrackerMyId BB in FindObjectsOfType<MachineTrackerMyId>())
            {
                NotEvenHavingAFireTag = BB.gameObject.GetComponent<FireTag>() == null;
                int BBID = BB.myId;
                switch (AIDifficultyValue)
                {
                    case 0:
                        if (BBID == 23 || BBID == 525 || BBID == 526 || BBID == 540 || BBID == 519)//Bombs, Tracking Computers
                        {
                            if (NotEvenHavingAFireTag)
                            {
                                BBList.Add(BB);
                                ImportanceMultiplier.Add(15);
                                break;
                            }
                            else if (!BB.gameObject.GetComponent<FireTag>().burning)
                            {
                                BBList.Add(BB);
                                ImportanceMultiplier.Add(15);
                                break;
                            }
                        }
                        else if (BBID == 59 || BBID == 54)//Rocket & Grenade
                        {
                            BBList.Add(BB);
                            ImportanceMultiplier.Add(12);
                            break;
                        }
                        else if (BBID == 14 || BBID == 2 || BBID == 46 || BBID == 39)//Locomotion && Proplusion
                        {
                            if (currentTarget.GetComponent<ConfigurableJoint>() != null)
                                if (NotEvenHavingAFireTag)
                                {
                                    BBList.Add(BB);
                                    ImportanceMultiplier.Add(10);
                                    break;
                                }
                                else if (!BB.gameObject.GetComponent<FireTag>().burning)
                                {
                                    BBList.Add(BB);
                                    ImportanceMultiplier.Add(10);
                                    break;
                                }
                        }
                        else if (BBID == 26 || BBID == 55 || BBID == 52)//Propellers
                        {
                            BBList.Add(BB);
                            ImportanceMultiplier.Add(8);
                            break;
                        }
                        else if (BBID == 34 || BBID == 25 || BBID == 43 /**/  || BBID == 28 || BBID == 4 || BBID == 18 || BBID == 27 || BBID == 3 || BBID == 20)//Large Aero Blocks/Mechanic Blocks
                        {
                            if (currentTarget.GetComponent<ConfigurableJoint>() != null)
                                if (NotEvenHavingAFireTag)
                                {
                                    BBList.Add(BB);
                                    ImportanceMultiplier.Add(4);
                                    break;
                                }
                                else if (!BB.gameObject.GetComponent<FireTag>().burning)
                                {
                                    BBList.Add(BB);
                                    ImportanceMultiplier.Add(4);
                                    break;
                                }
                        }
                        else if (BBID == 35 || BBID == 16 || BBID == 42 /**/ || BBID == 40 || BBID == 60 || BBID == 38 || BBID == 51 /**/ || BBID == 1 || BBID == 15 || BBID == 41 || BBID == 5)//Structure Block
                        {
                            if (currentTarget.GetComponent<ConfigurableJoint>() != null)
                                if (NotEvenHavingAFireTag)
                                {
                                    BBList.Add(BB);
                                    ImportanceMultiplier.Add(1);
                                    break;
                                }
                                else if (!BB.gameObject.GetComponent<FireTag>().burning)
                                {
                                    BBList.Add(BB);
                                    ImportanceMultiplier.Add(1);
                                    break;
                                }
                        }
                        break;

                    case 1:
                        if (BBID == 23 || BBID == 525 || BBID == 526 || BBID == 540 || BBID == 519)//Bombs, Tracking Computers
                        {
                            if (NotEvenHavingAFireTag)
                            {
                                BBList.Add(BB);
                                ImportanceMultiplier.Add(15);
                                break;
                            }
                            else if (!BB.gameObject.GetComponent<FireTag>().burning)
                            {
                                BBList.Add(BB);
                                ImportanceMultiplier.Add(15);
                                break;
                            }
                        }
                        else if (BBID == 59 || BBID == 54)//Rocket & Grenade
                        {
                            BBList.Add(BB);
                            ImportanceMultiplier.Add(12);
                            break;
                        }
                        break;
                    default:
                        /*this.currentTarget = null;
                        this.targetPoint = new Vector3(UnityEngine.Random.value * 1400 - 700, 500, UnityEngine.Random.value * 1400 - 700);
                        IAmEscaping = true;*/
                        return;
                }
            }
            foreach (MachineTrackerMyId BB2 in BBList.ToArray())
            {
                bool IgnoreAttitude = false;
                bool IgnoreDistance = false;
                int RemoveId;
                if (!IAmSwitching)
                {
                    IgnoreAttitude = true;
                    IgnoreDistance = true;
                }
                if (!IgnoreAttitude)
                {
                    if (BB2.gameObject.transform.position.y <= this.rigidBody.velocity.sqrMagnitude * 1.7f)
                    {
                        RemoveId = BBList.IndexOf(BB2);
                        BBList.RemoveAt(RemoveId);
                        ImportanceMultiplier.RemoveAt(RemoveId);
                        continue;
                    }
                }
                if (!IgnoreDistance)
                {
                    if (this.transform.InverseTransformPoint(BB2.gameObject.transform.position).sqrMagnitude <= this.rigidBody.velocity.sqrMagnitude * 2.8f)
                    {
                        RemoveId = BBList.IndexOf(BB2);
                        BBList.RemoveAt(RemoveId);
                        ImportanceMultiplier.RemoveAt(RemoveId);
                        continue;
                    }
                }
                if (BB2.gameObject == currentTarget)
                {
                    RemoveId = BBList.IndexOf(BB2);
                    BBList.RemoveAt(RemoveId);
                    ImportanceMultiplier.RemoveAt(RemoveId);
                    continue;
                }
            }
            if (BBList.Count == 0)
            {
                currentTarget = null;
                targetPoint = this.transform.TransformPoint(EulerToDirection(this.transform.eulerAngles.x, 45) * 200);
                if ((this.transform.position - targetPoint).sqrMagnitude <= 100 || targetPoint.y <= 45)
                {
                    this.targetPoint = new Vector3(UnityEngine.Random.value * 1400 - 700, 500, UnityEngine.Random.value * 1400 - 700);
                }
                Debug.Log(targetPoint);
                IAmEscaping = true;
            }
            else
            {
                foreach (MachineTrackerMyId BNB in BBList)
                {
                    int Index = BBList.IndexOf(BNB);
                    if (ImportanceMultiplier.Count == Index + 1)
                    {
                        this.currentTarget = BNB.gameObject;
                        break;
                    }
                    if (ImportanceMultiplier[Index + 1] > ImportanceMultiplier[Index])
                    {
                        continue;
                    }
                    else
                    {
                        this.currentTarget = BNB.gameObject;
                        NotEvenHavingAJoint = currentTarget.GetComponent<ConfigurableJoint>() == null;
                        NotEvenHavingAFireTag = currentTarget.GetComponent<FireTag>() == null;
                        if (currentTarget.GetComponent<ConfigurableJoint>() != null)
                        {
                            currentTarget.GetComponent<ConfigurableJoint>().breakForce = Mathf.Min(currentTarget.GetComponent<ConfigurableJoint>().breakForce, 45000);
                        }
                        break;
                    }
                }
            }
        }
    }
}
