using System;
using System.Collections.Generic;
using UnityEngine;

namespace _2_script
{
    [ExecuteAlways]
    public class MapGrids : MonoBehaviour
    {
        [Serializable]
        public struct Cell
        {
            public Vector3 position;
            public bool occupied;
        }

        [Header("Grid")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float rayHeight = 100f;
        [SerializeField] private LayerMask surfaceMask = ~0;

        [Header("Filter")]
        [SerializeField] private bool onlyFlatSurfaces = true;
        [SerializeField, Range(0f, 1f)] private float minNormalY = 0.7f;

        [Header("Debug")]
        [SerializeField] private bool drawGrid = true;
        [SerializeField] private float gizmoSize = 0.2f;

        [SerializeField] private List<Cell> cells = new();

        public IReadOnlyList<Cell> Cells => cells;
        public int CellCount => cells.Count;

        [ContextMenu("Bake Grid")]
        public void BakeGrid()
        {
            cells.Clear();

            Collider[] colliders = GetComponentsInChildren<Collider>();

            if (colliders.Length == 0)
            {
                Debug.LogError("Нет Collider внутри родителя. Raycast не может попасть в MeshRenderer без Collider.");
                return;
            }

            Bounds bounds = colliders[0].bounds;

            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);

            int rayCount = 0;
            int hitCount = 0;
            int filteredCount = 0;

            for (float x = bounds.min.x; x <= bounds.max.x; x += cellSize)
            {
                for (float z = bounds.min.z; z <= bounds.max.z; z += cellSize)
                {
                    rayCount++;

                    Vector3 rayStart = new Vector3(x, bounds.max.y + rayHeight, z);

                    if (Physics.Raycast(
                            rayStart,
                            Vector3.down,
                            out RaycastHit hit,
                            rayHeight * 2f,
                            surfaceMask,
                            QueryTriggerInteraction.Ignore))
                    {
                        hitCount++;

                        if (onlyFlatSurfaces && hit.normal.y < minNormalY)
                        {
                            filteredCount++;
                            continue;
                        }

                        cells.Add(new Cell
                        {
                            position = hit.point,
                            occupied = false
                        });
                    }
                }
            }

            Debug.Log(
                $"Bake complete. Rays: {rayCount}, Hits: {hitCount}, Filtered: {filteredCount}, Cells: {cells.Count}"
            );
        }

        public bool TryOccupyCell(int index, out Cell cell)
        {
            cell = default;

            if (index < 0 || index >= cells.Count)
                return false;

            cell = cells[index];

            if (cell.occupied)
                return false;

            cell.occupied = true;
            cells[index] = cell;

            return true;
        }

        public bool ReleaseCell(int index)
        {
            if (index < 0 || index >= cells.Count)
                return false;

            Cell cell = cells[index];

            if (!cell.occupied)
                return false;

            cell.occupied = false;
            cells[index] = cell;

            return true;
        }

        public bool TryGetCell(int index, out Cell cell)
        {
            cell = default;

            if (index < 0 || index >= cells.Count)
                return false;

            cell = cells[index];
            return true;
        }

        private void OnDrawGizmos()
        {
            if (!drawGrid || cells == null)
                return;

            Gizmos.color = Color.green;

            foreach (Cell cell in cells)
            {
                Gizmos.DrawCube(
                    cell.position,
                    Vector3.one * gizmoSize
                );
            }
        }
    }
}
