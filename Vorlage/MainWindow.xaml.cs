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

namespace Vorlage
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    /// 

    public delegate void DelString(string hexString);

    public partial class MainWindow : Window
    {
        // NICHT ÄNDERN
        // ----------------------------
        private vQuadroControl vmyQC;
        DelString DelStringObject;
        // ----------------------------


        public MainWindow()
        {
            // NICHT ÄNDERN
            // -------------------------------------------------
            vmyQC = new vQuadroControl();
            DelStringObject = new DelString(vSchickeHexString);
            // -------------------------------------------------

            InitializeComponent();

        }

        // NICHT ÄNDERN
        // ----------------------------------------------------------------------------------------------------------
        public void vSchickeHexString(String hexString)
        {
            if (!Dispatcher.CheckAccess())
            {Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, DelStringObject, hexString);}
            else {vmyQC.v_SchickeHexString(hexString); }
        }
        // ----------------------------------------------------------------------------------------------------------

    }
}
