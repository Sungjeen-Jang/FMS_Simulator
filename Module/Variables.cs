using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMS_Simulator.Module
{
    public class Variables
    {
        public static bool DebugOutput = true;
        public static int totalPDNum = 100;
        public static int totalTransmissionRequired = 2; // 종료 지정 숫자, 원하는 값으로 변경 가능

        public static double C_SIGNAL_LENGTH = 40.19; // in ms
        public static double P_SIGNAL_LENGTH = 61.69; // in ms
        public static double C_BROADCAST_INTERVAL = 5000; // 5 seconds in ms
    }
}
