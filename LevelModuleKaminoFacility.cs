using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using Unity.XR.CoreUtils;
using UnityEngine;
using static ThunderRoad.HandleRagdollData;

namespace TheNomadRim
{
    public class LevelModuleKaminoFacility : LevelModule
    {
        private Dictionary<GameObject, EmbryoData> embryoLookup = new Dictionary<GameObject, EmbryoData>();
        private List<ParticleSystem> rainSystems = new List<ParticleSystem>();
        private List<ParticleSystem> puddleSystems = new List<ParticleSystem>();

        public AudioContainer glassShatterSounds;
        public ItemSpawner secretItem;
        public int remainingEmbryos;

        private struct EmbryoData
        {
            public GameObject meshRoot;
            public ParticleSystem particles;
            public AudioSource sfx;
        }

        public override IEnumerator OnLoadCoroutine()
        {
            yield return Catalog.LoadAssetCoroutine<AudioContainer>("PC.TheNomadRim.Sound.GlassShatter", x => { glassShatterSounds = x; }, "audio");

            var rainRefs = level.customReferences.Find(x => x.name == "RainParticles")?.transforms;
            if (rainRefs != null)
                foreach (var t in rainRefs) rainSystems.Add(t.GetComponent<ParticleSystem>());

            var puddleRefs = level.customReferences.Find(x => x.name == "RainPuddles")?.transforms;
            if (puddleRefs != null)
                foreach (var t in puddleRefs) puddleSystems.Add(t.GetComponentInChildren<ParticleSystem>());

            var parents = level.customReferences.Find(x => x.name == "Embryos")?.transforms;
            if (!parents.IsNullOrEmpty())
            {
                foreach (var p in parents)
                {
                    var children = p.GetComponentsInChildren<Transform>(true);
                    foreach (var c in children)
                    {
                        if (c.name.Contains("kam_bldg_embryo"))
                        {
                            SetupEmbryo(c.gameObject);
                        }
                    }
                }
            }

            remainingEmbryos = embryoLookup.Count;
            secretItem = level.customReferences.Find(x => x.name == "SecretItem")?.transforms.FirstOrDefault()?.GetComponent<ItemSpawner>();

            SetRainIntensity(ModSettings.fKaminoRainIntensity);
        }

        private void SetupEmbryo(GameObject go)
        {
            var embryoVisual = go.GetNamedChild("Embryo");

            var proxy = embryoVisual.AddComponent<EmbryoProxy>();
            proxy.module = this;

            var data = new EmbryoData
            {
                meshRoot = embryoVisual,
                particles = go.GetNamedChild("GlassParticles").GetComponent<ParticleSystem>(),
                sfx = go.GetNamedChild("AudioSource").GetComponent<AudioSource>()
            };

            embryoLookup.Add(embryoVisual, data);
        }

        public void OnEmbryoHit(GameObject key)
        {
            if (embryoLookup.TryGetValue(key, out EmbryoData data))
            {
                data.sfx.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);
                data.sfx.pitch = UnityEngine.Random.Range(0.85f, 1.12f);
                Util.PlaySound(data.sfx, glassShatterSounds);

                data.particles.Play();
                data.meshRoot.SetActive(false);

                remainingEmbryos--;
                if (remainingEmbryos <= 0 && secretItem != null)
                {
                    secretItem.transform.position = Player.local.creature.transform.position + (Vector3.up * 0.5f);
                    secretItem.Spawn();
                }
                embryoLookup.Remove(key);
            }
        }

        public void SetRainIntensity(float intensity)
        {
            foreach (var ps in rainSystems)
            {
                var emission = ps.emission;
                emission.rateOverTime = intensity;
            }
            foreach (var ps in puddleSystems)
            {
                var emission = ps.emission;
                emission.rateOverTime = intensity * 0.2f;
            }
        }
    }

    public class EmbryoProxy : MonoBehaviour
    {
        public LevelModuleKaminoFacility module;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.sqrMagnitude > 4.0f)
            {
                module.OnEmbryoHit(this.gameObject);
            }
        }
    }
}