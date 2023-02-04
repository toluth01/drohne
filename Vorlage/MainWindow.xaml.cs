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

namespace Vorlage
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    /// 

    // declare delegates
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

        // thread for heartbeat
        private Thread HeartbeatThread;

        // thread for controller
        private Thread ControllerThread;

        // delegate object for GUIArming
        private GUIArmingDelType GuiArmingDel;

        // delegate object for GUINotAus
        private GUINotAusDelType GuiNotAusDel;

        // Controller und Controller State objekte
        Controller XBox;
        State myState;

        public MainWindow()
        {
            // NICHT ÄNDERN
            // -------------------------------------------------
            vmyQC = new vQuadroControl();
            DelStringObject = new DelString(vSchickeHexString);
            // -------------------------------------------------

            // automatisch erstellte Methode für Form Designer
            InitializeComponent();

            // instance of thread
            HeartbeatThread = new Thread(Heartbeat);
            ControllerThread = new Thread(ControllerFunktion);
            
            // instance of del
            GuiArmingDel = GUIArming;
            GuiNotAusDel = GUINotAus;
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

        // Thread Funktionen ----------------------------------------------------------------------------------------
        // Heartbeat Thread
        private void Heartbeat()
        {
            bool run = true;

            try
            {
                while (run)
                {
                    // Verschickt einen HexString zum Hearbeat an den FC
                    vSchickeHexString("3C 20 2F 00 0A DC D1 CA 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03");
                    Thread.Sleep(1000);
                }
            }
            catch (ThreadAbortException)
            {
                run = false; // Stoppt den Heartbeat Thread wenn ein Thread.Abort() ausgelöst wird
            }
        }

        // Controller Thread
        private void ControllerFunktion()
        {
            // aus dem Controller ausgelesene Werte für Trigger und Thumb
            int rightTrigger;
            int leftTrigger;
            int rightThumbX;
            int rightThumbY;
            int leftThumbX;
            int leftThumbY;

            // Variablen für die Übergabe an den FC
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

            float threshold = 0.2f; // threshold sorgt für einen Nullwert bei den Thumbs da Abweichung vom Nullwert

            bool run = true;

            try
            {
                while (run)
                {
                    // Sicherheitsfeature: Verschickt einen HexString mit Nullwerten, wenn der Xbox Controller disconnected wurde -> Drohne landet
                    if (!XBox.IsConnected)
                    {
                        vSchickeHexString("3C 20 38 00 80 74 10 C4 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00");
                        vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF");

                        GUIArming(false); //Setzt ArmingStatus auf GUI to false
                        return; // soll nicht mehr weiterlaufen
                    }
                    
                    // Liest die Conctroller State aus
                    myState = XBox.GetState();

                    // Liest Werte aus dem Controller aus
                    rightTrigger = myState.Gamepad.RightTrigger;
                    leftTrigger = myState.Gamepad.LeftTrigger;
                    rightThumbX = myState.Gamepad.RightThumbX;
                    leftThumbX = myState.Gamepad.LeftThumbX;
                    leftThumbY = myState.Gamepad.LeftThumbY;

                    // Verarbeitet die Werte und schreibt sie in die float Variablen rein
                    rightTriggerf = rightTrigger / 255f;
                    leftTriggerf = leftTrigger / 255f;
                    rightThumbXf = rightThumbX / 32768f;
                    leftThumbXf = leftThumbX / 32768f;
                    leftThumbYf = leftThumbY / 32768f;

                    // Sorgt für die Nullwerte bei den Thumbs, da Abweichung vom Nullwert
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

                    // Stellt bei Nullwerten die Variablen auf -1f für den FC (Nach Anleitung)
                    if (leftTriggerf == 0f)
                    {
                        leftTriggerf = -1f;
                    }
                    else
                    {
                        leftTriggerf = leftTriggerf * 0.4f + 0.6f; // Passt die Throttle Kurve an (BEI REALER DROHNE LÖSCHEN)
                    }

                    if (rightTriggerf == 0f)
                    {
                        rightTriggerf = -1f;
                    }
                    else
                    {
                        rightTriggerf = rightTriggerf * 0.4f + 0.6f; // Passt die Throttle Kurve an (BEI REALER DROHNE LÖSCHEN)
                    }

                    // Übergibt dem FC nur den größeren Wert der beiden Trigger
                    if (rightTriggerf > leftTriggerf)
                    {
                        leftTriggerf = rightTriggerf;
                    }

                    // Konvertiert die Werte zu einem HexString
                    Throttle = QuadroControl.KonvertiereFloatZuHexString(leftTriggerf);
                    Yaw = QuadroControl.KonvertiereFloatZuHexString(rightThumbXf);
                    Roll = QuadroControl.KonvertiereFloatZuHexString(leftThumbXf);
                    Pitch = QuadroControl.KonvertiereFloatZuHexString(leftThumbYf);

                    // Übergibt die Werte im HexString an den FC
                    vSchickeHexString("3C 20 38 00 80 74 10 C4 00 00 " + Throttle + " " + Roll + " " + Pitch + " " + Yaw + " 00 00 00 00 " + Throttle + " 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00");

                    //Arming
                    if (myState.Gamepad.Buttons == GamepadButtonFlags.Start)
                    {
                        vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 3F");
                        GUIArming(true); //Setze ArmingStatus auf GUI
                    }

                    //Disarming

                    if (myState.Gamepad.Buttons == GamepadButtonFlags.Back && leftTriggerf < threshold)
                    {
                        vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF");
                        GUIArming(false); //Setze ArmingStatus auf GUI
                    }


                    // Sicherheitsfeature: NOT-Aus

                    if (myState.Gamepad.Buttons == GamepadButtonFlags.B)
                    {
                        // Übergibt einen Null HexString -> Drohne landet
                        vSchickeHexString("3C 20 38 00 80 74 10 C4 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00");
                        // Übergibt einen HexString zum Disarmen -> Drohne disarmt
                        vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF");

                        GUIArming(false); //Setze ArmingStatus auf GUI

                        GUINotAus(true); //Setze NotAUs Mode auf GUI

                        HeartbeatThread.Abort(); // Heartbeat Thread wird gestoppt
                        ControllerThread.Abort(); // Controller Thread wird gestoppt

                        // App muss neugestartet werden
                    }

                    Thread.Sleep(20); // pausiert den Thread (nach Anleitung)
                }
            }
            catch (ThreadAbortException)
            {
                run = false; // Stoppt den Controller Thread wenn ein Thread.Abort() ausgelöst wird
            }
        }

        // Delegaten Funktionen ----------------------------------------------------------------------------------------
        // GUIArming Status
        private void GUIArming (bool status)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, GuiArmingDel, status); // Führt den Delegaten asynchron für den Thread aus
                return;
            }
            else
            {
                if (status)
                {
                    // Gearmed-Modus
                    Label_ArmingStatus.Content = "armed"; // passt den Text des Lables an
                    Label_ArmingStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98)); // stellt die Arming Status Vordergrundfarbe auf grün
                    el_aussen.Stroke = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98)); // stellt die Frabe der äußeren Elipse auf grün
                    el_innen.Fill = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98)); // stellt die Frabe der inneren Elipse auf grün
                }
                else
                {
                    // Disarmed-Modus
                    Label_ArmingStatus.Content = "disarmed"; // passt den Text des Lables an
                    Label_ArmingStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89)); // stellt die Arming Status Vordergrundfarbe auf blau
                    el_aussen.Stroke = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89)); // stellt die Frabe der äußeren Elipse auf blau
                    el_innen.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); // stellt die Frabe der inneren Elipse auf weiß
                }
            }
        }

        // GUINotAus Modus
        private void GUINotAus(bool status)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, GuiNotAusDel, status); // Führt den Delegaten asynchron für den Thread aus
                return;
            }
            else
            {
                if (status)
                {
                    TB_Hinweis.Text = "NOT-Aus ausgelöst!\nStarte die Anwendung neu!"; // passt den Text an im Header
                    TB_DroneControl.Content = "NOT-Aus"; // passt den Text an im ersten Block
                    TB_DroneControl.Background = new SolidColorBrush(Color.FromArgb(255, 212, 98, 98)); // ändert die Farbe auf rot

                    // reset für alle Buttons und Textblöcke
                    TB_COMPort.Text = "";
                    TB_COMPort.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

                    Label_ArmingStatus.Content = "disarmed";
                    Label_ArmingStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                    el_aussen.Stroke = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                    el_innen.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

                    BT_HS_HB.Background = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                    BT_XBoxRead.Background = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89));
                }
            }
        }

        // ButtonClick Funktionen -----------------------------------------------------------------------------------
        // COM-Port verbinden Button
        private void BT_COMPortVerbinden_Click(object sender, RoutedEventArgs e)
        {
            string COMPortName;

            COMPortName = TB_COMPort.Text;

            //COM-Port verbinden
            if (vmyQC.vVerbinde(COMPortName))
            {
                TB_COMPort.Text = TB_COMPort.Text + " verbunden"; // passt den text an
                TB_COMPort.Background = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98)); // stellt die Hintergrundfarbe auf grün
            }
            else
            {
                TB_COMPort.Background = new SolidColorBrush(Color.FromArgb(255, 212, 98, 98)); // stellt die Hintergrundfarbe auf rot
            }
        }

        //Com-Port trennen Button
        private void BT_COMPortTrennen_Click(object sender, RoutedEventArgs e)
        {
            if (vmyQC.vVerbindungTrennen())
            {
                TB_COMPort.Text = "COMPort getrennt"; // passt den text an
                TB_COMPort.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); // stellt die Hintergrundfarbe auf weiß

                Label_ArmingStatus.Content = "disarmed"; // passt den text an (arming label)
                Label_ArmingStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89)); // stellt die Arming Status Vordergrundfarbe auf blau
                el_aussen.Stroke = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89)); // stellt die Frabe der äußeren Elipse auf blau
                el_innen.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); // stellt die Frabe der inneren Elipse auf weiß

                BT_HS_HB.Background = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89)); // stellt die Farbe des Controller Verbinden Buttons auf blau
                BT_XBoxRead.Background = new SolidColorBrush(Color.FromArgb(255, 67, 81, 89)); // stellt die Farbe des Verbinden Buttons auf blau
            }
            else
            {
                TB_COMPort.Text = "Trennung FEHLGESCHLAGEN"; // passt den text an
            }
        }

        // Controller verbinden Button
        private void BT_HS_HB_Click(object sender, RoutedEventArgs e)
        {
            //Handshake

            vSchickeHexString("3C 20 2F 00 0A DC D1 CA 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
            vSchickeHexString("3C 20 2F 00 0A DC D1 CA 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01");
            vSchickeHexString("3C 20 2F 00 0A DC D1 CA 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03");
            vSchickeHexString("3c 20 12 00 81 74 10 c4 00 00 41 00 c0 07 64 00 00 00");
            vSchickeHexString("3c 20 12 00 5c 98 09 c4 00 00 41 00 d0 07 64 00 00 00");

            //Heartbeat

            if (HeartbeatThread.ThreadState == ThreadState.Unstarted) // prüft ob der Thread noch nicht gestartet wurde
            {
                HeartbeatThread.Start(); // startet den Heartbeat Thread
            }


            BT_HS_HB.Background = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98)); // stellt die Farbe des Controller verbinden Buttons auf grün

        }

        // Controller lesen Button
        private void BT_XBoxRead_Click(object sender, RoutedEventArgs e)
        {
            XBox = new Controller(UserIndex.One); // liest den Controller mit dem ersten Channel

            if (ControllerThread.ThreadState == ThreadState.Unstarted) // prüft ob der Thread noch nicht gestartet wurde
            {
                ControllerThread.Start(); // startet den Controller Thread
            }


            BT_XBoxRead.Background = new SolidColorBrush(Color.FromArgb(255, 128, 212, 98)); // stellt die Farbe des Controller lesen Buttons auf grün
        }

        // Stoppt die App beim schließen des Fensters
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            vSchickeHexString("3C 20 38 00 80 74 10 C4 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00");

            vSchickeHexString("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF");
            //Setze ArmingStatus auf GUI
            GUIArming(false);

            ControllerThread.Abort();
            HeartbeatThread.Abort();
        }

        // Löscht den Platzhaltertext im TB_COMPort mit einem Doppelclick
        private void TB_COMPort_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TB_COMPort.Text = "";
        }
    }
}
