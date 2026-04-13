using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemImprovedCollisions : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        Item item;
        ModuleImprovedCollisions module;
        Rigidbody body;

        protected void Awake()
        {
            item = GetComponent<Item>();
            module = item.data.GetModule<ModuleImprovedCollisions>();
            body = GetComponent<Rigidbody>();
        }

        protected override void ManagedUpdate()
        {
            if (!ModSettings.bItemImprovedCollisions) return;

            if (body.velocity.magnitude >= ModSettings.fItemImprovedCollisionsThreshold)
            {
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            else
            {
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }
    }
}
