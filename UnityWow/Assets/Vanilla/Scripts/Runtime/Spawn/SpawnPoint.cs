// Copyright (c) 2026 CatRabbit. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CR
{
    public class SpawnPoint : MonoBehaviour
    {
        public float m_GizmoRadius = 0.5f;
        public float m_DirectionArrowLength = 1.5f;
        public Color m_GizmoColor = Color.green;
        public Color m_SelectedColor = Color.yellow;

        void Awake()
        {
        }

        void Start()
        {
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = m_GizmoColor;
            DrawSpawnPointGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = m_SelectedColor;
            DrawSpawnPointGizmos(true);
        }

        private void DrawSpawnPointGizmos(bool isSelected)
        {
            Vector3 position = transform.position;
            Vector3 forward = transform.forward;
            Vector3 up = transform.up;
            Vector3 right = transform.right;

            Gizmos.DrawWireSphere(position, m_GizmoRadius);
            Gizmos.DrawSphere(position, m_GizmoRadius * 0.3f);

            Vector3 arrowEnd = position + forward * m_DirectionArrowLength;
            Gizmos.DrawLine(position, arrowEnd);

            float arrowHeadSize = 0.2f;
            Vector3 arrowHeadBase = arrowEnd - forward * arrowHeadSize;
            Vector3 arrowHeadLeft = arrowHeadBase - right * arrowHeadSize * 0.5f;
            Vector3 arrowHeadRight = arrowHeadBase + right * arrowHeadSize * 0.5f;
            Vector3 arrowHeadUp = arrowHeadBase + up * arrowHeadSize * 0.5f;
            Vector3 arrowHeadDown = arrowHeadBase - up * arrowHeadSize * 0.5f;

            Gizmos.DrawLine(arrowEnd, arrowHeadLeft);
            Gizmos.DrawLine(arrowEnd, arrowHeadRight);
            Gizmos.DrawLine(arrowEnd, arrowHeadUp);
            Gizmos.DrawLine(arrowEnd, arrowHeadDown);

            Gizmos.DrawLine(arrowHeadLeft, arrowHeadRight);
            Gizmos.DrawLine(arrowHeadUp, arrowHeadDown);

            if (isSelected)
            {
                Gizmos.color = new Color(m_SelectedColor.r, m_SelectedColor.g, m_SelectedColor.b, 0.3f);
                Gizmos.DrawSphere(position, m_GizmoRadius);
            }
        }
    }
}

