using System;
using spaar.ModLoader;
using UnityEngine;
using System.Collections.Generic;
using TheGuysYouDespise;
using System.Collections;
namespace Blocks
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
    /// 6.Climb to sky when is clear, set as direction + 500 Y, else keep curise flight and not ignoring incoming. 
    /// 
    /// Target Velocity: Use average velocity for 0.5 seconds
    /// Orbiting Target should be perpendicular to the target's velocity if it's moving. 
    /// </summary>

    public class DroneMod : BlockMod
    {
        public override string Name { get; } = "Drone_Deployment_Block";
        public override string DisplayName { get; } = "Drone Deployment Block";
        public override string Author { get; } = "wang_w571";
        public override Version Version { get; } = new Version("0.53");
        protected Block Drone = new Block()
            ///模块ID
            .ID(575)

            ///模块名称
            .BlockName("Drone I")

            ///模型信息
            .Obj(new List<Obj> { new Obj("zDrone.obj", //Obj
                                         "zDrone.png", //贴图 
                                         new VisualOffset(Vector3.one, //Scale
                                                          Vector3.forward * 3f, //Position
                                                          new Vector3(-90f, 0f, 0f)))//Rotation
            })

            ///在UI下方的选模块时的模样
            .IconOffset(new Icon(new Vector3(1f, 1f, 1f),  //Scale
                                 new Vector3(-0.11f, -0.13f, 0.00f),  //Position
                                 new Vector3(350f, 150f, 250f))) //Rotation

            ///没啥好说的。
            .Components(new Type[] {
                                    typeof(FullyAIDrone),
            })

            ///给搜索用的关键词
            .Properties(new BlockProperties().SearchKeywords(new string[] {
                                                             "Drone",
                                                             "无人机",
                                                             "靶机",
                                                             "Target",
                                                             "War",
                                                             "Weapon"
                                             })
            )
            ///质量
            .Mass(2f)

            ///是否显示碰撞器（在公开你的模块的时候记得写false）a
            .ShowCollider(false)

            ///碰撞器
            .CompoundCollider(new List<ColliderComposite> {
                ColliderComposite.Mesh("zDroneColl.obj",Vector3.one,Vector3.forward * 3f,Vector3.zero)
            })

            ///你的模块是不是可以忽视强搭
            //.IgnoreIntersectionForBase()

            ///载入资源
            .NeededResources(new List<NeededResource>
            {
                //new NeededResource(ResourceType.Mesh,"zDroneColl.obj")
                                new NeededResource(ResourceType.Texture,"zDroneBump.png")


            }
            )

            ///连接点
            .AddingPoints(new List<AddingPoint> {
                               new BasePoint(true, false)         //底部连接点。第一个是指你能不能将其他模块安在该模块底部。第二个是指这个点是否是在开局时粘连其他链接点
                                                .Motionable(true,true,true) //底点在X，Y，Z轴上是否是能够活动的。
                                                .SetStickyRadius(0f),
            new AddingPoint(new Vector3(0f, 0.2f, 1.5f), new Vector3(-180f, 00f, 360f),true).SetStickyRadius(0.3f)
            });

        protected Block ControlBlock = new Block()
            .ID(576)
            .BlockName("Drone Controller Block")
            .Obj(new List<Obj> { new Obj("DroneColtroller.obj", //Obj
                                         "DroneController.png", //贴图
                                         new VisualOffset(new Vector3(1f, 1f, 1f), //Scale
                                                          new Vector3(0f, 0f, 0f), //Position
                                                          new Vector3(0f, 0f, 0f)))//Rotation
            })
            ///在UI下方的选模块时的模样
            .IconOffset(new Icon(new Vector3(1.30f, 1.30f, 1.30f),  //Scale
                                 new Vector3(-0.11f, -0.13f, 0.00f),  //Position
                                 new Vector3(45f, 45f, 45f))) //Rotation
            .Components(new Type[] { typeof(DroneControlBlockBehavior), })

            ///给搜索用的关键词
            .Properties(new BlockProperties().SearchKeywords(new string[] {
                                                             "Drone",
                                                             "控制",
                                                             "Spawner",
                                             }))
            .Mass(0.5f)
            .ShowCollider(false)
            .CompoundCollider(new List<ColliderComposite> { new ColliderComposite(new Vector3(1, 1, 1), new Vector3(0f, 0f, 0.5f), new Vector3(0f, 0f, 0f)) })
            .NeededResources(null
            )
            .AddingPoints(new List<AddingPoint> {
                               (AddingPoint)new BasePoint(true, true)
                                                .Motionable(false,false,false)
                                                .SetStickyRadius(0.5f),
            });
        public override void OnLoad()
        {
            // Your initialization code here
            LoadBlock(Drone);
            LoadBlock(ControlBlock);
        }

        public override void OnUnload()
        {
            // Your code here
            // e.g. save configuration, destroy your objects if CanBeUnloaded is true etc
        }
    }

    public class DroneControlBlockBehavior : BlockScript
    {
        private RaycastHit hitt;

        MKey Activation;
        MKey Engage;
        MKey Recall;
        MKey ForceEngage;
        MMenu DroneAIType;
        MMenu DroneSize;
        MMenu Difficulty;
        MSlider OrbitRadius;
        //MMenu DroneWeapon;
        MSlider DroneAmount;
        MToggle ContinousSpawn;
        public MSlider DroneTag;

        public List<FullyAIDrone> AIDroneList;
        public List<Vector3> RelativeLeavePositions;
        public List<Boolean> AIDroneReachedOriginalPosition;

        public bool Engaging = false;

        GameObject DetectiveSphere;
        GameObject Target;
        public override void SafeAwake()
        {
            Engage = AddKey("Engage", "Engage", KeyCode.T);
            ForceEngage = AddKey("Forced Engege", "FEngage", KeyCode.X);
            Recall = AddKey("Recall", "Rc", KeyCode.R);
            //DroneSize = new MMenu("SizeType", 0, new List<string>() { "Heavt", "Medium", "Light" });
            //Difficulty = new MMenu("Difficulty", 0, new List<string>() { "Aggressive", "Defensive", "For Practice" });
            //Aggressive: To all moving items|Defensive: Only to aggressive blocks|For Practice: Flying around, keeping radar function, 
            OrbitRadius = AddSlider("Orbit Radius", "OrbitRadius", 15, 5, 200);
            //DroneAmount = new MSlider("Drone Amount", "Amount", 3, 1, 15);
            //ContinousSpawn = new MToggle("Spawn Drones\r\n after losing", "CSpawn", false);
            DroneTag = AddSlider("Drone Tag", "Tag", 0, 0, 100);

            AIDroneList = new List<FullyAIDrone>();
        }
        public override void OnSave(XDataHolder data)
        {
            SaveMapperValues(data);
        }
        public override void OnLoad(XDataHolder data)
        {
            LoadMapperValues(data);
            if (data.WasSimulationStarted) return;
        }
        protected override void BuildingUpdate()
        {
            DroneTag.Value = (int)DroneTag.Value;
            /*if (DroneAIType.Value == 0)
            {
                Engage.DisplayInMapper = true;
                ForceEngage.DisplayInMapper = true;
                OrbitRadius.DisplayInMapper = true;
            }
            else
            {
                Engage.DisplayInMapper = false;
                ForceEngage.DisplayInMapper = false;
                OrbitRadius.DisplayInMapper = false;
            }*/
        }
        protected override void OnSimulateFixedStart()
        {
            /* for (int i = 0; i < DroneAmount.Value; ++i)
             {
                 if (DroneAIType.Value == 1)
                 {
                     if (Difficulty.Value == 0)
                     {
                         GameObject OneDrone = new GameObject(DroneTag.Value.ToString());
                         //Collider, renderer, meshfilter things
                         FullyAIDrone Script = OneDrone.AddComponent<FullyAIDrone>();
                         Script.Parent = this;
                         Script.SetUpHP(100000);
                     }
                 }
             }*/
        }
        protected override void OnSimulateUpdate()
        {
            //Debug.Log(AIDroneList.Count);
            if (Engage.IsPressed)
            {
                RaycastHit[] rhs = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition), float.PositiveInfinity);
                if (rhs.Length != 0)
                {
                    foreach (RaycastHit hit in rhs)
                    {
                        if (hit.transform.position != this.transform.position && hit.collider.attachedRigidbody != null && !hit.collider.isTrigger)
                        {
                            Target = hit.transform.gameObject;
                            if (Target.GetComponentInParent<MachineTrackerMyId>() || this.name.Contains(("IsCloaked")))
                            {
                                if (Target.GetComponentInParent<MachineTrackerMyId>().gameObject.name.Contains("IsCloaked") || this.name.Contains(("IsCloaked")))
                                {
                                    Target = null;
                                    continue;
                                }
                            }
                            break;
                        }
                    }
                }

                //if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitt, float.PositiveInfinity))
                //{
                //    if (hitt.transform.position != this.transform.position && hitt.collider.attachedRigidbody != null)
                //    {
                //        Target = hitt.transform.gameObject;
                //        if (Target.GetComponentInParent<MachineTrackerMyId>() || this.name.Contains(("IsCloaked")))
                //        {
                //            if (Target.GetComponentInParent<MachineTrackerMyId>().gameObject.name.Contains("IsCloaked") || this.name.Contains(("IsCloaked")))
                //            {
                //                Target = null;
                //            }
                //        }
                //    }
                //}
                foreach (FullyAIDrone FAD in AIDroneList)
                {
                    FAD.currentTarget = Target;
                    FAD.IgnoreIncoming = false;
                }
            }
            if (ForceEngage.IsPressed)
            {
                RaycastHit[] rhs = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition), float.PositiveInfinity);
                if (rhs.Length != 0)
                {
                    foreach (RaycastHit hit in rhs)
                    {
                        if (hit.transform.position != this.transform.position && hit.collider.attachedRigidbody != null && !hit.collider.isTrigger)
                        {
                            Target = hit.transform.gameObject;
                            if (Target.GetComponentInParent<MachineTrackerMyId>() || this.name.Contains(("IsCloaked")))
                            {
                                if (Target.GetComponentInParent<MachineTrackerMyId>().gameObject.name.Contains("IsCloaked") || this.name.Contains(("IsCloaked")))
                                {
                                    Target = null;
                                    continue;
                                }
                            }
                            break;
                        }
                    }
                }

                foreach (FullyAIDrone FAD in AIDroneList)
                {
                    FAD.currentTarget = Target;
                    FAD.IgnoreIncoming = true;
                }
            }
            if (Recall.IsPressed)
            {
                if (Engaging)
                {
                    foreach (FullyAIDrone FAD in AIDroneList)
                    {
                        FAD.currentTarget = null;
                        FAD.IgnoreIncoming = false;
                        FAD.IAmEscapingOrReturning = true;
                        FAD.targetPoint = RelativeLeavePositions[AIDroneList.IndexOf(FAD)];
                    }
                }
            }
        }

        protected override void OnSimulateFixedUpdate()
        {

            //FAD.targetPoint = RelativeLeavePositions[AIDroneList.IndexOf(FAD)];
            if (!Engaging)
            {
                RelativeLeavePositions = new List<Vector3>();
                foreach (FullyAIDrone FAD in AIDroneList)
                {
                    RelativeLeavePositions.Add(this.transform.InverseTransformPoint(FAD.transform.position));
                }
            }
        }

        public Vector3 PleaseGiveMeNewOrbitPoint(Vector3 NowPoistion, Vector3 MyVeloDirection, bool GiveMeRandom)
        {
            Vector3 Relatived = this.transform.InverseTransformPoint(NowPoistion);
            Vector3 Returner = Vector3.zero;
            float DroneRelativeAngleX = Vector3.Angle(transform.forward, new Vector3(Relatived.x, 0, Relatived.z));
            float DroneRelativeAngleY = Vector3.Angle(transform.forward, new Vector3(0, Relatived.y, Relatived.z));
            if (!GiveMeRandom)
            {
                Vector3 one = EulerToDirection(DroneRelativeAngleX + 15, DroneRelativeAngleY + 15) * OrbitRadius.Value;
                Vector3 two = EulerToDirection(DroneRelativeAngleX + 15, DroneRelativeAngleY - 15) * OrbitRadius.Value;
                Vector3 three = EulerToDirection(DroneRelativeAngleX - 15, DroneRelativeAngleY + 15) * OrbitRadius.Value;
                Vector3 four = EulerToDirection(DroneRelativeAngleX - 15, DroneRelativeAngleY - 15) * OrbitRadius.Value;
                //Vector3.Min(Vector3.Min(Returner - one, Returner - two), Vector3.Min(Returner - three, Returner - four));
                if(Vector3.SqrMagnitude((Relatived + MyVeloDirection) - four) > Vector3.SqrMagnitude((Relatived + MyVeloDirection) - three))
                {
                    Returner = three;
                }
                else if(Vector3.SqrMagnitude((Relatived + MyVeloDirection) - three) > Vector3.SqrMagnitude((Relatived + MyVeloDirection) - two))
                {
                    Returner = two;
                }
                else if (Vector3.SqrMagnitude((Relatived + MyVeloDirection) - two) > Vector3.SqrMagnitude((Relatived + MyVeloDirection) - one))
                {
                    Returner = one;
                }
                else if (Vector3.SqrMagnitude((Relatived + MyVeloDirection) - one) > Vector3.SqrMagnitude((Relatived + MyVeloDirection) - four))
                {
                    Returner = four;
                }
                //Returner = EulerToDirection(UnityEngine.Random.value * 360 - 180, 15) * OrbitRadius.Value;
            }
            else
            {
                Returner = EulerToDirection(UnityEngine.Random.value * 360 - 180, UnityEngine.Random.value * 720 - 360) * OrbitRadius.Value;
            }

            return Returner;
        }
        Vector3 EulerToDirection(float Elevation, float Heading)
        {
            float elevation = Elevation * Mathf.Deg2Rad;
            float heading = Heading * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(elevation) * Mathf.Sin(heading), Mathf.Sin(elevation), Mathf.Cos(elevation) * Mathf.Cos(heading));
        }
        protected void LogFo()
        {
            Debug.Log("fofo");
        }
    }


}
