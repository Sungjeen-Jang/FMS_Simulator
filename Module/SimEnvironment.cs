using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMS_Simulator.Module
{
    public class SimEnvironment
    {
        private List<PrivateDevice> privateDevices = new List<PrivateDevice>();
        private ControlDevice controlDevice;

        private List<Transmission> transmissions = new List<Transmission>();

        public event Action SimulationCompleted;

        public static double simulationStartTime;

        private bool isSimulationRunning = false;
        private bool isSimulationCompleted = false;

        public ControlDevice ControlDeviceInstance => controlDevice;

        //private int totalTransmissionRequired = Variables.totalTransmissionRequired; // 종료 지정 숫자, 원하는 값으로 변경 가능
        private int transmissionCounter = 0;
        private List<Transmission> allCollisions = new List<Transmission>();

        // CD 전송 횟수를 추적하는 카운터
        private int cdTransmissionCount;
        public int CDTransmissionCount
        {
            get { return cdTransmissionCount; }
            set { cdTransmissionCount = value; }  // set 접근자를 추가합니다.
        }

        public SimEnvironment(int numberOfPDs)
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("SimEnvironment-SimEnvironment");
            }
            simulationStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            controlDevice = new ControlDevice();
            controlDevice.CSent += HandleCSent;

            for (int i = 0; i < numberOfPDs; i++)
            {
                privateDevices.Add(new PrivateDevice());
            }
        }

        private double GetCurrentRelativeTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - simulationStartTime;
        }

        public List<Transmission> Simulate()
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("SimEnvironment-Simulate");
            }
            List<Transmission> collisionList = new List<Transmission>();
            controlDevice.SendCSignal();

            // 모든 PD들의 응답을 저장합니다.
            List<Transmission> pdTransmissions = new List<Transmission>();

            
            
            foreach (var pd in privateDevices)
            {
                if (pd.IsReadyToRespond)
                {
                    var transmission = pd.RespondToC();
                    pdTransmissions.Add(transmission);
                    transmissionCounter++;
                }
            }

            if (Variables.DebugOutput)
            {
                Debug.WriteLine("Before checking for collisions");
            }

            // 충돌 확인
            collisionList = CheckForCollisions(pdTransmissions);

            // 충돌 목록의 길이가 0이면 시뮬레이션을 종료합니다.
            if (collisionList.Count == 0)
            {
                isSimulationRunning = false;
                isSimulationCompleted = true;
                SimulationCompleted?.Invoke();
                return allCollisions;
            }
            allCollisions.AddRange(collisionList);  // 충돌 목록을 전역 리스트에 추가

            if (Variables.DebugOutput)
            {
                Debug.WriteLine("After checking for collisions");
            }

            // 충돌이 없는 전송만 전역 transmissions 리스트에 추가
            foreach (var trans in pdTransmissions)
            {
                if (!collisionList.Contains(trans))
                {
                    transmissions.Add(trans);
                    
                }                    
            }

            //transmissionCounter += transmissions.Count; // 전송 횟수 갱신
            // 모든 PD의 전송이 완료되었는지 확인
            /*
            if (transmissionCounter>=privateDevices.Count*totalTransmissionRequired)
            {
                isSimulationRunning = false;    // 시뮬레이션 종료
                isSimulationCompleted = true;
                SimulationCompleted?.Invoke();
            }
            */
            return allCollisions;   // 전체 충돌 목록 반환
        }
        private List<Transmission> CheckForCollisions(List<Transmission> pdTransmissions)
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("SimEnvironment-CheckForCollisions");
            }
            List<Transmission> collisions = new List<Transmission>();

            for (int i = 0; i < pdTransmissions.Count; i++)
            {
                for (int j = 0; j < pdTransmissions.Count; j++)
                {
                    if (i != j && pdTransmissions[i] != null && pdTransmissions[j] != null)
                    {
                        if (pdTransmissions[i].StartTime < pdTransmissions[j].EndTime && pdTransmissions[i].EndTime > pdTransmissions[j].StartTime)
                        {
                            if (!collisions.Contains(pdTransmissions[i]))
                            {
                                collisions.Add(pdTransmissions[i]);
                            }
                            if (!collisions.Contains(pdTransmissions[j]))
                            {
                                collisions.Add(pdTransmissions[j]);
                            }
                            // Collision이 발생한 PD 정보 
                            //System.Diagnostics.Debug.WriteLine($"Collision detected between PD {pdTransmissions[i].PdId} and PD {pdTransmissions[j].PdId} at time {pdTransmissions[i].StartTime}");
                        }
                    }
                }
            }

            foreach (var collision in collisions)  // 수정된 부분: collisionList를 collisions로 변경
            {
                if (collision.PdId < 0 || collision.PdId >= privateDevices.Count)
                {
                    Debug.WriteLine($"Invalid PdId: {collision.PdId}");
                }

                privateDevices[collision.PdId].ResetInteraction();
            }
            foreach (var trans in pdTransmissions)
            {
                if (trans == null)
                {
                    continue;  // null인 경우 다음 반복으로 넘어갑니다.
                }

                if (!collisions.Contains(trans))
                {
                    transmissions.Add(trans);
                    if (trans.PdId >= 0 && trans.PdId < privateDevices.Count) // 인덱스가 유효한 범위에 있는지 확인
                    {
                        privateDevices[trans.PdId]?.SetInteraction();  // privateDevices[trans.PdId]가 null인 경우 방지
                    }
                }
            }


            return collisions;
        }





        private bool IsCollision(Transmission newTransmission)
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("SimEnvironment-IsCollision");
            }
            if (newTransmission == null)
            {
                return false; // null 값이 전달된 경우 콜리전이 없다고 가정
            }
            foreach (var transmission in transmissions)
            {
                if (newTransmission.StartTime < transmission.EndTime && newTransmission.EndTime > transmission.StartTime)
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleCSent()
        {
            CDTransmissionCount++; // 카운터 증가
            
            if (isSimulationRunning) return; // 이미 실행 중이면 반환
            isSimulationRunning = true;

            // C 신호가 전송된 후 PD들이 응답을 시작합니다.
            List<Transmission> currentTransmissions = new List<Transmission>();
            foreach (var pd in privateDevices)
            {
                pd.StartTransmission();
                if (pd.IsReadyToRespond)
                {
                    var transmission = pd.RespondToC();
                    currentTransmissions.Add(transmission);
                }
            }

            var collisionList = CheckForCollisions(currentTransmissions);
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("SimEnvironment-HandleCSent-CollionNumbers : "+collisionList.Count);
            }

            if (collisionList.Count == 0)
            {
                if (Variables.DebugOutput)
                {
                    Debug.WriteLine($"SimEnvironment-HandleCSent: CD Transmission Count = {CDTransmissionCount}");
                }
                isSimulationRunning = false;
                isSimulationCompleted = true;
                SimulationCompleted?.Invoke();
                return;
            }
            // 충돌이 없는 전송만 전역 transmissions 리스트에 추가
            foreach (var trans in currentTransmissions)
            {
                if (!collisionList.Contains(trans))
                    transmissions.Add(trans);
            }

            isSimulationRunning = false; // 추가한 코드: 시뮬레이션 실행 완료 후 값을 다시 false로 설정
        }


        public void StopAllPDTransmissions()
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("SimEnvironment-StopAllPDTransmissions");
            }
            foreach (var pd in privateDevices)
            {
                pd.StopTransmission();
            }
        }

        public void ClearSimEnvironment()
        {           
            
            controlDevice.CSent -= HandleCSent;
            controlDevice = null;
            privateDevices = null;

            PrivateDevice.ResetNextId(); // nextId 초기화
        }
    }
}
