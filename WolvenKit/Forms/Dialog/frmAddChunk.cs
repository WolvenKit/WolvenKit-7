using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WolvenKit.CR2W;
using WolvenKit.CR2W.Types;
using WolvenKit.CR2W.Reflection;
using System.Windows.Documents;

namespace WolvenKit
{
    public partial class frmAddChunk : Form
    {
        private string[] classTypes = null;
        private string[] enumTypes = null;
        private List<string> vanillaClasses = null;
        private List<string> customClasses = null;
        private List<string> vanillaEnums = null;
        private List<string> customEnums = null;
        public frmAddChunk(List<string> list = null, bool isVariant = false, bool allowEditName = false)
        {
            InitializeComponent();

            if (!isVariant)
            {
                flowLayoutVariants.Enabled = false;
                txTypeFinal.Enabled = false;
            }
            if (!allowEditName)
            {
                txName.Enabled = false;
            }
            if (list == null)
            {
                list = AssemblyDictionary.TypeNames;
            }
            list.Sort();

            customClasses = CR2WManager.TypeNames;
            
            if (customClasses?.Any() == true)
            {
                customClasses.Sort();
                classTypes = list.Concat(customClasses).Distinct().ToArray();
            }
            else
            {
                classTypes = list.ToArray();
            }
            

            vanillaEnums = AssemblyDictionary.EnumNames;
            vanillaEnums.Sort();
            customEnums = CR2WManager.EnumNames;
            

            if (customEnums?.Any() == true)
            {
                customEnums.Sort();
                enumTypes = vanillaEnums.Concat(customEnums).Distinct().ToArray();
            }
            else
            {
                enumTypes = vanillaEnums.ToArray();
            }


            UpdateTypeChoices();
        }

        private void UpdateTypeChoices()
        {
            txType.Items.Clear();
            if (checkEnum.Checked)
            {
                txType.Items.AddRange(enumTypes);
            }
            else
            {
                txType.Items.AddRange(classTypes);
            }
            if (txType.SelectedIndex < 0 && txType.Items.Count > 0)
                txType.SelectedIndex = 0;
        }

        private void SelectedTypeChanged(object sender,
        System.EventArgs e)
        {
            UpdateFinalType();
        }
        private void UpdateFinalType()
        {
            txTypeFinal.Text = string.Empty;
            if (checkArray.Checked)
            {
                txTypeFinal.Text += "array:2,0,";
            }
            if (checkHandle.Checked)
            {
                txTypeFinal.Text += "handle:";
            }
            else if (checkSoft.Checked)
            {
                txTypeFinal.Text += "soft:";
            }
            txTypeFinal.Text += txType.Text;
        }

        public string FinalType => txTypeFinal.Text;
        public string ArgType => txType.Text;
        public string VarName => txName.Text;

        private void checkArray_CheckedChanged(object sender, System.EventArgs e)
        {
            UpdateFinalType();
        }

        private void checkHandle_CheckedChanged(object sender, System.EventArgs e)
        {
            checkSoft.Enabled = !checkHandle.Checked && !checkEnum.Checked;
            checkEnum.Enabled = !checkHandle.Checked && !checkSoft.Checked;
            UpdateFinalType();
        }

        private void checkSoft_CheckedChanged(object sender, System.EventArgs e)
        {
            checkHandle.Enabled = !checkSoft.Checked && !checkEnum.Checked;
            checkEnum.Enabled = !checkSoft.Checked && !checkHandle.Checked;
            UpdateFinalType();
        }

        private void checkEnum_CheckedChanged(object sender, System.EventArgs e)
        {
            checkHandle.Enabled = !checkEnum.Checked && !checkSoft.Checked;
            checkSoft.Enabled = !checkEnum.Checked && !checkHandle.Checked;
            UpdateTypeChoices();
            UpdateFinalType();
        }
    }
}
