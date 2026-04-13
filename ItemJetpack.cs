using ThunderRoad;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemJetpack : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.FixedUpdate | ManagedLoops.Update;

        protected Item item;
        protected ModuleItemJetpack module;

        ParticleSystem fireLeft;
        ParticleSystem fireRight;

        AudioSource audioSource;
        AudioSource thrustSource;

        Creature creature;
        Locomotion locomotion;
        Rigidbody body;

        bool equipped;
        bool isFlying;

        float currentForce;

        float controllerInput;
        float controllerInputForward;
        float originalAirSpeed;

        protected void Awake()
        {
            item = GetComponent<Item>();
            module = item.data.GetModule<ModuleItemJetpack>();

            fireRight = item.gameObject.GetNamedChild("FlameEffect0")?.GetComponent<ParticleSystem>();
            fireLeft = item.gameObject.GetNamedChild("FlameEffect1")?.GetComponent<ParticleSystem>();

            audioSource = item.gameObject.GetNamedChild("SoundSource")?.GetComponent<AudioSource>();
            thrustSource = item.gameObject.GetNamedChild("ThrustSource")?.GetComponent<AudioSource>();

            if  (!fireRight || !fireLeft || !audioSource || !thrustSource)
            {
                DebugService.LogError($"[TheNomadRim] ItemJetpack on item {item.name} is missing required components.");
                DebugService.LogInfo($"[TheNomadRim] Found required components: FlameEffect0 {fireRight}, FlameEffect1 {fireLeft}, SoundSource {audioSource}, ThrustSource {thrustSource}.");
                return;
            }

            if (audioSource) audioSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);
            if (thrustSource) thrustSource.outputAudioMixerGroup = ThunderRoadSettings.GetAudioMixerGroup(AudioMixerName.Effect);

            currentForce = 0;

            item.OnSnapEvent += OnEquip;
            item.OnUnSnapEvent += OnUnSnapEvent;
        }

        protected override void ManagedFixedUpdate()
        {
            base.ManagedFixedUpdate();
            if (isFlying && equipped && Mathf.Abs(currentForce) >= 0.25f)
            {
                ApplyThrust();
                currentForce = 0;
            }
        }

        private void ApplyThrust()
        {
            body.AddForce(body.transform.up * currentForce, ForceMode.Acceleration);
        }

        protected override void ManagedUpdate()
        {
            base.ManagedUpdate();

            if (creature == null ||
                Player.currentCreature == null ||
                Player.local == null ||
                !equipped) return;

            if (!locomotion.isGrounded)
            {
                var movementHand = GameManager.options.locomotionController;
                var forceHand = movementHand == Side.Left ? Side.Right : Side.Left;

                controllerInput = PlayerControl.GetHand(forceHand).JoystickAxis.y;
                controllerInputForward = PlayerControl.GetHand(movementHand).JoystickAxis.y;

                InputXR.Controller controller = forceHand == Side.Right ? ((InputXR)PlayerControl.input).rightController : ((InputXR)PlayerControl.input).leftController;

                if (controller.thumbstickClick.GetDown())
                    if (isFlying) DisableEffects();

                if (controllerInput > ModSettings.fJetpackDeadzone)
                {
                    if (!isFlying)
                    {
                        EnableEffects();
                    }
                }

                if (controllerInput > ModSettings.fJetpackDeadzone || controllerInput < -ModSettings.fJetpackDeadzone)
                {
                    if (isFlying)
                    {
                        currentForce = Mathf.Clamp(currentForce + (controllerInput * (module.maxThrust*ModSettings.fJetpackThrustMultiplier/90)), -(module.maxThrust * ModSettings.fJetpackThrustMultiplier), module.maxThrust * ModSettings.fJetpackThrustMultiplier);
                    }
                }

                UpdateEffectValues();
                if (isFlying) locomotion.horizontalAirSpeed = 0.25f * ModSettings.fJetpackMoveForceMultiplier;
            }
            else
            {
                controllerInput = 0;
                controllerInputForward = 0;

                if (isFlying)
                {
                    DisableEffects();
                }
            }
        }

        //-------------------------------------------------------------------------------------------\\

        public void OnEquip(Holder holder)
        {
            if (!holder || !holder.creature)
                return;

            creature = holder?.creature;
            equipped = creature == Player.currentCreature;
            if (!equipped)
                return;

            locomotion = Player.local.locomotion;
            originalAirSpeed = locomotion.horizontalAirSpeed;
            body = Player.local.locomotion.physicBody.rigidBody;
        }

        public void OnUnSnapEvent(Holder holder)
        {
            if (isFlying) DisableEffects();
            UnEquip();
        }

        //-------------------------------------------------------------------------------------------\\

        public void UnEquip()
        {
            if (locomotion)
                locomotion.horizontalAirSpeed = originalAirSpeed;

            if (body) body.useGravity = true;

            equipped = false;
            creature = null;
            body = null;
        }

        public void EnableEffects()
        {
            Util.PlaySound(thrustSource, module.startSoundContainer);

            Util.PlaySoundLooped(audioSource, module.loopSoundContainer);
            UpdateEffectValues();

            fireLeft.Play();
            fireRight.Play();

            isFlying = true;

            if (body) body.useGravity = false;
        }

        public void UpdateEffectValues()
        {
            audioSource.volume = Mathf.Clamp(controllerInputForward + 0.65f, 0.65f, 1.35f);
            audioSource.pitch = Mathf.Clamp(controllerInput + 0.5f, 0.5f, 1.35f);
        }

        public void DisableEffects()
        {
            audioSource.Stop();

            fireRight.Stop();
            fireLeft.Stop();

            Util.PlaySound(audioSource, module.stopSoundContainer);

            if (locomotion) locomotion.horizontalAirSpeed = originalAirSpeed;

            isFlying = false;

            if (body) body.useGravity = true;
        }
    }
}
