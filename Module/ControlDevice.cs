using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FMS_Simulator.Module
{
    public class ControlDevice    {
        

        public event Action CSent;
        public event Action SimulationCompleted;
        private System.Timers.Timer timer;

        private int signalCount = 0;
        //private int MAX_SIGNAL_COUNT = Variables.totalTransmissionRequired; // 예를 들어 5번만 C신호를 보냅니다.


        // ... 기본 로직과 속성들
        public ControlDevice()
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("ControlDevice-ControlDevice");
            }
            timer = new System.Timers.Timer(Variables.C_BROADCAST_INTERVAL);
            timer.Elapsed += (s, e) => SendCSignal();
            timer.Start();
        }
        public void SendCSignal()
        {
            timer.Stop();
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("\nControlDevice-SendCSignal Step " + signalCount);
            }
            // ... C 신호 전송 로직
            double startC = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - SimEnvironment.simulationStartTime;
            double endC = startC + Variables.C_SIGNAL_LENGTH;
            System.Diagnostics.Debug.WriteLine($"CD starts transmission at {startC} and ends at {endC}");

            CSent?.Invoke();
            signalCount++;
            //if (signalCount < MAX_SIGNAL_COUNT)
            //{
            //    CSent?.Invoke();
            //    signalCount++;
            //}
            //else
            //{
            //    timer.Stop();
            //}
            Thread.Sleep((int)Variables.C_SIGNAL_LENGTH);

            //if (signalCount < MAX_SIGNAL_COUNT)
            //{
            //    timer.Start();
            //}
            timer.Start();
        }
        public void OnSimulationCompleted()
        {
            SimulationCompleted?.Invoke();
        }
        public void StopSignalTransmission()
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("ControlDevice-StopSignalTransmission");
            }
            timer.Stop();
        }
        public void StopTimer()
        {
            timer.Stop();
        }
        public void ResetSignalCount()
        {
            signalCount = 0;
        }
    }
}
