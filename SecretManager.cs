using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using ThunderRoad;
using UnityEngine;

namespace TheNomadRim
{
    public class SecretManager : ThunderScript
    {
        public static List<string> unlockedSecrets = new List<string>();
        public static List<string> allSecrets = new List<string>();

        private static string tnrDataPath = Path.Combine(Application.persistentDataPath, "UserData", "TheNomadRim");
        private static string unlocksFile = Path.Combine(tnrDataPath, "UNLOCKS.TNR");

        public static AudioContainer secretDiscoverSFX;

        public static void UpdateUnlocksList()
        {
            unlockedSecrets.Clear();
            if (File.Exists(unlocksFile))
            {
                StreamReader reader = new StreamReader(unlocksFile);
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        unlockedSecrets.Add(line);
                    }
                }
                reader.Close();
            }

            UpdateSecretsProgress();
        }
        public static void DumpUnlocks()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(unlocksFile, false))
                {
                    foreach (var item in unlockedSecrets)
                    {
                        writer.WriteLine(item);
                    }
                }
                UpdateSecretsProgress();
            }
            catch (System.Exception e)
            {
                DebugService.LogError($"Failed to save secrets: {e.Message}");
            }
        }

        public static void ClearSecrets()
        {
            unlockedSecrets.Clear();
            if (File.Exists(unlocksFile)) File.Delete(unlocksFile);

            UpdateSecretsProgress();
        }

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);

            if (!Directory.Exists(tnrDataPath))
            {
                Directory.CreateDirectory(tnrDataPath);
            }

            Catalog.LoadAssetAsync<AudioContainer>("PC.TheNomadRim.Sound.SecretManager.Discover", x => { secretDiscoverSFX = x; }, "audio");

            UpdateUnlocksList();
        }

        public static void UpdateSecretsProgress()
        {
            if (LevelModuleEbonHawk.secretsDisplay != null)
            {
                var secretsDisplay = LevelModuleEbonHawk.secretsDisplay;

                secretsDisplay.UpdateAll();
                secretsDisplay.UpdateSecretsCounter();
            }
        }
    }

    public class ItemModuleSecret : ItemModule
    {
        public string hint = "A mysterious item that holds a secret";

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.GetOrAddComponent<ItemSecret>();
        }

        public override void OnItemDataRefresh(ItemData data)
        {
            base.OnItemDataRefresh(data);
            if (!SecretManager.allSecrets.Contains(data.id)) SecretManager.allSecrets.Add(data.id);
        }
    }

    public class ItemSecret : ThunderBehaviour
    {
        private Item item;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            item.OnGrabEvent += Item_OnGrabEvent;
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            if (SecretManager.unlockedSecrets.Contains(item.itemId)) return;

            SecretManager.unlockedSecrets.Add(item.itemId);
            SecretManager.DumpUnlocks();

            GameObject sfxObj = new GameObject("DiscoverSFX");
            sfxObj.transform.position = item.transform.position;

            AudioSource audioSource = sfxObj.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);

            StartCoroutine(PlayDiscover(audioSource));
        }

        private IEnumerator PlayDiscover(AudioSource source)
        {
            if (source != null)
            {
                Util.PlaySound(source, SecretManager.secretDiscoverSFX);

                while (source.isPlaying)
                {
                    yield return null;
                }

                Destroy(source);
            }

            yield return null;
        }
    }
}
