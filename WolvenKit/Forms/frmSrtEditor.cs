using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using WolvenKit.App;
using WolvenKit.Common;
using WolvenKit.Common.Services;
using WolvenKit.CR2W.SRT;
using WolvenKit.CR2W.Types;
using WolvenKit.Services;
using WolvenKit.App.Model;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WolvenKit.Forms.Editors
{
    public partial class frmSrtEditor : DockContent, IThemedContent
    {
        private Srtfile SRT;
        public frmSrtEditor(string filepath)
        {
            InitializeComponent();
            ApplyCustomTheme();
            LoadSRT(filepath);
        }

        public void ApplyCustomTheme()
        {
            UIController.Get().ToolStripExtender.SetStyle(toolStrip, VisualStudioToolStripExtender.VsVersion.Vs2015, UIController.GetThemeBase());
        }

        #region Methods
        private void SetView()
        {
            this.textBox1.Text = "";
        }
        private void LoadSRT(string filepath)
        {
            AppendLine("[LoadSRT] Loading: " + filepath);
            using (var fstream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(fstream))
                {
                    SRT = new Srtfile()
                    {
                        FileName = Path.GetFileName(filepath)
                    };
                    var res = SRT.Read(reader);
                    AppendLine($"[LoadSRT] {res} read {fstream.Length} bytes, remainingBytes = " + SRT.debug_remainingbytes);
                }
                fstream.Close();
            }
            AppendLine($"[DEBUG] Geometry.StrShaderPath = {SRT.Geometry.StrShaderPath}");
            for (int i = 0; i < SRT.StringTable.Length; i += 1)
            {
                AppendLine($"[DEBUG] StringTable[{i}] = {SRT.StringTable[i]}");
            }
        }
         public void AppendLine(string line)
        {
            if (textBox1.Text.Length == 0)
                textBox1.Text = line;
            else
                textBox1.AppendText("\r\n" + line);
        }
        #endregion

        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "SRT|*.srt";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (var fstream = new FileStream(sfd.FileName, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (var writer = new BinaryWriter(fstream))
                    {
                        var res = SRT.Write2(writer);
                        AppendLine($"[SaveSRT] {(res ? "OK" : "ERROR")}, written {fstream.Length} bytes to: {sfd.FileName}");
                    }
                    fstream.Close();
                }
            }
        }

        private void toolStripButtonExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "JSON|*.json";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
                byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(SRT, options);
                File.WriteAllBytes(sfd.FileName, jsonUtf8Bytes);
                AppendLine($"[SerializeSRT] Written {jsonUtf8Bytes.Length} bytes to {sfd.FileName}");

                //string jsonString = JsonSerializer.Serialize(SRT, options);
                //File.WriteAllText(sfd.FileName, jsonString);
            }
        }

        private void toolStripButtonImport_Click(object sender, EventArgs e)
        {
            using (var of = new OpenFileDialog())
            {
                of.Multiselect = false;
                of.Filter = "SRT JSON dump|*.json;";
                of.Title = "Please select an srt json dump for importing";
                if (of.ShowDialog() == DialogResult.OK)
                {
                    byte[] jsonUtf8Bytes = File.ReadAllBytes(of.FileName);
                    var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
                    var utf8Reader = new Utf8JsonReader(jsonUtf8Bytes);
                    SRT = JsonSerializer.Deserialize<Srtfile>(ref utf8Reader, options);
                    AppendLine($"[DeserializeSRT] Read {jsonUtf8Bytes.Length} bytes from {of.FileName}");
                }
            }
        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {
            byte[] orig = new byte[0];
            byte[] edit = new byte[0];
            using (var of = new OpenFileDialog())
            {
                of.Multiselect = true;
                of.Filter = "SRT|*.srt;";
                of.Title = "Please select TWO srt files for comparing";
                if (of.ShowDialog() == DialogResult.OK)
                {
                    orig = File.ReadAllBytes(of.FileNames[0]);
                    edit = File.ReadAllBytes(of.FileNames[1]);
                    AppendLine($"ORIG: {of.FileNames[0]}");
                    AppendLine($"EDIT: {of.FileNames[1]}");
                }
            }
            int wrongA = 0, wrongB = 0;
            List<byte> bA = new List<byte>(), bB = new List<byte>();
            bool wrong = false;
            int wrongCount = 0;
            AppendLine($"LEN: {orig.Length}, EDIT LEN: {edit.Length}");
            for (int i = 0; i < edit.Length; i += 1)
            {
                if (orig[i] != edit[i])
                {
                    if (!wrong)
                    {
                        wrongA = i + 1;
                        wrongCount += 1;
                    }
                    wrongB = i + 1;
                    bA.Add(orig[i]);
                    bB.Add(edit[i]);
                    wrong = true;
                } else
                {
                    if (wrong)
                    {
                        AppendLine($"WRONG: [{wrongA} - {wrongB}]");
                        AppendLine($"Original: {string.Join("-", bA)}");
                        AppendLine($"Edited  : {string.Join("-", bB)}");
                    }
                    bA.Clear();
                    bB.Clear();
                    wrong = false;
                }
                //if (wrongCount > 10)
                //   return;
            }
            if (wrong)
            {
                AppendLine($"WRONG: [{wrongA} - {wrongB}]");
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            using (var of = new OpenFileDialog())
            {
                of.Multiselect = false;
                of.Filter = "SRT File|*.srt;";
                of.Title = "Please select srt file";
                if (of.ShowDialog() == DialogResult.OK)
                {
                    LoadSRT(of.FileName);
                }
            }
        }
    }
}
