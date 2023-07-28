﻿using BrightIdeasSoftware;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using WolvenKit.Bundles;
using WolvenKit.Cache;
using WolvenKit.Common;
using WolvenKit.Common.Model;

namespace WolvenKit
{
    public partial class frmAssetBrowser : DockContent
    {
        public List<string> Autocompletelist;
        private readonly List<IWitcherFile> FileList = new List<IWitcherFile>();
        private readonly IEnumerable<IWitcherArchiveManager> Managers;

        public List<string> Files { get; set; }
        private WitcherTreeNode ActiveNode { get; set; }
        private WitcherTreeNode RootNode { get; set; }

        private List<WitcherListViewItem> SelectedPaths
        {
            get
            {
                var ret = new List<WitcherListViewItem>();
                foreach (WitcherListViewItem item in pathlistview.Items)
                    ret.Add(item);
                return ret;
            }
        }

        public event EventHandler<AddFileArgs> RequestFileAdd;

        public frmAssetBrowser(List<IWitcherArchiveManager> managers)
        {
            InitializeComponent();

            this.Icon = UIController.GetThemedWkitIcon();


            pathlistview.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            pathlistview.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            Managers = managers;
            RootNode = new WitcherTreeNode();
            RootNode.Name = "Root";
            foreach (var manager in managers)
            {
                if (manager == null || manager.RootNode == null)
                    continue;

                FileList.AddRange(manager.FileList.Where(_ => _ != null));
                RootNode.Directories[manager.RootNode.Name] = manager.RootNode;
                manager.RootNode.Parent = RootNode;

                bundleExtensionCB.Items.Add(manager.TypeName);
                bundleExtensionCB.SelectedIndex = 0;

                for (int i = 0; i < manager.AutocompleteSource.Count; i++)
                {
                    SearchBox.AutoCompleteCustomSource.Add(manager.AutocompleteSource[i]);
                }

                // fill in all file extensions
                FillFileExtensionCB(manager);
            }
        }

        private void FillFileExtensionCB(IWitcherArchiveManager manager)
        {
            var fileExtensions = manager.Extensions;
            fileExtensions.Sort();
            fileExtensionsCB.Items.AddRange(fileExtensions.ToArray());
            fileExtensionsCB.SelectedIndex = 0;
        }

        private void frmBundleExplorer_Load(object sender, EventArgs e)
        {
        }

        private void OpenNode(WitcherTreeNode node,bool reset = false)
        {
            if (ActiveNode == node && !reset) return;

            ActiveNode = node;

            UpdatePathPanel();

            fileListView.Items.Clear();
            List<WitcherListViewItem> res = new List<WitcherListViewItem>();
            if (node.Parent != null)
            {
                res.Add(new WitcherListViewItem
                {
                    Node = node.Parent,
                    Text = "..",
                    IsDirectory = true,
                    ImageKey = "FolderOpened"
                });
            }

            res.AddRange(node.Directories.OrderBy(x => x.Key).Select(item => new WitcherListViewItem
            {
                Node = item.Value,
                Text = item.Key,
                IsDirectory = true,
                ImageKey = "FolderOpened"
            }));


            foreach (var item in node.Files.OrderBy(x => x.Key))
            {
                var lastItem = item.Value[item.Value.Count - 1];
                var listItem = new WitcherListViewItem
                {
                    Node = node,
                    RelativePath = lastItem.Name,
                    Text = item.Key,
                    IsDirectory = false,
                    ImageKey = GetImageKey(item.Key)
                };
                listItem.SubItems.Add(lastItem.Size.ToString());
                listItem.SubItems.Add($"{(100 - (int) (lastItem.ZSize / (float) lastItem.Size * 100.0f))}%");
                listItem.SubItems.Add(lastItem.CompressionType);
                listItem.SubItems.Add(lastItem.Bundle.TypeName.ToString());
                res.Add(listItem);
            }
            fileListView.Items.AddRange(res.ToArray());
        }

