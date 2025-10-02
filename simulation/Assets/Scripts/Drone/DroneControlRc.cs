using hakoniwa.drone.service;
using JetBrains.Annotations;
using UnityEngine;

namespace hakoniwa.drone
{
    public class DroneControlRc : IDroneControlOp
    {
        public int PutUpButton(int index, int value)
        {
            //nothing to do
            return 0;
        }
        public int PutDownButton(int index, int value)
        {
            //nothing to do
            return 0;
        }
        public void DoFlush()
        {
            //nothing to do
        }

        public void DoInitialize(string robotName)
        {
            //nothing to do
        }

        public bool GetMagnetRequest(out bool magnet_on)
        {
            magnet_on = false;
            return false;
        }


        public int PutForward(int index, double value)
        {
            return DroneServiceRC.PutForward(index, value);
        }

        public int PutHeading(int index, double value)
        {
            return DroneServiceRC.PutHeading(index, value);
        }

        public int PutHorizontal(int index, double value)
        {
            return DroneServiceRC.PutHorizontal(index, value);
        }

        public void PutMagnetStatus(bool magnet_on, bool contact_on)
        {
            //nothing to do
        }

        public int PutRadioControlButton(int index, int value)
        {
            return DroneServiceRC.PutRadioControlButton(index, value);
        }
        public int PutFlightModeChangeButton(int index, int value)
        {
            return DroneServiceRC.PutModeChangeButton(index, value);
        }

        public int PutVertical(int index, double value)
        {
            return DroneServiceRC.PutVertical(index, value);
        }
    }
}

