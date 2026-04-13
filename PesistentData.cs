using System;
using System.Collections.Generic;
using ThunderRoad;

namespace TheNomadRim
{
    [Serializable]
    public class ConnectedLightsaber
    {
        public string s_id;
        public List<string> s_kyber_crystals = new List<string>();
        public List<float> f_lengths = new List<float>();
    }

    [Serializable]
    public class LightsaberSaveData : ContentCustomData
    {
        public List<string> s_kyber_crystals = new List<string>();
        public List<float> f_lengths = new List<float>();
        public ConnectedLightsaber m_connected;
    }

    [Serializable]
    public class BlasterSaveData : ContentCustomData
    {
        public string blasterBoltID;
        public string blasterBoltOverride;
        public int ammo;
        public int fireMode;
        public int scopeZoom;
    }

    [Serializable]
    public class ItemSaveHolderData : ContentCustomData
    {
        public List<HolderItemData> m_holder_items = new List<HolderItemData>();
    }

    [Serializable]
    public class HolderItemData
    {
        public string s_holder_name;
        public string s_item_id;
        public List<ContentCustomData> m_custom_data;
    }

    [Serializable]
    public class CustomLightsaberData : ContentCustomData
    {
        public List<string> pieceIDs = new List<string>();
    }
}
