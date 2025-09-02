#region Using statements

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Bitgem.VFX.StylisedWater
{
    public class WateverVolumeFloater : MonoBehaviour
    {
        #region Public fields

        public WaterVolumeHelper WaterVolumeHelper = null;

        #endregion

        #region MonoBehaviour events

        void Update()
        {
            if (!WaterVolumeHelper)
            {
                return;
            }

            transform.position = new Vector3(transform.position.x, WaterVolumeHelper.GetHeight(transform.position) ?? transform.position.y, transform.position.z);
        }

        #endregion
    }
}