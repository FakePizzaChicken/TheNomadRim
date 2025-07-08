using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.Skill.SpellPower;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace TheNomadRim
{
    public class ItemElectrobinocular : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        public Item m_item;
        public ModuleItemElectrobinocular m_module;

        private Camera m_camera_left;
        private Camera m_camera_right;
        private RenderTexture m_texture_left;
        private RenderTexture m_texture_right;
        private MeshRenderer m_eye_left;
        private MeshRenderer m_eye_right;
        private AudioSource m_audio_source;

        private int i_old_res;

        private int i_zoom;

        protected void Awake()
        {
            m_item = GetComponent<Item>();
            m_module = m_item.data.GetModule<ModuleItemElectrobinocular>();

            m_item.OnGrabEvent += OnGrabbed;
            m_item.OnUngrabEvent += OnDrop;
            m_item.OnHeldActionEvent += OnAction;

            m_camera_left = m_item.gameObject.GetNamedChild("CameraLeft").GetComponent<Camera>();
            m_camera_right = m_item.gameObject.GetNamedChild("CameraRight").GetComponent<Camera>();

            m_eye_left = m_item.gameObject.GetNamedChild("EyeLeft").GetComponent<MeshRenderer>();
            m_eye_right = m_item.gameObject.GetNamedChild("EyeRight").GetComponent<MeshRenderer>();

            m_audio_source = m_item.gameObject.GetComponent<AudioSource>();

            SetupRenderer();
        }

        private void SetupRenderer()
        {
            if (m_camera_left == null || m_camera_right == null || m_eye_left == null || m_eye_right == null)
            {
                return;
            }

            CreateRenderTexture();
            SetFOV(m_module.f_zoom_fovs[0]);
            i_zoom = 0;
           
        }

        private void SetFOV(float fov)
        {
            m_camera_left.fieldOfView = fov;
            m_camera_right.fieldOfView = fov;
        }

        public void SetRenderer(bool state)
        {
            if (m_eye_left != null)
            {
                m_eye_left.enabled = state;
            }

            if (m_eye_right != null)
            {
                m_eye_right.enabled = state;
            }

            if (m_camera_left != null)
            {
                m_camera_left.enabled = state;
            }

            if (m_camera_right != null)
            {
                m_camera_right.enabled = state;
            }   

            if (m_texture_left != null)
            {
                if (state)
                {
                    if (!m_texture_left.IsCreated())
                        m_texture_left.Create();
                    m_camera_left.targetTexture = m_texture_left;
                }
                else
                {
                    m_camera_left.targetTexture = null;
                    if (m_texture_left.IsCreated())
                        m_texture_left.Release();
                }
            }

            if (m_texture_right != null)
            {
                if (state)
                {
                    if (!m_texture_right.IsCreated())
                        m_texture_right.Create();
                    m_camera_right.targetTexture = m_texture_right;
                }
                else
                {
                    m_camera_right.targetTexture = null;
                    if (m_texture_right.IsCreated())
                        m_texture_right.Release();
                }
            }
        }

        public void CreateRenderTexture()
        {
            if (m_texture_left != null)
            {
                m_texture_left.Release();
            }

            if (m_texture_right != null)
            {
                m_texture_right.Release();
            }

            i_old_res = ModSettings.iElectrobinocularResolution;

            m_texture_left = new RenderTexture(i_old_res, i_old_res, 24, RenderTextureFormat.ARGB32);
            m_texture_right = new RenderTexture(i_old_res, i_old_res, 24, RenderTextureFormat.ARGB32);

            m_texture_left.Create();
            m_texture_right.Create();

            m_camera_left.targetTexture = m_texture_left;
            m_camera_right.targetTexture = m_texture_right;

            MaterialPropertyBlock leftBlock = new MaterialPropertyBlock();
            MaterialPropertyBlock rightBlock = new MaterialPropertyBlock();

            m_eye_left.GetPropertyBlock(leftBlock);
            m_eye_right.GetPropertyBlock(rightBlock);

            leftBlock.SetTexture("_RenderTexture", m_texture_left);
            rightBlock.SetTexture("_RenderTexture", m_texture_right);

            m_eye_left.SetPropertyBlock(leftBlock);
            m_eye_right.SetPropertyBlock(rightBlock);
        }

        protected override void ManagedUpdate()
        {
            if (ModSettings.iElectrobinocularResolution != i_old_res)
            {
                SetupRenderer();
                i_old_res = ModSettings.iElectrobinocularResolution;
            }
        }

        public void OnGrabbed(Handle handle, RagdollHand ragdollHand)
        {
            if (ragdollHand.playerHand)
            {
                SetRenderer(true);
            }
        }

        public void OnDrop(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            SetRenderer(false);
        }

        public void SwitchZoom(bool fwd)
        {
            if (m_module.f_zoom_fovs == null || m_module.f_zoom_fovs.Length == 0)
                return;

            if (fwd)
            {
                i_zoom++;
                if (i_zoom >= m_module.f_zoom_fovs.Length)
                    i_zoom = 0;
            }
            else
            {
                i_zoom--;
                if (i_zoom < 0)
                    i_zoom = m_module.f_zoom_fovs.Length - 1;
            }

            SetFOV(m_module.f_zoom_fovs[i_zoom]);
            Util.PlaySound(m_audio_source, fwd? m_module.m_zoom_sounds : m_module.m_unzoom_sounds, ModSettings.fZoomSoundVolume);

        }

        public void OnAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                SwitchZoom(true);
            }
            else if (action == Interactable.Action.AlternateUseStart)
            {
                SwitchZoom(false);
            }
        }
    }
}
