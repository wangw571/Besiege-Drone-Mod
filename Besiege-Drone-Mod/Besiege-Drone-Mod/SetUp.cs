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
        public override Version Version { get; } = new Version("0.21");
        protected Block Block = new Block()
            ///模块ID
            .ID(575)

            ///模块名称
            //.BlockName("Tracking Computer I")
            .BlockName("meow I")

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
            .ShowCollider(true)

            ///碰撞器
            .CompoundCollider(new List<ColliderComposite> {
                ColliderComposite.Mesh("zDrone.obj",Vector3.one,Vector3.forward * 3f,Vector3.zero),
            })

            ///你的模块是不是可以忽视强搭
            //.IgnoreIntersectionForBase()

            ///载入资源
            .NeededResources(null)

            ///连接点
            .AddingPoints(new List<AddingPoint> {
                               new BasePoint(true, false)         //底部连接点。第一个是指你能不能将其他模块安在该模块底部。第二个是指这个点是否是在开局时粘连其他链接点
                                                .Motionable(true,true,true) //底点在X，Y，Z轴上是否是能够活动的。
                                                .SetStickyRadius(0f),
            new AddingPoint(new Vector3(0f, 0.2f, 1.5f), new Vector3(-180f, 00f, 360f),true).SetStickyRadius(0.3f)
            });
        public override void OnLoad()
        {
            // Your initialization code here
            LoadBlock(Block);
        }

        public override void OnUnload()
        {
            // Your code here
            // e.g. save configuration, destroy your objects if CanBeUnloaded is true etc
        }
    }

    public class DroneControlBlockBehavior : BlockScript
    {
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

        GameObject DetectiveSphere;
        public override void SafeAwake()
        {
            Activation = new MKey("Activate Spawning Drones", "Activate", KeyCode.P);
            Activation.DisplayInMapper = false;
            Engage = new MKey("Engage", "Engage", KeyCode.T);
            ForceEngage = new MKey("Forced Engege", "FEngage", KeyCode.X);
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
            DroneTag.Value = (int)DroneTag.Value;
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
            }
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
    }


}
