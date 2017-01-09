using System;
using spaar.ModLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Blocks
{
    public class TimedSelfDestruct : MonoBehaviour
    {
        float timer = 0;
        void FixedUpdate()
        {
            ++timer;
            if (timer > 260)
            {
                Destroy(this.gameObject);
            }
            if (this.GetComponent<TimedRocket>())
            {
                Destroy(this.GetComponent<TimedRocket>());
            }
        }
    }
}
