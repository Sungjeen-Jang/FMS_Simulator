using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMS_Simulator.Module
{
    public class Transmission
    {
        public double StartTime { get; set; }
        public double EndTime => StartTime + Duration;
        public double Duration { get; }
        public int PdId { get; set; } // 추가한 PD의 Id 속성

        public Transmission(double startTime, double duration)
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("Transmission-Transmission");
            }
            StartTime = startTime;
            Duration = duration;
        }
    }
}
