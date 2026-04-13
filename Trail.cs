using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace TheNomadRim
{
    public class TrailData
    {
        public Vector3 m_tip_position;
        public Vector3 m_bottom_position;
        public float f_time;

        public TrailData(Vector3 tip, Vector3 bottom, float time)
        {
            m_tip_position = tip;
            m_bottom_position = bottom;
            f_time = time;
        }
    }

    public class LightsaberTrail : ThunderBehaviour
    {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.FixedUpdate;

        public Vector3 m_tip;
        public Vector3 m_bottom;

        public float f_time;
        public float f_transition_speed = 5f;

        private Vector3 m_last_rotation;
        private TrailData m_current_data;
        private Matrix4x4 m_local_space_transform;

        private Transform m_trans;
        private Mesh m_mesh;
        private Vector3[] m_vertices;
        private Color[] m_colors;
        private Vector2[] m_uv;

        public List<TrailData> l_trails = new List<TrailData>();

        protected void Awake()
        {
            MeshFilter meshF = GetComponent<MeshFilter>();
            m_mesh = meshF.mesh;
            m_trans = transform;
        }

        protected override void ManagedFixedUpdate()
        {
            if (ModSettings.bLightsaberTrail)
            {
                UpdateTrail();
            }
            else
            {
                ClearTrail();
            }
        }

        //-------------------------------------------------------------------------------------------\\

        private void UpdateTrail()
        {
            AddTrailSegment();
            RemoveOldSegments();
            UpdateMesh();
        }

        private void AddTrailSegment()
        {
            float rotationDelta = (m_last_rotation - m_trans.rotation.eulerAngles).sqrMagnitude;
            m_last_rotation = m_trans.rotation.eulerAngles;

            if (l_trails.Count == 0 || rotationDelta > 0)
            {
                Vector3 tipPosition = m_trans.TransformPoint(m_tip);
                Vector3 bottomPosition = m_trans.TransformPoint(m_bottom);

                l_trails.Insert(0, new TrailData(tipPosition, bottomPosition, Time.time));
            }
        }

        private void RemoveOldSegments()
        {
            while (l_trails.Count > 0 && Time.time > l_trails[l_trails.Count - 1].f_time + ModSettings.fTrailLifetime)
            {
                l_trails.RemoveAt(l_trails.Count - 1);
            }
        }

        private void UpdateMesh()
        {
            if (l_trails.Count < 2)
            {
                m_mesh.Clear();
                return;
            }

            m_vertices = new Vector3[l_trails.Count * 2];
            m_colors = new Color[l_trails.Count * 2];
            m_uv = new Vector2[l_trails.Count * 2];

            m_local_space_transform = m_trans.worldToLocalMatrix;

            for (int i = 0; i < l_trails.Count; i++)
            {
                m_current_data = l_trails[i];
                float normalizedTime = Mathf.Clamp01((Time.time - m_current_data.f_time) / ModSettings.fTrailLifetime);

                m_vertices[i * 2] = m_local_space_transform.MultiplyPoint(m_current_data.m_bottom_position);
                m_vertices[i * 2 + 1] = m_local_space_transform.MultiplyPoint(m_current_data.m_tip_position);

                m_uv[i * 2] = new Vector2(normalizedTime, 0);
                m_uv[i * 2 + 1] = new Vector2(normalizedTime, 1);

                Color fadeColor = Color.Lerp(Color.white, new Color(1, 1, 1, 0), (normalizedTime - 0.3f) * 10f);
                m_colors[i * 2] = fadeColor;
                m_colors[i * 2 + 1] = fadeColor;
            }

            int[] triangles = new int[(l_trails.Count - 1) * 6];
            for (int i = 0; i < l_trails.Count - 1; i++)
            {
                triangles[i * 6] = i * 2;
                triangles[i * 6 + 1] = i * 2 + 2;
                triangles[i * 6 + 2] = i * 2 + 1;

                triangles[i * 6 + 3] = i * 2 + 2;
                triangles[i * 6 + 4] = i * 2 + 3;
                triangles[i * 6 + 5] = i * 2 + 1;
            }

            m_mesh.Clear();
            m_mesh.vertices = m_vertices;
            m_mesh.colors = m_colors;
            m_mesh.uv = m_uv;
            m_mesh.triangles = triangles;

            m_mesh.RecalculateNormals();
            m_mesh.RecalculateBounds();
            m_mesh.UploadMeshData(false);

            UpdateTrailLife();
        }

        private void UpdateTrailLife()
        {
            if (f_time > ModSettings.fTrailLifetime)
            {
                f_time -= Time.deltaTime * f_transition_speed;
                if (f_time <= ModSettings.fTrailLifetime) f_time = ModSettings.fTrailLifetime;
            }
            else if (f_time < ModSettings.fTrailLifetime)
            {
                f_time += Time.deltaTime * f_transition_speed;
                if (f_time >= ModSettings.fTrailLifetime) f_time = ModSettings.fTrailLifetime;
            }
        }

        private void ClearTrail()
        {
            m_mesh.Clear();
            l_trails.Clear();
        }
    }
}