        private void UpdatePathPanel()
        {
            var nodes = new List<WitcherTreeNode>();
            var c = ActiveNode;
            while (c != null)
            {
                nodes.Add(c);
                c = c.Parent;
            }

            pathPanel.Controls.Clear();
            var x = 0;
            nodes.Reverse();
            foreach (var link in nodes)
            {
                var button = new Label();
                button.Text = link.Name + " >";
                button.Location = new Point(x, -3);
                button.Padding = new Padding(0);
                button.Margin = new Padding(0);
                var textSize = TextRenderer.MeasureText(button.Text, button.Font);
                button.Width = textSize.Width;
                button.Height = 23;
                button.BackColor = Color.Transparent;
                button.MouseLeave += button_MouseLeave;
                button.MouseEnter += button_MouseEnter;
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.Cursor = Cursors.Hand;
                button.Click += delegate { OpenNode(link); };

                pathPanel.Controls.Add(button);

                x += button.Width;
            }
        }

        private void button_MouseEnter(object sender, EventArgs e)
        {
            var label = (Label) sender;
            label.BackColor = Color.LightBlue;
        }

        private void button_MouseLeave(object sender, EventArgs e)
        {
            var label = (Label) sender;
            label.BackColor = Color.Transparent;
        }

        private void fileListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (fileListView.SelectedItems.Count > 0)
            {
                var item = (WitcherListViewItem) fileListView.SelectedItems[0].Clone();
                if (item.IsDirectory)
                {
                    OpenNode(item.Node);
                }
                else
                {
                    var cont = false;
                    foreach (WitcherListViewItem i in pathlistview.Items)
                    {
                        if (i.Text == item.RelativePath)
                            cont = true;
                    }
                    if (!cont)
                    {
                        var tempnode = new WitcherListViewItem
                        {
                            ImageKey = GetImageKey(item.RelativePath),
                            Text = item.RelativePath,
                            ToolTipText = item.RelativePath,
                            IsDirectory = item.IsDirectory,
                            Node = item.Node,
                            RelativePath = item.RelativePath
                        };
                        pathlistview.Items.Add(tempnode);
                    }
                }
            }
        }

        private void pathPanel_Click(object sender, EventArgs e)
        {
            pathPanel.Controls.Clear();
            var textbox = new TextBox
            {
                Location = new Point(16, 2),
                Width = pathPanel.Width,
                Height = pathPanel.Height,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BorderStyle = BorderStyle.None
            };
            pathPanel.Controls.Add(textbox);
            textbox.Focus();

            textbox.PreviewKeyDown += textbox_PreviewKeyDown;
            textbox.LostFocus += textbox_LostFocus;
            textbox.Margin = new Padding(3);
            textbox.Text = ActiveNode.FullPath;
            textbox.SelectAll();
            textbox.AcceptsReturn = true;
        }

