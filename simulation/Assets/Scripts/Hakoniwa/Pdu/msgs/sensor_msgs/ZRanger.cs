using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hakoniwa.pdu.interfaces;

namespace hakoniwa.pdu.msgs.sensor_msgs
{
    public class ZRanger
    {
        protected internal readonly IPdu _pdu;
        public IPdu GetPdu() { return _pdu; }

        public ZRanger(IPdu pdu)
        {
            _pdu = pdu;
        }
        public float z_range
        {
            get => _pdu.GetData<float>("z_range");
            set => _pdu.SetData("z_range", value);
        }
        public float min_range
        {
            get => _pdu.GetData<float>("min_range");
            set => _pdu.SetData("min_range", value);
        }
        public float max_range
        {
            get => _pdu.GetData<float>("max_range");
            set => _pdu.SetData("max_range", value);
        }
        public float field_of_view
        {
            get => _pdu.GetData<float>("field_of_view");
            set => _pdu.SetData("field_of_view", value);
        }
    }
}
