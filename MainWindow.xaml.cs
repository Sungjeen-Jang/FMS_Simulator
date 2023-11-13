using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FMS_Simulator.Module;

/*
다음은 시뮬레이션을 위한 시나리오이다.
C신호는 5초마다 전송하고, 30.975ms의 길이를 갖는다.
이 응답을 받으면 일정한 랜덤 시간 이후에 PD들이 46.336ms의 길이를 갖는 P신호를 전송한다.
이러한 경우 PD들의 패킷들이 collision이 발생할 수도 있고 CD에게 정상적으로 도착할 수도 있다.
각각의 pd가 c에 응답하여 p신호를 전송한다고 한다면, 전송하는 시각을 기록해야 할 것이다.
예를 들어 pd1이 시각 t1에 응답하고 t1부터 term(packet 길이)의 시간동안 채널을 점유한다고 하면 
pd2는 시각 t1+term < t2에 응답하거나, t2+term < t1 (pd 2의 채널 점유 시간이 pd 1과 겹치지 않는 조건)을 만족해야  collision이 발생하지 않는다.

이 코드들을 사용해서 CD, PD의 점유 시작 시각, 점유 종료 시각을 Debug로 출력해야 한다.
예를 들어 CD는 5초마다 전송하고, t_c1에 전송을 시작했다고 하면, t_c1 + term_c(C_SIGNAL_LENGTH)에 전송이 종료된다.
그리고 t_c1 + 5초에 다시 전송을 하여, t_c1 + 5초 + term_c(C_SIGNAL_LENGTH)에 전송이 종료된다.
이러한 절차는 5초마다 반복된다.
CD의 C신호를 수신하면 PD1은 랜덤한 시간 이후 전송을 하여 t_p1 + term_p(P_SIGNAL_LENGTH)에 종료하고,
CD의 C신호를 수신하면 PD2 랜덤한 시간 이후 전송을 하여 t_p2 + term_p(P_SIGNAL_LENGTH)에 종료하며, 나머지 PD들의 절차도 유사하게 동작한다.
그리고 PD들의 충돌여부, 충돌시각을 Debug로 출력해야 한다.
*/
namespace FMS_Simulator
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private SimEnvironment env;

        private int simulationRuns = 0; // 현재까지 실행된 시뮬레이션 횟수
        public MainWindow()
        {
            InitializeComponent();
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("MainWindow-MainWindow");
            }
            
            env = new SimEnvironment(Variables.totalPDNum); // For example, 10 PDs
            env.SimulationCompleted += OnSimulationCompleted;
        }

        private void OnSimulateClick(object sender, RoutedEventArgs e)
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("MainWindow-OnSimulateClick");
            }
            lstResults.Items.Clear();

            var collisions = env.Simulate();
            foreach (var collision in collisions)
            {
                lstResults.Items.Add($"Collision detected for PD at time {collision.StartTime}");
            }
        }

        private void OnSimulationCompleted()
        {
            if (Variables.DebugOutput)
            {
                Debug.WriteLine("MainWindow-OnSimulationCompleted : # of Sim - "+simulationRuns);
            }
            Dispatcher.Invoke(() =>
            {
                env.ControlDeviceInstance.StopSignalTransmission(); // CD 전송 중단
                env.StopAllPDTransmissions(); // 모든 PD 전송 중단

                simulationRuns++;
                if (simulationRuns < Variables.totalTransmissionRequired)
                {
                    env.ControlDeviceInstance.StopTimer();   
                    env.ControlDeviceInstance.ResetSignalCount();
                    env.ClearSimEnvironment();
                    env.SimulationCompleted -= OnSimulationCompleted;   // event handler WPRJ
                    env = null; // 참조를 null로 설정
                    env = new SimEnvironment(Variables.totalPDNum);
                    env.SimulationCompleted += OnSimulationCompleted;
                    env.CDTransmissionCount = 0; // 카운터 초기화
                    return;
                }
                else
                {
                    env.ControlDeviceInstance.StopTimer();
                    env.ControlDeviceInstance.ResetSignalCount();
                    env.ClearSimEnvironment();
                    env.SimulationCompleted -= OnSimulationCompleted;   // event handler WPRJ
                    env = null; // 참조를 null로 설정
                    Application.Current.Shutdown();                    
                }                
            });
        }
    }
}
