using System;
using System.Collections.Generic;
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

using vKTCQuadroControl;
using KTCQuadroControl;

using System.Threading;
using System.Windows.Threading;

using SharpDX;
using SharpDX.XInput;



namespace Vorlage
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    /// 

    public delegate void DelString(string hexString);

    public delegate void GUIArmingDelType(bool status);

    public partial class MainWindow : Window
    {
        // NICHT ÄNDERN
        // ----------------------------
        private vQuadroControl vmyQC;
        DelString DelStringObject;
        // ----------------------------

        //thread for heartbeat
        private Thread HeartbeatThread;

        //thread for controller
        private Thread ControllerThread;

        Controller XBox;
        State myState;

        //delegate object for GUIArming
        private GUIArmingDelType GuiArmingDel;

        public MainWindow()
        {
            // NICHT ÄNDERN
            // -------------------------------------------------
            vmyQC = new vQuadroControl();
            DelStringObject = new DelString(vSchickeHexString);
            // -------------------------------------------------

            InitializeComponent();

            HeartbeatThread = new Thread(Heartbeat);
            ControllerThread = new Thread(ControllerFunktion);

            GuiArmingDel = GUIArming;
        }

        // NICHT ÄNDERN
        // ----------------------------------------------------------------------------------------------------------
        public void vSchickeHexString(String hexString)
        {
            if (!Dispatcher.CheckAccess())
            { Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, DelStringObject, hexString); }
            else { vmyQC.v_SchickeHexString(hexString); }
        }

        // ----------------------------------------------------------------------------------------------------------


        // ButtonClick Funktionen -----------------------------------------------------------------------------------
        private void BT_COMPortVerbinden_Click(object sender, RoutedEventArgs e)
        {
            string COMPortName;

            COMPortName = TB_COMPort.Text;

            //COMPort verbinden
            if (vmyQC.vVerbinde(COMPortName))
            {
                Label_VerbindungsStatus.Content = "COMPort verbunden";
            }
            else
            {
                Label_VerbindungsStatus.Content = "COMPort Verbindung FEHLGESCHLAGEN";
            }



        }

        private void BT_COMPortTrennen_Click(object sender, RoutedEventArgs e)
        {
            if (vmyQC.vVerbindungTrennen())
            {
                Label_VerbindungsStatus.Content = "COMPort getrennt";
            }
            else
            {
                Label_VerbindungsStatus.Content = "COMPort Trennung FEHLGESCHLAGEN";
            }
        }

        private void BT_HS_HB_Click(object sender, RoutedEventArgs e)
        {
            //Handshake

            vSchickeHexString("3C 20 2F 00 0A DC D1 CA 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
            vSchickeHexString("3C 20 2F 00 0A DC D1 CA 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01");
            vSchickeHexString("3C 20 2F 00 0A DC D1 CA 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03");
            vSchickeHexString("3c 20 12 00 81 74 10 c4 00 00 41 00 c0 07 64 00 00 00");
            vSchickeHexString("3c 20 12 00 5c 98 09 c4 00 00 41 00 d0 07 64 00 00 00");

            //Heartbeat
            HeartbeatThread.Start();

        }

        private void BT_XBoxRead_Click(object sender, RoutedEventArgs e)
        {
            XBox = new Controller(UserIndex.One);
            ControllerThread.Start();
        }


        // Thread Funktionen ----------------------------------------------------------------------------------------
        private void Heartbeat()
        {
            bool run = true;


            try
            {
                while (run)
                {
                    vSchickeHexString("3C 20 2F 00 0A DC D1 CA 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03");
                    Thread.Sleep(1000);
                }
            }

            catch (ThreadAbortException)
            {
                run = false;
            }
        }

        private void ControllerFunktion()
        {
            int leftTrigger;
            float leftTriggerf;
            string Throttle;

            bool run = true;

            try
            {
                while (run)
                {
                    //XBox Controller Auslesen
                    myState = XBox.GetState();


                    //linken Trigger Wert auslesen
                    leftTrigger = myState.Gamepad.LeftTrigger;

                    //Triggerwert veratbeiten - to float
                    leftTriggerf = leftTrigger / 255f;

                    if (leftTriggerf == 0f)
                    {
                        leftTriggerf = -1f;
                    }

                    Throttle = QuadroControl.KonvertiereFloatZuHexString(leftTriggerf);

                    //ManualControlCommand UAVTalk-Nachricht erstellen (Throttle = -1f - mehrmals)

                    //WICHTIG: die nullen nach Throttle sind die werte für ROLL PITCH YAW COLLECTIVE THRUST. Müssen später ergänzt werden

                    vSchickeHexString("3C 20 38 00 80 74 10 C4 00 00 " + Throttle + " 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " + Throttle + " 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00");

                    //AccsoryDesired setzen

                    if (myState.Gamepad.Buttons == GamepadButtonFlags.Start)
                    {
                        vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 3F");
                        //Setze ArmingStatus auf GUI
                        GUIArming(true);
                    }

                    Thread.Sleep(20);

                    if (myState.Gamepad.Buttons == GamepadButtonFlags.Back)
                    {
                        vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF");
                        //Setze ArmingStatus auf GUI
                        GUIArming(false);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                run = false;
            }
        }

        private void GUIArming (bool status)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, GuiArmingDel, status);
                return;
            }
            else
            {
                if (status)
                {
                    Label_ArmingStatus.Content = "GEARMED!";
                }
                else
                {
                    Label_ArmingStatus.Content = "DISARMED!";
                }
            }
        }

    }
}
