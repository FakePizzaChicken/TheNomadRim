using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace TheNomadRim
{
    public class ModuleBlasterBolt : ItemModule
    {
        public float[] color = { 1, 0, 0, 1 };
        public float[] coreColorGO = { 1, 0, 0, 1 };
        public float[] colorGO = { 1, 0, 0, 1 };
        public float[] lightColor = { 1, 0, 0, 1 };

        public bool isStun = false;
        public bool useGravity = false;
        public bool disintegrate = false;

        public int bounces = 0;

        public float baseDamage = 25f;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ProjectileBlasterBolt>();
        }
    }

    public class BlasterBoltData : CustomData
    {
        public bool useGravity;
        public bool isStun;
        public bool disintegrate;
        public int bounces;

        public float boltSizeMultiplier = 1f;

        public float damageMultiplier;

        public string disintegrateEffectID;
        public EffectData disintegrateEffect;

        public string impactEffectID;
        public EffectData impactEffect;

        public float blastRadius = 0f;
        public float blastRadiusDamage = 0f;
        public string blastRadiusStatusEffect = "";
        public float blastRadiusStatusEffectDuration = 2f;
        public float blastRadiusForce = 0f;
        public bool removeLimbs = false;
        public int maxLimbLimit = 4;

        public bool overrideColorData = false;
        public float[] color = { 1, 0, 0, 1 };
        public float[] coreColorGO = { 1, 0, 0, 1 };
        public float[] colorGO = { 1, 0, 0, 1 };
        public float[] lightColor = { 1, 0, 0, 1 };

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            if (!string.IsNullOrEmpty(disintegrateEffectID)) disintegrateEffect = Catalog.GetData<EffectData>(disintegrateEffectID);
            if (!string.IsNullOrEmpty(impactEffectID)) impactEffect = Catalog.GetData<EffectData>(impactEffectID);
        }

        public override void ReleaseAddressableAssets()
        {
            base.ReleaseAddressableAssets();
            if (disintegrateEffect != null) Catalog.ReleaseAsset(disintegrateEffect);
        }
    }
}
