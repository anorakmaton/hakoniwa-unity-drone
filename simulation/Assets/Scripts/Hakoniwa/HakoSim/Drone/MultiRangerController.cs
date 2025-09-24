using System;
using hakoniwa.objects.core.sensors;
using hakoniwa.pdu.interfaces;
using hakoniwa.pdu.msgs.geometry_msgs;
using hakoniwa.pdu.unity;
using hakoniwa.sim;
using UnityEngine;

namespace hakoniwa.drone.sim
{
    public class MultiRangerController : MonoBehaviour
    {
        private IMultiRangerController controller;
        private IMultiRangerController GetController()
        {
            if (controller != null)
            {
                return controller;
            }
            controller = this.GetComponentInChildren<IMultiRangerController>();
            if (controller == null)
            {
                throw new Exception("Can not find IMultiRangerController");
            }
            return controller;
        }


        public void DoInitialize(string robotName, IHakoPdu hakoPdu)
        {
            var ret = hakoPdu.DeclarePduForWrite(robotName, DefaultMultiRangerController.pdu_name_multiranger);
            if (ret == false)
            {
                throw new ArgumentException($"Can not declare pdu for write: {robotName} {DefaultMultiRangerController.pdu_name_multiranger}");
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
