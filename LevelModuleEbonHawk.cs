using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Skill.Spell;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheNomadRim
{
    public class LevelModuleEbonHawk : LevelModule
    {
        Lever hyperSpaceLever;
        MeshRenderer hyperSpaceRenderer;
        MeshRenderer spaceRenderer;

        Light starLight;

        AudioSource hyperSpaceJumpSource;
        AudioSource hyperSpaceLoopSource;

        AudioSource weirdstarAmbience;
        AudioSource weirdstarAngry;

        Collider cockpitTrigger;

        AudioContainer enterAudio;
        AudioContainer exitAudio;
        AudioContainer loopAudio;

        List<Transform> weirdstars = new List<Transform>();

        Color currentGalaxyColor = Color.white;
        float currentGalaxyIntensity = 0f;
        float currentHyperSpeedIntensity = 0f;

        bool isLeverUp = false;
        float hyperSpeedScroll = 0f;

        public static SecretsDisplay secretsDisplay;
        public static TravelManager travelManager = new TravelManager();

        public override System.Collections.IEnumerator OnLoadCoroutine()
        {
            hyperSpaceLever = level.customReferences.Find(x => x.name == "HyperSpaceLever")?.transforms.FirstOrDefault()?.GetComponent<Lever>();
            hyperSpaceRenderer = level.customReferences.Find(x => x.name == "HyperSpaceRenderer")?.transforms.FirstOrDefault()?.GetComponent<MeshRenderer>();
            spaceRenderer = level.customReferences.Find(x => x.name == "SpaceRenderer")?.transforms.FirstOrDefault()?.GetComponent<MeshRenderer>();
            starLight = level.customReferences.Find(x => x.name == "StarLight")?.transforms.FirstOrDefault()?.GetComponent<Light>();

            hyperSpaceJumpSource = level.customReferences.Find(x => x.name == "HyperSpaceJumpSource")?.transforms.FirstOrDefault()?.GetComponent<AudioSource>();
            hyperSpaceLoopSource = level.customReferences.Find(x => x.name == "HyperSpaceLoopSource")?.transforms.FirstOrDefault()?.GetComponent<AudioSource>();

            weirdstars = level.customReferences.Find(x => x.name == "Weirdstar")?.transforms;
            cockpitTrigger = level.customReferences.Find(x => x.name == "CockpitTrigger")?.transforms.FirstOrDefault().GetComponent<Collider>();

            travelManager.travelUI = level.customReferences.Find(x => x.name == "TravelDestination")?.transforms.FirstOrDefault();
            travelManager.travelMaps = level.customReferences.Find(x => x.name == "TravelMaps")?.transforms;
            travelManager.levelView = level.customReferences.Find(x => x.name == "DestinationMap")?.transforms.FirstOrDefault();
            travelManager.Init();

            secretsDisplay = new SecretsDisplay(level.customReferences.Find(x => x.name == "SecretsDisplay")?.transforms.FirstOrDefault());

            var voices = level.customReferences.Find(x => x.name == "WeirdstarVoices")?.transforms;
            if (!voices.IsNullOrEmpty())
            {
                weirdstarAmbience = voices.FirstOrDefault()?.GetComponent<AudioSource>();
                if (voices.Count > 1) weirdstarAngry = voices[1]?.GetComponent<AudioSource>();
            }

            if (hyperSpaceJumpSource) hyperSpaceJumpSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);
            if (hyperSpaceLoopSource) hyperSpaceLoopSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);

            yield return Catalog.LoadAssetCoroutine<AudioContainer>("PC.TheNomadRim.Sounds.HyperspaceStart", x => { enterAudio = x; }, "audio");
            yield return Catalog.LoadAssetCoroutine<AudioContainer>("PC.TheNomadRim.Sounds.HyperspaceExit", x => { exitAudio = x; }, "audio");
            yield return Catalog.LoadAssetCoroutine<AudioContainer>("PC.TheNomadRim.Sounds.HyperspaceLoop", x => { loopAudio = x; }, "audio");
            yield return Catalog.LoadAssetCoroutine<AudioContainer>("PC.TheNomadRim.Sound.SecretManager.Spawn", x => { SecretsDisplay.itemSpawnContainer = x; }, "audio");
            yield return Catalog.LoadAssetCoroutine<AudioContainer>("PC.TheNomadRim.Sound.SecretManager.Locked", x => { SecretsDisplay.itemLockedContainer = x; }, "audio");

            hyperSpaceLever?.leverUpEvent.AddListener(() => OnHyperSpaceLeverUp());
            hyperSpaceLever?.leverDownEvent.AddListener(() => OnHyperSpaceLeverDown());

            RandomizeGalaxyColors();
            UpdateHyperSpace(currentHyperSpeedIntensity);
            SetupCockpitTrigger();

            starweirdManager = new StarweirdManager(level, weirdstars, weirdstarAmbience, weirdstarAngry, hyperSpaceLever, travelManager);

            EventManager.onLevelUnload += LevelUnload;

            yield return null;
        }

        private void LevelUnload(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                EventManager.onLevelUnload -= LevelUnload;
                return;
            }

            Player.characterData.SaveAsync(true);

            hyperSpaceLever?.leverUpEvent.RemoveListener(OnHyperSpaceLeverUp);
            hyperSpaceLever?.leverDownEvent.RemoveListener(OnHyperSpaceLeverDown);
        }

        private void OnHyperSpaceLeverUp()
        {
            if (!travelManager.hyperspaceAllowed) return;

            if (!isLeverUp)
            {
                Util.PlaySound(hyperSpaceJumpSource, enterAudio, stopPlaying: true);
                hyperSpaceLoopSource.volume = 0;
                Util.PlaySoundLooped(hyperSpaceLoopSource, loopAudio);
            }

            travelManager.SetTravelButtonState(false);
            isLeverUp = true;
        }

        private void OnHyperSpaceLeverDown()
        {
            if (isLeverUp)
            {
                Util.PlaySound(hyperSpaceJumpSource, exitAudio, stopPlaying: true);
                if (currentHyperSpeedIntensity >= 0.99f) RandomizeGalaxyColors();
                hyperSpaceLoopSource.Stop();
                travelManager.GetRandomLevel();
            }

            if (string.IsNullOrEmpty(travelManager.destinationLevel))
                starweirdManager?.OnNewGalaxy(currentGalaxyIntensity);

            isLeverUp = false;
        }

        public override void Update()
        {
            currentHyperSpeedIntensity = Mathf.MoveTowards(currentHyperSpeedIntensity, isLeverUp ? 1f : 0f, Time.deltaTime * 0.5f);
            if (hyperSpaceLoopSource) hyperSpaceLoopSource.volume = currentHyperSpeedIntensity;

            if (currentHyperSpeedIntensity > 0.01f && hyperSpaceRenderer != null)
            {
                hyperSpeedScroll += Time.deltaTime * 0.2f * currentHyperSpeedIntensity;
                Vector2 scroll = new Vector2(0, hyperSpeedScroll);
                hyperSpaceRenderer.material.SetTextureOffset("_BaseMap", scroll);
            }

            UpdateHyperSpace(currentHyperSpeedIntensity);
            starweirdManager?.Update(currentHyperSpeedIntensity, isLeverUp);
        }

        private void UpdateHyperSpace(float intensity)
        {
            Color targetHyperLight = Color.HSVToRGB(0.583f, 0.55f, 1f);

            float hdrPower = Mathf.Pow(2, currentGalaxyIntensity);
            Color targetSpaceColor = currentGalaxyColor * hdrPower;

            if (spaceRenderer) spaceRenderer.material.SetColor("_EmissionColor", targetSpaceColor);

            if (hyperSpaceRenderer)
            {
                if (intensity <= 0.0f && hyperSpaceRenderer.gameObject.activeSelf) hyperSpaceRenderer.gameObject.SetActive(false);
                else if (intensity > 0.0f && !hyperSpaceRenderer.gameObject.activeSelf) hyperSpaceRenderer.gameObject.SetActive(true);
                hyperSpaceRenderer.material.SetColor("_BaseColor", new Color(1, 1, 1, intensity));
            }

            if (starLight)
            {
                starLight.color = Color.Lerp(currentGalaxyColor, targetHyperLight, intensity);
                starLight.intensity = Mathf.Lerp(100f * (hdrPower * 0.5f), 1000f, intensity);
            }
        }

        private void RandomizeGalaxyColors()
        {
            Color newColor = UnityEngine.Random.ColorHSV(0f, 0.8f, 0.75f, 1f, 0.8f, 1f);
            float newIntensity = UnityEngine.Random.Range(-4.0f, 8.0f);
            currentGalaxyColor = newColor;
            currentGalaxyIntensity = newIntensity;

            if (spaceRenderer)
                spaceRenderer.material.SetTextureScale("_BaseMap", new Vector2(UnityEngine.Random.Range(0.05f, 1f), UnityEngine.Random.Range(0.05f, 1f)));
        }

        // --------------------- Starweird Manager ---------------------

        private StarweirdManager starweirdManager;

        public class StarweirdManager
        {
            private List<Transform> weirdstars;
            private AudioSource ambienceSource;
            private AudioSource angrySource;
            private Lever lever;
            private TravelManager travel;

            private Transform current;
            private Transform currentWaypoint;
            private Vector3 currentWaypointPos;
            private Vector3 originalScale;
            private Vector3 originalPos;
            private bool playerInCockpit = false;

            // Timings
            private float gazeTimer = 0f;
            private bool gazeAggroActive = false;

            private float approachDuration = 60f;
            private float approachTimer = 0f;
            private bool approachActive = false;
            private float gazeStartDistance = 0f;

            private enum ScaryPhase { None, Alarm, Hunting, Survived }
            private ScaryPhase scaryPhase = ScaryPhase.None;

            private GameObject alarmRoot;
            private Light alarmLight;
            private AudioSource alarmAudio;
            private float alarmTimer = 0f;
            private float alarmDuration = 0f;
            private float alarmLightPhase = 0f;

            private GameObject enemyRoot;
            private Transform enemyTransform;
            private NavMeshAgent navAgent;
            private float survivalTimer = 0f;

            private GameObject announcementRoot;
            private AudioSource announcementAudio;

            private LightmapData[] cachedLightmaps;
            private bool lightmapsRemoved = false;
            private Color cachedAmbientLight;
            private List<MeshRenderer> renderers = new List<MeshRenderer>();
            private List<Material> emissiveRenderers = new List<Material>();
            private List<GameObject> disabledOnBoarding = new List<GameObject>();

            public StarweirdManager(Level level, List<Transform> s, AudioSource ambience, AudioSource angry, Lever lev, TravelManager tm)
            {
                weirdstars = s;
                ambienceSource = ambience;
                angrySource = angry;
                lever = lev;
                travel = tm;

                alarmRoot = level.customReferences.Find(x => x.name == "AlarmResources")?.transforms.FirstOrDefault()?.gameObject;
                if (alarmRoot != null)
                {
                    alarmLight = alarmRoot.GetComponentInChildren<Light>(true);
                    alarmAudio = alarmRoot.GetComponentInChildren<AudioSource>(true);
                    alarmRoot.SetActive(false);
                }

                enemyRoot = level.customReferences.Find(x => x.name == "StarweirdEnemy")?.transforms.FirstOrDefault()?.gameObject;
                if (enemyRoot != null)
                {
                    enemyTransform = enemyRoot.transform;
                    enemyRoot.SetActive(false);
                }

                announcementRoot = level.customReferences.Find(x => x.name == "Announcement")?.transforms.FirstOrDefault()?.gameObject;
                if (announcementRoot != null)
                {
                    announcementAudio = announcementRoot.GetComponent<AudioSource>();
                    announcementRoot.SetActive(false);
                }

                var travelMapsRef = level.customReferences.Find(x => x.name == "TravelMaps");
                if (travelMapsRef != null)
                    foreach (var t in travelMapsRef.transforms)
                        if (t != null) disabledOnBoarding.Add(t.gameObject);

                var secretsBoardRef = level.customReferences.Find(x => x.name == "SecretsDisplay");
                if (secretsBoardRef != null && !secretsBoardRef.transforms.IsNullOrEmpty())
                    disabledOnBoarding.Add(secretsBoardRef.transforms.FirstOrDefault()?.gameObject);

                cachedLightmaps = LightmapSettings.lightmaps;
                cachedAmbientLight = RenderSettings.ambientLight;

                foreach (var mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>())
                    renderers.Add(mr);
            }

            public void OnNewGalaxy(float galaxyIntensity)
            {
                ResetAll();

                if (weirdstars == null || weirdstars.Count == 0) return;
                if (UnityEngine.Random.value > 0.1f) return;

                int id = UnityEngine.Random.Range(0, weirdstars.Count);
                current = weirdstars[id];
                originalScale = current.localScale;
                originalPos = current.position;

                currentWaypoint = current.Find("Waypoint");
                currentWaypointPos = currentWaypoint.position;

                float clamped = Mathf.Clamp(galaxyIntensity, 1f, 8f);
                approachDuration = Mathf.Lerp(60f, 480f, Mathf.InverseLerp(1f, 8f, clamped));
                approachTimer = 0f;
                approachActive = true;
                gazeTimer = 0f;
                gazeAggroActive = false;

                current.localScale = originalScale;
                current.gameObject.SetActive(true);
            }

            public void OnCockpitEntered() => playerInCockpit = true;
            public void OnCockpitExited() => playerInCockpit = false;

            public void Update(float hyperIntensity, bool leverUp)
            {
                switch (scaryPhase)
                {
                    case ScaryPhase.None: UpdatePhase(hyperIntensity, leverUp); break;
                    case ScaryPhase.Alarm: UpdateAlarmPhase(); break;
                    case ScaryPhase.Hunting: UpdateHuntPhase(hyperIntensity, leverUp); break;
                    case ScaryPhase.Survived: break;
                }
            }

            private void UpdatePhase(float hyperIntensity, bool leverUp)
            {
                if (current == null || !approachActive) return;

                float progress = approachTimer / approachDuration;
                bool isGlobal = progress >= 0.5f;

                if (!gazeAggroActive)
                    current.localScale = originalScale * 3f;

                if (ambienceSource != null)
                {
                    ambienceSource.spatialBlend = isGlobal ? 0f : 1f;
                    if (!ambienceSource.isPlaying) ambienceSource.Play();

                    if (isGlobal)
                        ambienceSource.volume = Mathf.InverseLerp(0.5f, 1f, progress);
                    else if (playerInCockpit)
                    {
                        Transform cam = Player.local?.head?.cam?.transform;
                        if (cam != null)
                        {
                            Vector3 dir = (current.position - cam.position).normalized;
                            float dot = Vector3.Dot(cam.forward, dir);
                            ambienceSource.volume = Mathf.Lerp(ambienceSource.volume, Mathf.Max(0f, dot), Time.deltaTime * 3f);
                        }
                    }
                    else ambienceSource.volume = 0f;
                }

                if (playerInCockpit && !gazeAggroActive)
                {
                    Transform cam = Player.local?.head?.cam?.transform;
                    if (cam != null)
                    {
                        Vector3 dir = (current.position - cam.position).normalized;
                        float dot = Vector3.Dot(cam.forward, dir);

                        if (dot > 0.9f)
                        {
                            gazeTimer += Time.deltaTime;
                            if (gazeTimer >= 3f)
                            {
                                gazeAggroActive = true;

                                gazeAggroActive = true;
                                gazeStartDistance = Vector3.Distance(current.position, currentWaypointPos);

                                if (ambienceSource) ambienceSource.Stop();
                                if (angrySource) { angrySource.loop = false; angrySource.Play(); }
                            }
                        }
                        else gazeTimer = Mathf.Max(0f, gazeTimer - Time.deltaTime);
                    }
                }

                if (gazeAggroActive)
                {
                    current.position = Vector3.MoveTowards(
                        current.position,
                        currentWaypointPos,
                        Time.deltaTime * (gazeStartDistance / 8f)
                    );

                    float waypointProgress = angrySource != null && angrySource.clip != null
                        ? Mathf.Clamp01(angrySource.time / angrySource.clip.length)
                        : Mathf.Clamp01(1f - (Vector3.Distance(current.position, currentWaypointPos) / Mathf.Max(gazeStartDistance, 0.01f)));

                    current.localScale = originalScale * Mathf.Lerp(3f, 7f, waypointProgress);

                    if (Vector3.Distance(current.position, currentWaypointPos) < 0.1f)
                    {
                        TriggerBoarding();
                        return;
                    }

                    if (angrySource != null && angrySource.clip != null && !angrySource.isPlaying)
                    {
                        gazeAggroActive = false;
                        gazeTimer = 0f;
                        if (ambienceSource) ambienceSource.Play();
                    }
                    else if (angrySource == null || angrySource.clip == null)
                    {
                        if (Vector3.Distance(current.position, currentWaypointPos) < 0.1f)
                        {
                            gazeAggroActive = false;
                            gazeTimer = 0f;
                            if (ambienceSource) ambienceSource.Play();
                        }
                    }
                }

                if (leverUp && hyperIntensity >= 0.99f)
                {
                    ResetAll();
                    return;
                }

                approachTimer += Time.deltaTime;

                if (approachTimer >= approachDuration)
                    TriggerBoarding();
            }

            private void TriggerBoarding()
            {
                approachActive = false;
                scaryPhase = ScaryPhase.Alarm;

                if (current != null) current.gameObject.SetActive(false);
                if (ambienceSource) ambienceSource.Stop();
                if (angrySource) angrySource.Stop();

                travel.SetHyperspaceAllowed(false);
                RemoveLightmaps();

                foreach (var obj in disabledOnBoarding)
                    if (obj != null) obj.SetActive(false);

                if (travel.travelUI != null) travel.travelUI.gameObject.SetActive(false);

                StartAlarm();
            }

            private void StartAlarm()
            {
                if (alarmRoot == null) { StartHunting(); return; }

                alarmRoot.SetActive(true);
                if (alarmLight) alarmLight.intensity = 0f;
                if (alarmAudio) alarmAudio.Play();

                alarmDuration = UnityEngine.Random.Range(5f, 15f);
                alarmTimer = 0f;
                alarmLightPhase = 0f;
            }

            private void UpdateAlarmPhase()
            {
                alarmTimer += Time.deltaTime;

                alarmLightPhase += Time.deltaTime * Mathf.PI * 2f;
                if (alarmLight) alarmLight.intensity = (Mathf.Sin(alarmLightPhase) * 0.5f + 0.5f) * 2f;

                if (alarmTimer >= alarmDuration)
                {
                    if (alarmLight) alarmLight.intensity = 0f;
                    if (alarmAudio) alarmAudio.Stop();
                    if (alarmRoot) alarmRoot.SetActive(false);
                    StartHunting();
                }
            }

            private void StartHunting()
            {
                scaryPhase = ScaryPhase.Hunting;
                survivalTimer = 0f;

                if (enemyRoot != null)
                {
                    enemyRoot.SetActive(true);

                    navAgent = enemyRoot.GetComponent<NavMeshAgent>();
                    if (navAgent == null) navAgent = enemyRoot.AddComponent<NavMeshAgent>();

                    navAgent.speed = 5.5f;
                    navAgent.angularSpeed = 360f;
                    navAgent.acceleration = 12f;
                    navAgent.stoppingDistance = 0.3f;
                }
            }

            private void UpdateHuntPhase(float hyperIntensity, bool leverUp)
            {
                survivalTimer += Time.deltaTime;

                if (survivalTimer >= 60f && announcementRoot != null && !announcementRoot.activeSelf)
                {
                    announcementRoot.SetActive(true);
                    if (announcementAudio) announcementAudio.Play();
                    travel.SetHyperspaceAllowed(true);
                    if (navAgent) navAgent.speed = 2.5f;
                }

                if (navAgent != null && Player.local != null)
                {
                    Vector3 playerPos = Player.local.transform.position;
                    navAgent.SetDestination(playerPos);

                    Vector3 toPlayer = playerPos - enemyTransform.position;
                    toPlayer.y = 0f;
                    float flatDist = toPlayer.magnitude;

                    if (flatDist < 3f)
                    {
                        if (flatDist < 0.5f)
                        {
                            Player.currentCreature.Damage(Player.currentCreature.maxHealth * 0.5f * Time.deltaTime);
                            if (navAgent) navAgent.speed = Mathf.Max(0.8f, navAgent.speed - Time.deltaTime * 2f);
                        }
                        else
                        {
                            float dps = Mathf.Lerp(0f, 10f, 1f - (flatDist / 3f));
                            Player.currentCreature.Damage(dps * Time.deltaTime);
                        }
                    }
                }

                if (leverUp)
                {
                    bool enemyClose = false;
                    if (enemyTransform != null && Player.local != null)
                    {
                        Vector3 toEnemy = Player.local.transform.position - enemyTransform.position;
                        toEnemy.y = 0f;
                        enemyClose = toEnemy.magnitude <= 5f;
                    }

                    if (!enemyClose || hyperIntensity >= 0.99f)
                        TriggerSurvived();
                }
            }

            private void TriggerSurvived()
            {
                scaryPhase = ScaryPhase.Survived;

                if (enemyRoot) enemyRoot.SetActive(false);
                if (announcementRoot) announcementRoot.SetActive(false);

                RestoreLightmaps();

                foreach (var go in disabledOnBoarding)
                    if (go != null) go.SetActive(true);

                travel.SetRewardDestination();
                travel.SetHyperspaceAllowed(false);
            }

            private void RemoveLightmaps()
            {
                if (lightmapsRemoved) return;
                lightmapsRemoved = true;

                LightmapSettings.lightmaps = new LightmapData[0];
                RenderSettings.ambientLight = Color.black;

                emissiveRenderers.Clear();
                foreach (var mr in renderers)
                {
                    if (mr == null) continue;
                    foreach (var mat in mr.materials)
                    {
                        if (mat == null) continue;
                        if (mat.GetFloat("_UseEmission") == 1f || mat.IsKeywordEnabled("_USEEMISSION_ON"))
                        {
                            mat.SetFloat("_UseEmission", 0f);
                            mat.DisableKeyword("_USEEMISSION_ON");
                            emissiveRenderers.Add(mat);
                        }
                    }
                }
            }

            private void RestoreLightmaps()
            {
                if (!lightmapsRemoved) return;
                lightmapsRemoved = false;

                LightmapSettings.lightmaps = cachedLightmaps;
                RenderSettings.ambientLight = cachedAmbientLight;

                foreach (var mat in emissiveRenderers)
                {
                    if (mat != null)
                    {
                        mat.SetFloat("_UseEmission", 1f);
                        mat.EnableKeyword("_USEEMISSION_ON");
                    }
                }

                emissiveRenderers.Clear();
            }

            private void ResetAll()
            {
                approachActive = false;
                approachTimer = 0f;
                gazeAggroActive = false;
                gazeTimer = 0f;
                scaryPhase = ScaryPhase.None;

                if (current != null)
                {
                    current.gameObject.SetActive(false);
                    current.localScale = originalScale;
                    current.position = originalPos;
                    current = null;
                }
                currentWaypoint = null;

                if (ambienceSource) ambienceSource.Stop();
                if (angrySource) angrySource.Stop();
                if (enemyRoot) enemyRoot.SetActive(false);
                if (alarmRoot) alarmRoot.SetActive(false);
                if (announcementRoot) announcementRoot.SetActive(false);

                RestoreLightmaps();

                foreach (var go in disabledOnBoarding)
                    if (go != null) go.SetActive(true);

                travel.SetHyperspaceAllowed(true);
            }
        }

        //  --------------------- Cockpit Trigger Handler ---------------------

        private void SetupCockpitTrigger()
        {
            if (cockpitTrigger == null) return;

            var trigger = cockpitTrigger.gameObject.AddComponent<CockpitTriggerHandler>();
            trigger.onCockpitEntered = () => starweirdManager?.OnCockpitEntered();
            trigger.onCockpitExited = () => starweirdManager?.OnCockpitExited();
        }

        public class CockpitTriggerHandler : MonoBehaviour
        {
            public Action onCockpitEntered;
            public Action onCockpitExited;

            private void OnTriggerEnter(Collider other)
            {
                if (other.GetComponentInParent<Player>()) onCockpitEntered?.Invoke();
            }

            private void OnTriggerExit(Collider other)
            {
                if (other.GetComponentInParent<Player>()) onCockpitExited?.Invoke();
            }
        }

        //  --------------------- Travel Manager ---------------------

        public class TravelManager
        {
            public Transform travelUI;
            public Transform levelView;
            public List<Transform> travelMaps;

            public Button travelButton;
            public TextMeshProUGUI travelText;

            public Image levelIcon;
            public TextMeshProUGUI levelName;

            public UIWorldMapBoard setDestination;

            public string destinationLevel = "";

            public bool hyperspaceAllowed = true;
            public bool isRewardMode = false;
            private string rewardItemID = "StarweirdWhistle";

            public static bool hasFirstLoaded;

            public void Init()
            {
                if (travelUI != null)
                {
                    travelButton = travelUI.Find("Canvas/Button")?.GetComponent<Button>();
                    travelText = travelUI.Find("Canvas/Button/Text")?.GetComponent<TextMeshProUGUI>();
                    travelButton?.onClick.AddListener(TravelToDestination);
                }

                if (levelView != null)
                {
                    levelIcon = levelView.Find("LevelIcon")?.GetComponent<Image>();
                    levelName = levelView.Find("LevelName")?.GetComponent<TextMeshProUGUI>();
                }

                setDestination = travelMaps.FirstOrDefault()?.GetComponent<UIWorldMapBoard>();

                destinationLevel = "";
                GetRandomLevel();
            }

            public void SetHyperspaceAllowed(bool allowed)
            {
                hyperspaceAllowed = allowed;
                if (travelButton != null)
                {
                    travelButton.interactable = allowed && !string.IsNullOrEmpty(destinationLevel);
                    if (isRewardMode) travelButton.interactable = true;
                }
            }

            public void SetRewardDestination()
            {
                isRewardMode = true;
                destinationLevel = "";

                if (travelUI != null) travelUI.gameObject.SetActive(true);
                if (levelView != null) levelView.gameObject.SetActive(true);

                var itemData = Catalog.GetData<ItemData>(rewardItemID);
                if (itemData != null)
                {
                    if (itemData.icon != null) { if (levelIcon) levelIcon.sprite = itemData.icon; }
                    else Catalog.LoadAssetAsync<Sprite>(itemData.iconAddress, x => { if (levelIcon) levelIcon.sprite = x; }, "Item");
                    if (levelName) levelName.text = itemData.displayName;
                }

                SetTravelButtonState(true, "Retrieve");
            }

            public void GetRandomLevel()
            {
                if (isRewardMode) { SetRewardDestination(); return; }

                var rand = UnityEngine.Random.Range(0, 100);

                if (rand >= 60 || !hasFirstLoaded)
                    SetDestinationLevel();
                else
                {
                    var ld = GenerateRandomLevel();
                    if (ld != null) SetDestinationLevel(ld);
                }

                hasFirstLoaded = true;
            }

            private LevelData GenerateRandomLevel()
            {
                if (setDestination.selectedLevelInstance != null && setDestination.currentLevelInstance != null)
                {
                    var ld = setDestination.selectedLevelInstance.LevelData;
                    setDestination.ResetLocationSelected();
                    setDestination.selectedLevelInstance = null;
                    return ld;
                }

                var validLevels = Catalog.GetAllID<LevelData>()
                    .Select(id => Catalog.GetData<LevelData>(id, true))
                    .Where(ld => ld != null && ld.showOnMap && ld.id != Level.current.data.id && (!ld.showOnlyDevMode || GameManager.DevMode)).ToList();

                if (validLevels.Count == 0) return null;
                return validLevels[UnityEngine.Random.Range(0, validLevels.Count)];
            }

            private void SetDestinationLevel(LevelData ld = null)
            {
                if (isRewardMode) return;

                if (ld == null)
                {
                    destinationLevel = "";
                    if (levelView != null) levelView.gameObject.SetActive(false);
                    if (levelIcon != null) levelIcon.sprite = null;
                    if (levelName != null) levelName.text = "???";
                    SetTravelButtonState(false);
                }
                else
                {
                    destinationLevel = ld.id;
                    if (levelView != null) levelView.gameObject.SetActive(true);

                    if (!ld.mapLocationIcon)
                        Catalog.LoadAssetAsync<Sprite>(ld.mapLocationIconAddress, x => { if (levelIcon) levelIcon.sprite = x; }, "Map");
                    else if (levelIcon != null)
                        levelIcon.sprite = ld.mapLocationIcon;

                    if (levelName != null) levelName.text = ld.name;
                    SetTravelButtonState(hyperspaceAllowed);
                }
            }

            public void SetTravelButtonState(bool active, string overrideText = null)
            {
                if (travelButton == null) return;
                travelButton.interactable = active;
                if (travelText != null)
                    travelText.color = (active) ? travelButton.colors.normalColor : travelButton.colors.disabledColor;
                if (travelText != null)
                    travelText.text = !string.IsNullOrEmpty(overrideText) ? overrideText : "Land";
            }

            private void TravelToDestination()
            {
                if (isRewardMode)
                {
                    var itemData = Catalog.GetData<ItemData>(rewardItemID);
                    if (itemData != null)
                    {
                        itemData.SpawnAsync(x =>
                        {
                            if (!x) return;
                            var hand = Player.currentCreature.GetHand(Side.Right);
                            if (hand != null && hand.grabbedHandle != null) hand.UnGrab(false);
                            hand?.Grab(x.handles.First());
                        }, Player.currentCreature.transform.position + Vector3.up, Quaternion.identity, owner: Item.Owner.Player);
                    }

                    isRewardMode = false;
                    GetRandomLevel();

                    return;

                }

                if (!hyperspaceAllowed || string.IsNullOrEmpty(destinationLevel)) return;
                LevelManager.LoadLevel(destinationLevel);
            }
        }

        // --------------------- Secrets Display ---------------------

        public class SecretsDisplay
        {
            public SecretsDisplay(Transform transform)
            {
                parent = transform;
                if (!parent) { Debug.LogError("SecretsDisplay not found"); return; }

                Transform canvas = parent.Find("Canvas");
                if (!canvas) { Debug.LogError("Canvas not found!"); return; }

                sfxPlayer = parent.Find("SFXPlayer")?.GetComponent<AudioSource>();
                secretsCounter = canvas.Find("Top/ProgressText")?.GetComponent<TextMeshProUGUI>();
                secretEntryBase = canvas.Find("Scroll View/Viewport/Content/SecretItem");
                itemSpawnPosition = parent.Find("ItemSpawnPosition");

                if (secretEntryBase) secretEntryBase.gameObject.SetActive(false);

                SetUpEntries();
                UpdateSecretsCounter();
            }

            public Transform parent;
            public Transform secretEntryBase;
            public TextMeshProUGUI secretsCounter;
            public List<SecretItemEntry> secretEntries = new List<SecretItemEntry>();

            public static AudioContainer itemLockedContainer;
            public static AudioContainer itemSpawnContainer;

            public AudioSource sfxPlayer;
            public Transform itemSpawnPosition;

            public void UpdateSecretsCounter()
            {
                if (SecretManager.allSecrets.Count == 0 || secretsCounter == null) return;

                float progress = (float)SecretManager.unlockedSecrets.Count / SecretManager.allSecrets.Count;
                secretsCounter.color = Color.HSVToRGB(progress * 0.3f, 1, 1);
                secretsCounter.text = $"{SecretManager.unlockedSecrets.Count}/{SecretManager.allSecrets.Count} ({(progress * 100):F0}%) Discovered";
            }

            private void SetUpEntries()
            {
                if (secretEntryBase == null) return;
                secretEntries.Clear();

                foreach (var secret in SecretManager.allSecrets)
                {
                    var entryObj = GameObject.Instantiate(secretEntryBase, secretEntryBase.parent);
                    entryObj.gameObject.SetActive(true);
                    secretEntries.Add(new SecretItemEntry(secret, entryObj, this));
                }
                UpdateSecretsCounter();
            }

            public void UpdateAll()
            {
                foreach (var entry in secretEntries) entry.UpdateEntry();
            }

            public class SecretItemEntry
            {
                private string id;
                private ItemData itemData;
                private Transform transform;
                private Button button;
                private TextMeshProUGUI itemName;
                private TextMeshProUGUI itemDescription;
                private RawImage itemPreview;
                private bool isUnlocked;
                private SecretsDisplay mainDisplay;

                public SecretItemEntry(string id, Transform transform, SecretsDisplay display)
                {
                    this.id = id;
                    this.transform = transform;
                    this.mainDisplay = display;

                    button = transform.GetComponent<Button>();
                    itemName = transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
                    itemDescription = transform.Find("ItemDescription")?.GetComponent<TextMeshProUGUI>();
                    itemPreview = transform.Find("RawImage")?.GetComponent<RawImage>();

                    SetupEntry();
                }

                private void SetupEntry()
                {
                    itemData = Catalog.GetData<ItemData>(id, true);
                    if (itemData == null) return;

                    isUnlocked = SecretManager.unlockedSecrets.Contains(itemData.id);
                    button?.onClick.AddListener(TrySpawn);

                    if (itemData.icon != null) itemPreview.texture = itemData.icon.texture;
                    else itemData.LoadIconAsync(false, x => { itemPreview.texture = x.texture; });

                    if (isUnlocked) UnlockSecret(); else LockSecret();
                }

                public void UnlockSecret()
                {
                    if (itemName) { itemName.text = itemData.displayName; itemName.color = new Color(1, 0.84f, 0); }
                    if (itemDescription) { itemDescription.text = itemData.description; itemDescription.color = Color.white; }
                    if (itemPreview) itemPreview.color = Color.white;
                }

                public void LockSecret()
                {
                    if (itemName) { itemName.text = "~LOCKED~"; itemName.color = Color.red; }
                    if (itemDescription)
                    {
                        var module = itemData.GetModule<ItemModuleSecret>();
                        itemDescription.text = module.hint;
                        itemDescription.color = Color.gray;
                    }
                    if (itemPreview) itemPreview.color = Color.black;
                }

                private void TrySpawn()
                {
                    if (isUnlocked)
                    {
                        itemData.SpawnAsync(x =>
                        {
                            if (!x) return;
                            Util.PlaySound(mainDisplay.sfxPlayer, itemSpawnContainer);
                        }, mainDisplay.itemSpawnPosition.position, mainDisplay.itemSpawnPosition.rotation, owner: Item.Owner.Player);
                    }
                    else Util.PlaySound(mainDisplay.sfxPlayer, itemLockedContainer);
                }

                public void UpdateEntry()
                {
                    bool currentlyUnlocked = SecretManager.unlockedSecrets.Contains(itemData.id);
                    if (currentlyUnlocked != isUnlocked)
                    {
                        isUnlocked = currentlyUnlocked;
                        if (isUnlocked) UnlockSecret(); else LockSecret();
                        mainDisplay.UpdateSecretsCounter();
                    }
                }
            }
        }
    }

    public class ModuleStarweirdWhistle : ItemModule
    {
        public AudioClip audioClip;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.GetOrAddComponent<ItemStarweirdWhistle>();
        }

        public override IEnumerator LoadAddressableAssetsCoroutine(ItemData data)
        {
            yield return Catalog.LoadAssetCoroutine<AudioClip>("PC.TheNomadRim.Sound.StarweirdChase", x => audioClip = x, "Sound");
        }

        public override void ReleaseAddressableAssets()
        {
            base.ReleaseAddressableAssets();

            if (audioClip != null)
            Catalog.ReleaseAsset(audioClip);
        }
    }

    public class ItemStarweirdWhistle : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        private Item item;
        private AudioSource audioSource;

        float waitTimer = 0;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            audioSource = this.GetComponentInChildren<AudioSource>();

            Debug.Log("[TNR] AudioSource found: " + (audioSource != null) + " | clip: " + (audioSource?.clip != null));

            audioSource.clip = item.data.GetModule<ModuleStarweirdWhistle>().audioClip;

            foreach (var handle in item.handles)
                handle.OnHeldActionEvent += HandleWhistleAction;
        }

        private void HandleWhistleAction(RagdollHand hand, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                audioSource.Play();
            }
            else if (action == Interactable.Action.UseStop)
            {
                audioSource.Stop();
            }
        }

        protected override void ManagedUpdate()
        {
            base.ManagedUpdate();
            if (waitTimer <= 0 && audioSource.isPlaying)
            {
                foreach (var crt in Creature.allActive)
                {
                    if (crt == null || Player.currentCreature == null || crt == Player.currentCreature) continue;

                    if (Vector3.Distance(crt.transform.position, Player.currentCreature.transform.position) <= 35.0)
                    {
                        var fear = crt.brain.instance.GetModule<BrainModuleFear>();
                        fear?.Panic(2f);
                    }
                }

                waitTimer = 0.25f;
            }

            if (waitTimer > 0) waitTimer -= Time.deltaTime;
        }
    }
}
