using System.IO;
using System.Text;
using System.Windows.Forms;
using WolvenKit.Properties;

namespace WolvenKit
{
    public partial class frmWCCLicense : Form
    {
        public frmWCCLicense()
        {
            InitializeComponent();

            richTextBox1.Rtf = Resources.wcc_eula;
        }
    }
}
