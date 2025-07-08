using ThunderRoad;
using Unity.XR.CoreUtils;

namespace TheNomadRim
{
    public class ItemImbue : ThunderBehaviour
    {

        Item m_item;
        ModuleItemImbue m_module;

        bool b_enabled;

        Handle m_grip;

        SpellCastCharge m_charge_data;
        ColliderGroup m_collider_group;

        ImbueController m_imbue_controller;

        protected void Awake()
        {
            m_item = GetComponent<Item>();
            m_module = m_item.data.GetModule<ModuleItemImbue>();

            var parent = m_item.gameObject;


            m_item.OnGrabEvent += ItemGrabbed;
            m_item.OnUngrabEvent += ItemDropped;
            m_item.OnHeldActionEvent += OnAction;

            if (m_module != null && !string.IsNullOrEmpty(m_module.s_grip))
                m_grip = parent.GetNamedChild(m_module.s_grip).GetComponent<Handle>();

            m_collider_group = parent.GetNamedChild(m_module.s_imbue_collider_group)?.GetComponent<ColliderGroup>();

            m_imbue_controller = parent.GetComponent<ImbueController>();

            m_charge_data = Catalog.GetData<SpellCastCharge>(m_module.s_spell_id);


            TurnOff();
            TurnOn();
            TurnOff();
        }

        //-------------------------------------------------------------------------------------------\\

        private void ItemGrabbed(Handle handle, RagdollHand ragdollHand)
        {
            if (!ragdollHand.playerHand)
                TurnOn();
        }

        private void ItemDropped(Handle handle, RagdollHand ragdollHand, bool thrown)
        {
            if (!ragdollHand.playerHand)
                TurnOff();

        }

        private void OnAction(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (m_grip == handle)
            {
                if (action == Interactable.Action.AlternateUseStop)
                {
                    ToggleImbue(ragdollHand);
                }
            }
        }

        //-------------------------------------------------------------------------------------------\\

        private void SetImbue(bool toggled, bool first = false)
        {
            if (!m_collider_group || !m_imbue_controller)
            {
                return;
            }

            var imbue = m_collider_group.imbue;

            if (!imbue && !m_imbue_controller)
                return;

            if (toggled)
            {
                m_imbue_controller.SetImbueID(m_module.s_spell_id);
                m_imbue_controller.SetImbueMaxPercent(100);
                m_imbue_controller.SetImbueRate(100f);

            }
            else
            {
                m_imbue_controller.SetImbueRate(-100f);
                if (imbue) imbue.SetEnergyInstant(0);
            }
        }

        private void ToggleImbue(RagdollHand ragdollHand)
        {
            if (b_enabled)
                TurnOff(ragdollHand);
            else
                TurnOn(ragdollHand);
        }

        private void TurnOff(RagdollHand ragdollHand = null)
        {
            if (!b_enabled)
                return;

            SetImbue(false);

            if (ragdollHand)
                Util.PlayHaptic(ragdollHand, 0.6f);

            b_enabled = false;
        }

        private void TurnOn(RagdollHand ragdollHand = null)
        {
            if (b_enabled)
                return;

            SetImbue(true);

            if (ragdollHand)
                Util.PlayHaptic(ragdollHand, 0.8f);

            b_enabled = true;
        }

    }
}
