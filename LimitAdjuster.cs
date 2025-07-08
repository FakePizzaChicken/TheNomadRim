using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TheNomadRim
{
    public class LimitAdjuster : ThunderScript
    {
        public override void ScriptEnable()
        {
            base.ScriptEnable();
        }

        public override void ScriptUpdate()
        {
            base.ScriptUpdate();

            UniversalRenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            if (pipeline)
            {
                pipeline.maxAdditionalLightsCount = ModSettings.iMaxLights;
            }

            if (GameManager.options.physicTimeStep != (ModSettings.bImprovedPhysics ? TimeManager.PhysicTimeStep.Halved : TimeManager.PhysicTimeStep.Default))
            {
                GameManager.options.physicTimeStep = ModSettings.bImprovedPhysics ? TimeManager.PhysicTimeStep.Halved : TimeManager.PhysicTimeStep.Default;
                GameManager.options.Apply();
            }
        }

    }
}