        private void textbox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            var textbox = (TextBox) sender;
            if (e.KeyCode == Keys.Enter)
            {
                var browsePath = textbox.Text;
                OpenPath(browsePath);
            }
        }

        private string GetImageKey(string filename)
        {
            try
            {
                if (treeImages.Images.ContainsKey(Path.GetExtension(filename).Replace(".", "")))
                {
                    return Path.GetExtension(filename).Replace(".", "");
                }
            }
            catch  { }

            return "genericFile";
        }

        public void OpenPath(string browsePath)
        {
            var currentNode = RootNode;
            var parts = browsePath.Split('\\');
            if (parts.Length == 1 && parts[0] == "Root")
            {
                OpenNode(RootNode);
                return;
            }
            if (parts.Length > 0 && parts[0] == "Root")
                parts = parts.Skip(1).ToArray();
            var success = false;
            for (var i = 0; i < parts.Length; i++)
            {
                if (currentNode.Directories.ContainsKey(parts[i]))
                {
                    currentNode = currentNode.Directories[parts[i]];

                    success = true;
                }
                else if (i == parts.Length - 1 && (currentNode.Files.ContainsKey(parts[i]) || parts[i] == ""))
                {
                    success = true;
                }
                else
                {
                    success = false;
                    break;
                }
            }

            if (success || currentNode != RootNode)
            {
                OpenNode(currentNode);
            }
            else
            {
                OpenNode(RootNode);
            }
        }

        private void textbox_LostFocus(object sender, EventArgs e)
        {
            UpdatePathPanel();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in pathlistview.SelectedItems)
            {
                pathlistview.Items.Remove(item);
            }
        }

        private void Search(string s, int bundleTypeIdx, int fileTypeIdx)
        {
            var extension = "";
            string bundletype = "";
            s = s.Replace("/", "\\");

            if (fileExtensionsCB.SelectedIndex != -1)
                extension = fileExtensionsCB.Items[fileTypeIdx].ToString();
            if (bundleExtensionCB.SelectedIndex != -1)
                bundletype = bundleExtensionCB.Items[bundleTypeIdx].ToString();
            var found = SearchFiles(FileList.ToArray(), s, bundletype, extension);
            if (limitCheckBox.Checked)
            {
                int limit = (int)limitUpDown.Value;
                if (found.Length > limit)
                    found = found.Take(limit).ToArray();
            }
            fileListView.Items.Clear();
            var results = new List<WitcherListViewItem>();
            foreach (var file in found)
            {
                var listItem = new WitcherListViewItem(file.Item2);
                listItem.SubItems.Add(file.Item2.Size.ToString());
                listItem.SubItems.Add($"{(100 - (int) (file.Item2.ZSize/(float)file.Item2.Size*100.0f))}%");
                listItem.SubItems.Add(file.Item2.CompressionType);
                listItem.SubItems.Add(file.Item2.Bundle.TypeName.ToString());
                results.Add(listItem);
            }
            fileListView.Items.AddRange(results.ToArray());
        }

        private Tuple<WitcherListViewItem,IWitcherFile>[] SearchFiles(IWitcherFile[] files, string searchkeyword, string bundletypestr, string extension)
        {
            if(bundletypestr=="Any") bundletypestr="ANY";
            EBundleType bundletype = (EBundleType)Enum.Parse(typeof(EBundleType), bundletypestr);

            if (regexCheckbox.Checked)
            {
                try
                {
                    if (currentfolderCheckBox.Checked)
                    {
                        return files.Where(item => item.Bundle.ArchiveAbsolutePath.Contains(ActiveNode.Name) && new Regex(searchkeyword).IsMatch(item.Name)).Select(x => new Tuple<WitcherListViewItem, IWitcherFile>(new WitcherListViewItem(x), x)).ToArray();
                    }

                    return files.Where(item => new Regex(searchkeyword).IsMatch(item.Name)).Select(x => new Tuple<WitcherListViewItem, IWitcherFile>(new WitcherListViewItem(x), x)).ToArray();
                }
                catch
                {
                    //TODO: Log
                }
            }
            if (currentfolderCheckBox.Checked)
            {
                return caseCheckBox.Checked
                    ? files.Where(item => item.Bundle.ArchiveAbsolutePath.Contains(ActiveNode.Name)
                        && item.Name.ToUpper().Contains(searchkeyword.ToUpper())
                        && (item.Name.ToUpper().EndsWith(extension.ToUpper()) || extension.ToUpper() == "ANY")
                        && (item.Bundle.TypeName == bundletype || bundletype == EBundleType.ANY))
                    .Distinct().Select(x => new Tuple<WitcherListViewItem, IWitcherFile>(new WitcherListViewItem(x), x)).ToArray()
                    : files.Where(item => item.Bundle.ArchiveAbsolutePath.Contains(ActiveNode.Name)
                        && item.Name.Contains(searchkeyword)
                        && (item.Name.EndsWith(extension) || extension.ToUpper() == "ANY")
                        && (item.Bundle.TypeName == bundletype || bundletype == EBundleType.ANY))
                    .Distinct().Select(x => new Tuple<WitcherListViewItem, IWitcherFile>(new WitcherListViewItem(x), x)).ToArray();
            }
            return caseCheckBox.Checked
                ? files.Where(item => item.Name.ToUpper().Contains(searchkeyword.ToUpper())
                    && (item.Name.ToUpper().EndsWith(extension.ToUpper()) || extension.ToUpper() == "ANY")
                    && (item.Bundle.TypeName == bundletype || bundletype == EBundleType.ANY))
                    .Select(x => new Tuple<WitcherListViewItem, IWitcherFile>(new WitcherListViewItem(x), x)).ToArray()
                : files.Where(item => item.Name.Contains(searchkeyword)
                    && (item.Name.EndsWith(extension) || extension.ToUpper() == "ANY")
                    && (item.Bundle.TypeName == bundletype || bundletype == EBundleType.ANY))
                    .Select(x => new Tuple<WitcherListViewItem, IWitcherFile>(new WitcherListViewItem(x), x)).ToArray();
        }

        /// <summary>
        /// Recursively collects files into an array, descending depth first from a given root node.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <returns>Array of collected files.</returns>
        private WitcherListViewItem[] CollectFiles(WitcherTreeNode root)
        {
            var collectedFilesList = new List<WitcherListViewItem>();

            foreach (var fileList in root.Files)
            {
                foreach (var file in fileList.Value)
                {
                    collectedFilesList.Add(new WitcherListViewItem(file));
                }
            }

            foreach (var dr in root.Directories)
            {
                collectedFilesList.AddRange(CollectFiles(dr.Value));
            }

            return collectedFilesList.ToArray();
        }

        private void MarkSelected_Click(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count > 0)
            {
                foreach (WitcherListViewItem item in fileListView.SelectedItems)
                {
                    if (!item.IsDirectory)
                    {
                        var cont = false;
                        foreach (WitcherListViewItem i in pathlistview.Items)
                        {
                            if (i.Text == item.RelativePath)
                                cont = true;
                        }
                        if (!cont)
                        {
                            var tempnode = new WitcherListViewItem
                            {
                                ImageKey = GetImageKey(item.RelativePath),
                                Text = item.RelativePath,
                                ToolTipText = item.RelativePath,
                                IsDirectory = item.IsDirectory,
                                Node = item.Node,
                                RelativePath = item.RelativePath
                            };
                            pathlistview.Items.Add(tempnode);
                        }
                    }
                }
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            Search(SearchBox.Text, bundleExtensionCB.SelectedIndex, fileExtensionsCB.SelectedIndex);
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            pathlistview.Items.Clear();
        }

        private void fileListView_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Back)
            {
                if (ActiveNode != RootNode)
                    OpenNode(ActiveNode.Parent);
            }
            if (e.KeyCode == Keys.Enter)
            {
                if (fileListView.SelectedItems.Count > 0)
                {
                    var item = (WitcherListViewItem)fileListView.SelectedItems[0];
                    if (item.IsDirectory)
                    {
                        OpenNode(item.Node);
                    }
                    else
                    {
                        var tempnode = new WitcherListViewItem
                        {
                            ImageKey = GetImageKey(item.RelativePath),
                            Text = item.RelativePath,
                            ToolTipText = item.RelativePath,
                            IsDirectory = item.IsDirectory,
                            Node = item.Node,
                            RelativePath = item.RelativePath
                        };
                        pathlistview.Items.Add(tempnode);
                    }
                }
            }
        }

        private void SearchBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Search(SearchBox.Text, bundleExtensionCB.SelectedIndex, fileExtensionsCB.SelectedIndex);
            }
        }

        private void btOpen_Click(object sender, EventArgs e)
        {
            if (pathlistview.Items.Count < 1)
            {
                if (fileListView.SelectedItems.Count > 0)
                {
                    foreach (WitcherListViewItem item in fileListView.SelectedItems)
                    {
                        if (!item.IsDirectory)
                        {
                            var cont = false;
                            foreach (WitcherListViewItem i in pathlistview.Items)
                            {
                                if (i.Text == item.RelativePath)
                                    cont = true;
                            }
                            if (!cont)
                            {
                                var tempnode = new WitcherListViewItem
                                {
                                    ImageKey = GetImageKey(item.RelativePath),
                                    Text = item.RelativePath,
                                    ToolTipText = item.RelativePath,
                                    IsDirectory = item.IsDirectory,
                                    Node = item.Node,
                                    RelativePath = item.RelativePath
                                };
                                pathlistview.Items.Add(tempnode);
                            }
                        }
                    }
                }
            }
            RequestFileAdd?.Invoke(this, new AddFileArgs(Managers, SelectedPaths, false, checkBoxUncook.Checked, checkBoxExport.Checked, checkUseLastBundle.Checked));
            pathlistview.Items.Clear();
        }

        private void AddDLCFile_Click(object sender, EventArgs e)
        {
            if (pathlistview.Items.Count < 1)
            {
                if (fileListView.SelectedItems.Count > 0)
                {
                    foreach (WitcherListViewItem item in fileListView.SelectedItems)
                    {
                        if (!item.IsDirectory)
                        {
                            var cont = false;
                            foreach (WitcherListViewItem i in pathlistview.Items)
                            {
                                if (i.Text == item.RelativePath)
                                    cont = true;
                            }
                            if (!cont)
                            {
                                var tempnode = new WitcherListViewItem
                                {
                                    ImageKey = GetImageKey(item.RelativePath),
                                    Text = item.RelativePath,
                                    ToolTipText = item.RelativePath,
                                    IsDirectory = item.IsDirectory,
                                    Node = item.Node,
                                    RelativePath = item.RelativePath
                                };
                                pathlistview.Items.Add(tempnode);
                            }
                        }
                    }
                }
            }
            RequestFileAdd?.Invoke(this, new AddFileArgs(Managers, SelectedPaths, true, checkBoxUncook.Checked, checkBoxExport.Checked, checkUseLastBundle.Checked));
            pathlistview.Items.Clear();
        }

        private void copyPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count > 0)
            {
                var item = ((WitcherListViewItem) fileListView.SelectedItems[0]);
                if (item?.IsDirectory == false)
                {
                    Clipboard.SetText(item.RelativePath);
                }
            }
        }

        private void markToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count > 0)
            {
                foreach (WitcherListViewItem item in fileListView.SelectedItems)
                {
                    if (!item.IsDirectory)
                    {
                        var cont = false;
                        foreach (WitcherListViewItem i in pathlistview.Items)
                        {
                            if (i.Text == item.RelativePath)
                                cont = true;
                        }
                        if (!cont)
                        {
                            var tempnode = new WitcherListViewItem
                            {
                                ImageKey = GetImageKey(item.RelativePath),
                                Text = item.RelativePath,
                                ToolTipText = item.RelativePath,
                                IsDirectory = item.IsDirectory,
                                Node = item.Node,
                                RelativePath = item.RelativePath
                            };
                            pathlistview.Items.Add(tempnode);
                        }
                    }
                }
            }
        }

        private void fileListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                fileListView.MultiSelect = true;
                foreach (WitcherListViewItem item in fileListView.Items)
                {
                    item.Selected = true;
                }
            }
        }

        private void clearSearch_Click(object sender, EventArgs e)
        {
            SearchBox.Clear();
            OpenNode(RootNode,true);
        }

        private void markAllFilesOfFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count > 0)
            {
                foreach (WitcherListViewItem item in fileListView.SelectedItems)
                {
                    if (item.IsDirectory)
                    {
                        var files = CollectFiles(item.Node);
                        //Remove duplicates.
                        var distinctFiles = files.GroupBy(x => x.RelativePath).Select(x => x.First()).ToArray();
                        if (files.Length > 1000)
                            pathlistview.Items.AddRange(distinctFiles.Take(1000).ToArray());
                        else
                            pathlistview.Items.AddRange(distinctFiles);
                    }
                }
            }
        }

        private void detailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fileListView.View = View.Details; 
        }

        private void largeIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fileListView.View = View.LargeIcon;
        }

        private void smallIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fileListView.View = View.SmallIcon;
        }

        private void listToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fileListView.View = View.List;
        }

        private void tileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fileListView.View = View.Tile;
        }

        private void Home_Click(object sender, EventArgs e)
        {
            OpenPath("Root");
        }

        private void checkBoxExport_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxExport.Checked)
                checkBoxUncook.Checked = true;
        }

        private void bundleExtensionCB_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (((ComboBox)sender).SelectedItem is EBundleType type)
            {
                fileExtensionsCB.Items.Clear();

                var specificManagers = Managers;
                if (type != EBundleType.ANY)
                    specificManagers = Managers.Where(_ => _.TypeName == type);

                foreach (var manager in specificManagers)
                {
                    FillFileExtensionCB(manager);
                }
            }
        }
    }
}
