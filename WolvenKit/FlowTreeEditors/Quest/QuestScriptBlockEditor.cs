using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WolvenKit.CR2W;
using WolvenKit.CR2W.Types;
using WolvenKit.FlowTreeEditors;

namespace WolvenKit {
    public class QuestScriptEditor : QuestLinkEditor {
        private Label script;

        public QuestScriptEditor() {
            script = new Label();
            script.Anchor = AnchorStyles.Left | AnchorStyles.Right| AnchorStyles.Top;
            script.AutoEllipsis = true;
            script.BackColor = Color.White;
            script.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            script.ForeColor = SystemColors.ActiveCaptionText;
            script.Location = new Point(0, 0);
            script.Name = "script";
            script.Size = new Size(313, 18);
            script.TabIndex = 0;
            script.Text = "script";
            script.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(script);
        }
        
        
        public override void UpdateView() {
            base.UpdateView();

            lblTitle.Text = Chunk.REDName + " : " + Chunk.Preview;
            script.Text = getScript();
            script.Location = Location + new Size(0, lblTitle.Height);
            Size measureText = TextRenderer.MeasureText(lblTitle.Text, lblTitle.Font);
            Size measureScript = TextRenderer.MeasureText(script.Text, script.Font);
            Size = new Size( Math.Max(measureScript.Width, measureText.Width),lblTitle.Height + script.Height);
        }

        private string getScript() {
            string ret = "";

            if (Chunk.data is CQuestScriptBlock block)
            {
                CName functionName = block.FunctionName;

                ret += functionName.Value + "(";
                CArray<QuestScriptParam> parameters = block.Parameters;

                QuestScriptParam last = parameters.LastOrDefault();

                foreach (var parameter in parameters)
                {
                    CName name = parameter.Name;
                    CVariant variant = parameter.Value;
                    ret += name + ":" + variant;
                    if (parameter != last)
                    {
                        ret += ", ";
                    }

                }

                ret += ")";

            }

            

            return ret;
        }

       
    }
}