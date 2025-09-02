using System.Collections.Generic;
using Bitgem.VFX.StylisedWater;
using Eco.Scripts.Trash;
using Unity.VisualScripting;
using UnityEngine;

namespace Eco.Scripts.World
{
    public class WaterField : Field
    {
        [SerializeField] private bool enableFloating;
        [SerializeField] private WaterVolumeTransforms water;
        [SerializeField] private WaterVolumeHelper waterVolumeHelper;

        private readonly List<WateverVolumeFloater> _waterVolumeFloaters = new();

        public override void MakeGrass()
        {
        }

        public override void OnDespawn()
        {
            if (enableFloating)
            {
                for (int i = _waterVolumeFloaters.Count - 1; i >= 0; i--)
                {
                    Destroy(_waterVolumeFloaters[i]);
                }

                _waterVolumeFloaters.Clear();
            }

            base.OnDespawn();
        }

        protected override TrashItem SpawnTrashAtTile(Tile tile, int id = -1)
        {
            var trash = base.SpawnTrashAtTile(tile, id);

            if (enableFloating)
            {
                var floater = trash.AddComponent<WateverVolumeFloater>();
                floater.WaterVolumeHelper = waterVolumeHelper;
                _waterVolumeFloaters.Add(floater);
            }

            trash.MakeKinematic(true);
            
            var pos = trash.transform.localPosition;
            pos.y -= 0.2f;
            trash.transform.localPosition = pos;

            return trash;
        }

        public void UpdateWaterCorners(int worldSize, Vector2Int position)
        {
            WaterVolumeBase.TileFace faces = 0;

            if (Mathf.Abs(position.x * position.y) >= Mathf.Pow(worldSize + 1, 2))
            {
                faces = 0;
            }
            else
            {
                if (position.x == -worldSize - 1)
                {
                    faces = WaterVolumeBase.TileFace.PosX;
                }

                if (position.x == worldSize + 1)
                {
                    faces = WaterVolumeBase.TileFace.NegX;
                }

                if (position.y == worldSize + 1)
                {
                    faces = WaterVolumeBase.TileFace.NegZ;
                }

                if (position.y == -worldSize - 1)
                {
                    faces = WaterVolumeBase.TileFace.PosZ;
                }
            }


            if (water.IncludeFoam != faces)
            {
                water.IncludeFoam = faces;
                water.Rebuild();
            }
        }
    }
}