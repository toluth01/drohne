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
using System.Xml.Serialization;
// neuer Kommentar
namespace Vorlage
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    /// 

    public delegate void DelString(string hexString);

    public delegate void GUIArmingDelType(bool status);
    public delegate void GUINotAusDelType(bool status);

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
        private GUINotAusDelType GuiNotAusDel;

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
            GuiNotAusDel= GUINotAus;
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
                TB_COMPort.Text = TB_COMPort.Text + " verbunden";
                //TB_COMPort.FontStyle = new FontStyle(FontStyles.Normal);
                Label_VerbindungsStatus.Content = "COMPort verbunden";
                TB_COMPort.Background = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98));
            }
            else
            {
                Label_VerbindungsStatus.Content = "COMPort Verbindung FEHLGESCHLAGEN";
                TB_COMPort.Background = new SolidColorBrush(Color.FromArgb(255, 212, 98, 98));
            }



        }

        private void BT_COMPortTrennen_Click(object sender, RoutedEventArgs e)
        {
            if (vmyQC.vVerbindungTrennen())
            {
                TB_COMPort.Text = "COMPort getrennt";
                Label_VerbindungsStatus.Content = "COMPort getrennt";
                TB_COMPort.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

                Label_ArmingStatus.Content = "disarmed";
                Label_ArmingStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                el_aussen.Stroke = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                el_innen.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

                BT_HS_HB.Background = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                BT_XBoxRead.Background = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));


                // HeartbeatThread.Abort();
               // ControllerThread.Abort();
                
            }
            else
            {
                TB_COMPort.Text = "Trennung FEHLGESCHLAGEN";
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

            if (HeartbeatThread.ThreadState == ThreadState.Unstarted)
            {
                HeartbeatThread.Start();
            }
            

            BT_HS_HB.Background = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98));

        }

        private void BT_XBoxRead_Click(object sender, RoutedEventArgs e)
        {
            XBox = new Controller(UserIndex.One);

            if (ControllerThread.ThreadState == ThreadState.Unstarted)
            {
                ControllerThread.Start();
            }
            

            BT_XBoxRead.Background = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98));
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
            float threshold = 0.2f;

            int rightTrigger;
            int leftTrigger;
            int rightThumbX;
            int rightThumbY;
            int leftThumbX;
            int leftThumbY;

            float rightTriggerf;
            float leftTriggerf;
            float rightThumbXf;
            float rightThumbYf;
            float leftThumbXf;
            float leftThumbYf;

            string Throttle;
            string Yaw;
            string Roll;
            string Pitch;


            bool run = true;

            try
            {
                while (run)
                {
                    
                    if (!XBox.IsConnected)
                    {
                        vSchickeHexString("3C 20 38 00 80 74 10 C4 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00");
                        vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF");
                        //Setze ArmingStatus auf GUI
                        GUIArming(false);
                        return;
                    }
                    
                    //XBox Controller Auslesen
                    myState = XBox.GetState();



                    //linken Trigger Wert auslesen
                    rightTrigger = myState.Gamepad.RightTrigger;
                    leftTrigger = myState.Gamepad.LeftTrigger;
                    rightThumbX = myState.Gamepad.RightThumbX;
                    leftThumbX = myState.Gamepad.LeftThumbX;
                    leftThumbY = myState.Gamepad.LeftThumbY;

                    //Triggerwert veratbeiten - to float

                    rightTriggerf = rightTrigger / 255f;
                    leftTriggerf = leftTrigger / 255f;
                    rightThumbXf = rightThumbX / 32768f;
                    leftThumbXf = leftThumbX / 32768f;
                    leftThumbYf = leftThumbY / 32768f;

                    if (Math.Abs(leftThumbXf) < threshold)
                    {
                        leftThumbXf = 0f;
                    }

                    if (Math.Abs(leftThumbYf) < threshold)
                    {
                        leftThumbYf = 0f;
                    }

                    if (Math.Abs(rightThumbXf) < threshold)
                    {
                        rightThumbXf = 0f;
                    }


                    if (leftTriggerf == 0f)
                    {
                        leftTriggerf = -1f;
                    }
                    else
                    {
                        leftTriggerf = leftTriggerf * 0.4f + 0.6f;
                    }

                    
                    if (rightTriggerf == 0f)
                    {
                        rightTriggerf = -1f;
                    }
                    else
                    {
                        rightTriggerf = rightTriggerf * 0.4f + 0.6f;
                    }

                    if (rightTriggerf > leftTriggerf)
                    {
                        leftTriggerf = rightTriggerf;
                    }

                    Throttle = QuadroControl.KonvertiereFloatZuHexString(leftTriggerf);
                    Yaw = QuadroControl.KonvertiereFloatZuHexString(rightThumbXf);
                    Roll = QuadroControl.KonvertiereFloatZuHexString(leftThumbXf);
                    Pitch = QuadroControl.KonvertiereFloatZuHexString(leftThumbYf);


                    //ManualControlCommand UAVTalk-Nachricht erstellen (Throttle = -1f - mehrmals)

                    //WICHTIG: die nullen nach Throttle sind die werte für ROLL PITCH YAW COLLECTIVE THRUST. Müssen später ergänzt werden

                    vSchickeHexString("3C 20 38 00 80 74 10 C4 00 00 " + Throttle + " " + Roll + " " + Pitch + " " + Yaw + " 00 00 00 00 " + Throttle + " 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00");

                    //AccsoryDesired setzen

                    //Arming

                    if (myState.Gamepad.Buttons == GamepadButtonFlags.Start)
                    {
                        vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 3F");
                        //Setze ArmingStatus auf GUI
                        GUIArming(true);
                    }

                    Thread.Sleep(20);

                    //Disarming

                    if (myState.Gamepad.Buttons == GamepadButtonFlags.Back && leftTriggerf < threshold)
                    {
                        vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF");
                        //Setze ArmingStatus auf GUI
                        GUIArming(false);
                    }


                    // NOT-Aus

                    if (myState.Gamepad.Buttons == GamepadButtonFlags.B)
                    {
                        vSchickeHexString("3C 20 38 00 80 74 10 C4 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00");

                        vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF");
                        //Setze ArmingStatus auf GUI
                        GUIArming(false);

                        GUINotAus(true);

                        HeartbeatThread.Abort();
                        ControllerThread.Abort();
                        
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
                    Label_ArmingStatus.Content = "armed";
                    Label_ArmingStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98));
                    el_aussen.Stroke = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98));
                    el_innen.Fill = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98));
                }
                else
                {
                    Label_ArmingStatus.Content = "disarmed";
                    Label_ArmingStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                    el_aussen.Stroke = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                    el_innen.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                }
            }
        }


        private void GUINotAus(bool status)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, GuiNotAusDel, status);
                return;
            }
            else
            {
                if (status)
                {
                    TB_Hinweis.Text = "NOT-Aus ausgelöst!\nStarte die Anwendung neu!";
                    //TB_COMPort.FontStyle = new FontStyle(FontStyles.Normal);
                    TB_DroneControl.Content = "NOT-Aus";
                    TB_DroneControl.Background = new SolidColorBrush(Color.FromArgb(255, 212, 98, 98));


                    TB_COMPort.Text = "";
                    Label_VerbindungsStatus.Content = "COMPort getrennt";
                    TB_COMPort.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

                    Label_ArmingStatus.Content = "disarmed";
                    Label_ArmingStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                    el_aussen.Stroke = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                    el_innen.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

                    BT_HS_HB.Background = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                    BT_XBoxRead.Background = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                }
                /*
                else
                {
                    Label_ArmingStatus.Content = "disarmed";
                    Label_ArmingStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                    el_aussen.Stroke = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                    el_innen.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                }
                */
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            vSchickeHexString("3C 20 38 00 80 74 10 C4 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00");

            vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF");
            //Setze ArmingStatus auf GUI
            GUIArming(false);

            ControllerThread.Abort();
            HeartbeatThread.Abort();
        }

        private void TB_COMPort_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TB_COMPort.Text = "";
        }

        private void TB_COMPort_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TB_COMPort.Text = "";
        }

        private void TB_COMPort_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TB_COMPort.Text = "";
        }

        private void TB_COMPort_TouchDown(object sender, TouchEventArgs e)
        {
            TB_COMPort.Text = "";
        }
    }
}
