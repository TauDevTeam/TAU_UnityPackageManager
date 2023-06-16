#if UNITY_STANDALONE_WIN || UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace Autohand.Demo
{
    public class TAUTeleporterLink : MonoBehaviour
    {
        public Teleporter teleport;
        public SteamVR_Input_Sources handType;
        public SteamVR_Action_Boolean teleportAction;
        bool teleporting;

        private void FixedUpdate()
        {

            bool bigFingerClosed = HubDataParcer.instance.TAU_Hands[((int)handType) - 1].lastFingersCurl[0] < 0.5f;

            if (!teleporting && bigFingerClosed)
            {
                teleporting = true;
                teleport.StartTeleport();
            }
            else if (teleporting && !bigFingerClosed)
            {
                teleporting = false;
                teleport.Teleport();
            }
        }
    }
}
#endif
