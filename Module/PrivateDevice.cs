using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMS_Simulator.Module
{
    public class PrivateDevice : Device
    {
        private static int nextId = 0; // Static 변수로 각 PD에 고유 ID를 부여합니다.
        public int Id { get; }
        public bool HasInteractedWithCD { get; private set; } = false;

        private static Random random = new Random();

        // Constants
        const int TIMESLOT_NUM_SF6 = 50;
        const double TIMESLOT_SIZE_SF6 = 80; // 80ms
        const double PI = 3.141592653589793238;
        double gRes = 0.00875; // You might need to set this value according to your gyro sensor's sensitivity // 0.00875, 0.01750, 0.0700

        public PrivateDevice()
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("PrivateDevice-PrivateDevice");
            }
            Id = nextId++; // 객체가 생성될 때마다 증가하는 ID 값을 부여합니다.
        }

        public bool IsReadyToRespond { get; private set; } = true;


        public Transmission RespondToC()
        {
            // 만약 PD가 이미 CD와 상호 인지되었다면, 응답하지 않음.
            if (HasInteractedWithCD)
            {
#if false
                if (Variables.DebugOutput)
                {
                    Debug.WriteLine($"PrivateDevice-RespondToC: PD {Id} already interacted with CD.");
                }
#endif
                return null;
            }
                
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("PrivateDevice-RespondToC");
            }
            if (!IsTransmitting)
            {
                System.Diagnostics.Debug.WriteLine($"PD {Id} is not transmitting.");
                return null;
            }

            // Generate gyro value - for the purpose of this example I will use a random gyro value
            int gx = random.Next(-32768, 32767); // Assuming 16-bit gyro output
            double f_GYR_X = LSM9DS1_calcGyro(gx);

            // Calculate the delay
            int timeslotDelay = Generate_RandNum_toSensor((int)Math.Round(Variables.P_SIGNAL_LENGTH), f_GYR_X);
            double actualDelay = timeslotDelay * (int)Math.Round(Variables.P_SIGNAL_LENGTH);
            Debug.WriteLine("Random Values : "+actualDelay);
            double startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - SimEnvironment.simulationStartTime+actualDelay;
            double endTime = startTime + Variables.P_SIGNAL_LENGTH;

            System.Diagnostics.Debug.WriteLine($"PD {Id} starts transmission at {startTime} and ends at {endTime}");
            return new Transmission(startTime, Variables.P_SIGNAL_LENGTH) { PdId = this.Id };
        }

        public bool IsTransmitting { get; private set; } = false;

        public void StartTransmission()
        {
#if false
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("PrivateDevice-StartTransmission");
            }
#endif
            IsTransmitting = true;
        }

        public void StopTransmission()
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("PrivateDevice-StopTransmission");
            }
            IsTransmitting = false;
        }


        // Modified Generate_RandNum_toSensor function
        int Generate_RandNum_toSensor(int Max_Num, double Seed_Num)
        {
            int Random_Num = 0;
            long Fabricate_Num = (long)(Seed_Num * 10000000);

            if (Fabricate_Num < 0)
                Fabricate_Num = -Fabricate_Num;

            Random_Num = (int)(Fabricate_Num % Max_Num);
            return Random_Num;
        }

        // Modified LSM9DS1_calcGyro function
        double LSM9DS1_calcGyro(int gyro)
        {
            return (PI / 180) * gRes * gyro;
        }
        public void ResetInteraction()
        {
            HasInteractedWithCD = false;
        }

        public void SetInteraction()
        {
            HasInteractedWithCD = true;
        }

        // nextId를 초기화하는 정적 메서드
        public static void ResetNextId()
        {
            nextId = 0;
        }
    }


    public class Device
    {
        // 기본 로직과 속성들 (현재는 빈 클래스로 둡니다.)
    }

}
