using System;
using hakoniwa.objects.core.sensors;
using hakoniwa.pdu.interfaces;
using hakoniwa.pdu.msgs.sensor_msgs;
using hakoniwa.pdu.unity;
using hakoniwa.sim;
using UnityEngine;

namespace hakoniwa.drone.sim
{
    public class ZRangerController : MonoBehaviour
    {
        private IZRangerController controller;
        private IZRangerController GetController()
        {
            if (controller != null)
            {
                return controller;
            }
            controller = this.GetComponentInChildren<IZRangerController>();
            if (controller == null)
            {
                throw new Exception("Can not find IZRangerController");
            }
            return controller;
        }


        public void DoInitialize(string robotName, IHakoPdu hakoPdu)
        {
            var ret = hakoPdu.DeclarePduForWrite(robotName, DefaultZRangerController.pdu_name_zranger);
            if (ret == false)
            {
                throw new ArgumentException($"Can not declare pdu for write: {robotName} {DefaultZRangerController.pdu_name_zranger}");
            }
            var pduManager = hakoPdu.GetPduManager();
            if (pduManager == null)
            {
                throw new ArgumentException("ERROR: can not find pduManager");
            }
            this.GetController().DoInitialize(robotName, pduManager);
        }

        public void DoControl(IPduManager pduManager)
        {
            this.GetController().DoControl(pduManager);
        }

    }
}