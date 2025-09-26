using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hakoniwa.pdu.interfaces;

namespace hakoniwa.pdu.msgs.sensor_msgs
{
    public class MultiRanger
    {
        protected internal readonly IPdu _pdu;
        public IPdu GetPdu() { return _pdu; }

        public MultiRanger(IPdu pdu)
        {
            _pdu = pdu;
        }
        public float front_range
        {
            get => _pdu.GetData<float>("front_range");
            set => _pdu.SetData("front_range", value);
        }
        public float back_range
        {
            get => _pdu.GetData<float>("back_range");
            set => _pdu.SetData("back_range", value);
        }
        public float left_range
        {
            get => _pdu.GetData<float>("left_range");
            set => _pdu.SetData("left_range", value);
        }
        public float right_range
        {
            get => _pdu.GetData<float>("right_range");
            set => _pdu.SetData("right_range", value);
        }
        public float up_range
        {
            get => _pdu.GetData<float>("up_range");
            set => _pdu.SetData("up_range", value);
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
