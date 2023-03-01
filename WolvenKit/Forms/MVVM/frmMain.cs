using AutoUpdaterDotNET;
using Dfust.Hotkeys;
using Microsoft.VisualBasic.FileIO;
using SharpPresence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;
using WeifenLuo.WinFormsUI.Docking;
using WolvenKit.App.Model;
using WolvenKit.Forms.Editors;
using SearchOption = System.IO.SearchOption;

namespace WolvenKit
{
    //using Common.Services;
    using App;
    using App.Commands;
    using App.ViewModels;
    using Bundles;
    using Common;
    using Common.Extensions;
    using Common.Model;
    using Common.Wcc;
    using CR2W;
    using CR2W.Types;
    using Extensions;
    using Forms;
    using Microsoft.WindowsAPICodePack.Dialogs;
#if !USE_RENDER
    using Render;
#endif
    using Scaleform;
    using System.Globalization;
    using WolvenKit.CR2W.Reflection;
    using Wwise.Player;
    using Enums = Enums;

    public partial class frmMain : Form
    {
        private const string BaseTitle = "Wolven kit";

#region Fields
        private readonly MainViewModel vm;

        private frmProgress ProgressForm { get; set; }
        //private List<frmImagePreview> OpenImages { get; } = new List<frmImagePreview>();

        private readonly HotkeyCollection hotkeys;
        private readonly ToolStripRenderer toolStripRenderer = new ToolStripProfessionalRenderer();

        private delegate void BoolDelegate(bool t);
        private delegate void StrDelegate(string t);
        private delegate void ColorDelegate(Color t);
        private delegate void LogDelegate(string t, WolvenKit.Common.Services.Logtype type);
        private delegate void IntDelegate(int t);

        private readonly Queue<string> lastClosedTab = new Queue<string>();
        private DeserializeDockContent m_deserializeDockContent;
        private WolvenKit.Common.Services.LoggerService Logger { get; set; }
        private static string Version => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

#endregion

#region Properties
        private W3Mod ActiveMod
        {
            get => MainController.Get().ActiveMod;
            set
            {
                MainController.Get().ActiveMod = value ?? throw new ArgumentNullException(nameof(value));
                UpdateTitle();
            }
        }

#endregion

#region Constructor
        public frmMain()
        {
            vm = MockKernel.Get().GetMainViewModel();
            vm.PropertyChanged += ViewModel_PropertyChanged;

            InitializeComponent();

            UpdateTitle();
            MainController.Get().PropertyChanged += MainControllerUpdated;

            
            hotkeys = new HotkeyCollection(Enums.Scope.Application);
            hotkeys.RegisterHotkey(Keys.Control | Keys.S, HKSave, "Save");
            hotkeys.RegisterHotkey(Keys.Control | Keys.Shift | Keys.S, HKSaveAll, "SaveAll");
            hotkeys.RegisterHotkey(Keys.F1, HKHelp, "Help");
            

            hotkeys.RegisterHotkey(Keys.F5, HKRun, "Run");
            hotkeys.RegisterHotkey(Keys.Control | Keys.F5, HKRunAndLaunch, "RunAndLaunch");
            hotkeys.RegisterHotkey(Keys.Control | Keys.W, HKCloseTab, "CloseTab");
            hotkeys.RegisterHotkey(Keys.Control | Keys.Shift | Keys.T, HKReopenTab, "ReopenTab");

            UIController.InitForm(this);

            MainBackgroundWorker.WorkerReportsProgress = true;
            MainBackgroundWorker.WorkerSupportsCancellation = true;
            MainBackgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            MainBackgroundWorker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            MainBackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);


            ToolStripManager.LoadSettings(this);
            m_deserializeDockContent = new DeserializeDockContent(MockKernel.Get().GetContentFromPersistString);
            this.FormBorderStyle = FormBorderStyle.None;
            menuStrip1.Show();

            visualStudioToolStripExtender1.DefaultRenderer = toolStripRenderer;
            UIController.Get().ToolStripExtender = visualStudioToolStripExtender1;

            watcher.Error += Watcher_Error;
            filePaths = new List<string>();
            rwlock = new ReaderWriterLockSlim();

            this.toolStripDropDownButtonGit.Paint += toolStripDropDownButtonGit_Paint;
        }

#endregion

#region Methods
        /// <summary>
        /// Opens a document in the background
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="memoryStream"></param>
        /// <param name="suppressErrors"></param>
        private IWolvenkitView LoadDocument(string filename, MemoryStream memoryStream = null, bool suppressErrors = false)
        {
            if (memoryStream == null && !File.Exists(filename))
                return null;

            var existing = TryOpenExisting(filename);
            if (existing != null) return existing;


            // switch between cr2w files and non-cr2w files (e.g. srt)
            if (Path.GetExtension(filename) == ".srt")
            {
                var doc = new frmOtherDocument(new CommonDocumentViewModel(UIController.Get().WindowFactory));

                doc.Activated += doc_Activated;
                doc.Show(dockPanel, DockState.Document);
                doc.FormClosed += doc_FormClosed;

                return doc;
            }
            else
            {


                //var doc = Args.Doc;
                frmCR2WDocument doc = new frmCR2WDocument(new CR2WDocumentViewModel(UIController.Get().WindowFactory));
                doc.WorkerLoadFileSetup(new LoadFileArgs(filename, memoryStream));


                doc.Activated += doc_Activated;
                doc.Show(dockPanel, DockState.Document);
                doc.FormClosed += doc_FormClosed;

                doc.PostLoadFile(filename, bool.Parse(renderW2meshToolStripMenuItem.Tag.ToString()));

                vm.AddOpenDocument(doc.GetViewModel());

                return doc;
            }
        }

        /// <summary>
        /// Creates a new W3Mod
        /// </summary>
        /// <returns></returns>
        public W3Mod CreateNewMod()
        {
            if (!vm.CloseAllDocuments()) return null;

            var dlg = new SaveFileDialog
            {
                Title = @"Create Witcher 3 Mod Project",
                Filter = @"Witcher 3 Mod|*.w3modproj",
                InitialDirectory = MainController.Get().Configuration.InitialModDirectory
            };

            while (dlg.ShowDialog() == DialogResult.OK)
            {
                if (dlg.FileName.Contains(' '))
                {
                    MessageBox.Show(
                        @"The mod path should not contain spaces because wcc_lite.exe will have trouble with that.",
                        "Invalid path");
                    continue;
                }

                MainController.Get().Configuration.InitialModDirectory = Path.GetDirectoryName(dlg.FileName);
                var modname = Path.GetFileNameWithoutExtension(dlg.FileName);
                var dirname = Path.GetDirectoryName(dlg.FileName);

                var moddir = Path.Combine(dirname, modname);
                try
                {
                    Directory.CreateDirectory(moddir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to create mod directory: \n" + moddir + "\n\n" + ex.Message);
                    return null;
                }

                ActiveMod = new W3Mod
                {
                    FileName = dlg.FileName,
                    Name = modname
                };
                // create default directories
                ActiveMod.CreateDefaultDirectories();
                watcher.Path = ActiveMod.FileDirectory;
                ResetWindows();

                // detect if radish-mod
                var filedir = new DirectoryInfo(MainController.Get().ActiveMod.ProjectDirectory).Parent;
                var radishdir = filedir.GetFiles("*.bat", SearchOption.AllDirectories)?.FirstOrDefault(_ => _.Name == "_settings_.bat")?.Directory;
                if (radishdir != null)
                {
                    switch (MessageBox.Show(
                        "WolvenKit detected a radish mod project installation in this directory. Would you like to add the radish files to the Mod Project?",
                        "Radish Tool Integration",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                    {
                        default:
                            return null;
                        case DialogResult.Yes:
                            {
                                if (!Directory.Exists(ActiveMod.RadishDirectory))
                                    Directory.CreateDirectory(ActiveMod.RadishDirectory);
                                //move radish files into Modfiledir
                                foreach (var file in radishdir.GetFiles("*", SearchOption.TopDirectoryOnly))
                                {
                                    File.Move(file.FullName, Path.Combine(ActiveMod.RadishDirectory, file.Name));
                                }
                                foreach (var dir in radishdir.GetDirectories("*", SearchOption.TopDirectoryOnly))
                                {
                                    if (dir.FullName == ActiveMod.ProjectDirectory)
                                        continue;
                                    Directory.Move(dir.FullName, Path.Combine(ActiveMod.RadishDirectory, dir.Name));
                                }
                                break;
                            }
                        case DialogResult.No:
                            {
                                break;
                            }
                    }
                }
                vm.SaveMod();
                Logger.LogString("\"" + ActiveMod.Name + "\" sucesfully created and loaded!\n", Common.Services.Logtype.Success);
                break;
            }

            return ActiveMod;
        }

        /// <summary>
        /// Deserializes a w3modproj File and loads the W3mod
        /// </summary>
        /// <param name="file"></param>
        public void OpenMod(string file = "")
        {
            if (!vm.CloseAllDocuments())
                return;

            //Opening the file from a dialog
            if (string.IsNullOrEmpty(file))
            {
                var s = UIController.Get().WindowFactory.ShowOpenFileDialog("Open Witcher 3 Mod Project", "Witcher 3 Mod|*.w3modproj",
                    MainController.Get().Configuration.InitialModDirectory);
                if (!string.IsNullOrEmpty(s))
                    file = s;
                else
                    return;
            }

            var old = XDocument.Load(file);
#region Upgrade from w3edit
            try
            {
                if (old.Descendants("InstallAsDLC").Any())
                {
                    //This is an old "Sarcen's W3Edit"-project. We need to upgrade it.
                    //Put the files into their respective folder.
                    switch (MessageBox.Show(
                        "The project you are opening has been made with an older version of Wolven Kit or Sarcen's Witcher 3 Edit.\n" +
                        "It needs to be upgraded for use with Wolvenkit.\n" +
                        "To load as a mod please press yes. To load as a DLC project please press no.\n" +
                        "You can manually do the upgrade if you check the project structure: https://github.com/Traderain/Wolven-kit/wiki/Project-structure press cancel if you desire to do so. This may not always work but I tried my best.",
                        "Out of date project", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                    {
                        default:
                            return;
                        case DialogResult.Yes:
                            {
                                Commonfunctions.DirectoryMove(Path.Combine(Path.GetDirectoryName(file), old.Root.Element("Name").Value, "files"),
                                    Path.Combine(Path.GetDirectoryName(file), old.Root.Element("Name").Value, "files", "Mod", EBundleType.Bundle.ToString()));
                                break;
                            }
                        case DialogResult.No:
                            {
                                Commonfunctions.DirectoryMove(Path.Combine(Path.GetDirectoryName(file), old.Root.Element("Name").Value, "files"),
                                    Path.Combine(Path.GetDirectoryName(file), old.Root.Element("Name").Value, "files", "DLC", EBundleType.Bundle.ToString()));
                                break;
                            }

                    }
                    //Upgrade the project xml
                    var nw = new W3Mod
                    {
                        Name = old.Root.Element("Name")?.Value,
                        FileName = file,
                    };

                    File.Delete(file);
                    XmlSerializer xs = new XmlSerializer(typeof(W3Mod));
                    var mf = new FileStream(file, FileMode.Create);
                    xs.Serialize(mf, nw);
                    mf.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to upgrade the project!\n" + ex, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endregion



            MainController.Get().Configuration.InitialModDirectory = Path.GetDirectoryName(file);

            // Loading the project
            var ser = new XmlSerializer(typeof(W3Mod));
            using (var modfile = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                ActiveMod = (W3Mod)ser.Deserialize(modfile);
                ActiveMod.FileName = file;
                ActiveMod.CreateDefaultDirectories();
                watcher.Path = ActiveMod.FileDirectory;
            }

            ResetWindows();
            Logger.LogString("\"" + ActiveMod.Name + "\" loaded successfully!\n", Common.Services.Logtype.Success);
            MainController.Get().ProjectStatus = EProjectStatus.Ready;

#region upgrade from older mod projects
            if (!old.Descendants("Version").Any() || (old.Descendants("Version").Any()
                                                      && int.TryParse(old.Descendants("Version").First().Value, out int version)
                                                      && version < 0.62))
            {
                MessageBox.Show(
                    "The project you are opening has been made with an older version of Wolven Kit.\n" +
                    "Some folder names have changed:\n" +
                    "CollisionCache and TextureCache have been unified into one Uncooked directory.\n" +
                    "Bundle has been renamed to Cooked",
                    "Out of date project", MessageBoxButtons.OK, MessageBoxIcon.Information);


                // check if any directories are misnamed
                // remap CollisionCache ===> Uncooked
                // remap TextureCache   ===> Uncooked
                // remap Bundle         ===> Cooked
                // mod
                MoveFiles(EBundleType.CollisionCache, EProjectFolders.Uncooked);
                MoveFiles(EBundleType.TextureCache, EProjectFolders.Uncooked);
                MoveFiles(EBundleType.Bundle, EProjectFolders.Cooked);

                // dlc
                MoveFiles(EBundleType.CollisionCache, EProjectFolders.Uncooked, true);
                MoveFiles(EBundleType.TextureCache, EProjectFolders.Uncooked, true);
                MoveFiles(EBundleType.Bundle, EProjectFolders.Cooked, true);


                //Upgrade the project xml
                File.Delete(file);
                XmlSerializer xs = new XmlSerializer(typeof(W3Mod));
                var mf = new FileStream(file, FileMode.Create);
                xs.Serialize(mf, ActiveMod);
                mf.Close();
            }


#endregion

            // Hash all filepaths
            var relativepaths = ActiveMod.ModFiles
                .Select(_ => _.Substring(_.IndexOf(Path.DirectorySeparatorChar) + 1))
                .ToList();
            Cr2wResourceManager.Get().RegisterAndWriteCustomPaths(relativepaths);

            // register all custom classes
            CR2WManager.Init(ActiveMod.FileDirectory, MainController.Get().Logger);

            // update import
            MockKernel.Get().GetImportViewModel().UseLocalResourcesCommand.SafeExecute();

            RepopulateRecentFiles(file);

            void MoveFiles(EBundleType oldtype, EProjectFolders newtype, bool isDlc = false)
            {
                var di = new DirectoryInfo(Path.Combine(isDlc ? ActiveMod.DlcDirectory : ActiveMod.ModDirectory, oldtype.ToString()));
                if (di.Exists && di.GetFiles("*", SearchOption.AllDirectories).Any())
                {
                    // move all files from old folders to new
                    foreach (var fi in di.GetFiles("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var relpath = fi.FullName.Substring(di.FullName.Length + 1);
                            var newpath = Path.Combine(isDlc ? ActiveMod.DlcDirectory : ActiveMod.ModDirectory, newtype.ToString(), relpath);
                            if (!File.Exists(newpath))
                            {
                                Directory.CreateDirectory(new FileInfo(newpath).Directory.FullName);
                                File.Move(fi.FullName, newpath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogString($"Couldn't move file {fi.FullName}. Please manually upgrade your mod project.", Common.Services.Logtype.Error);
                        }
                    }
                }
                // delete old directories
                if (di.Exists && !di.GetFiles("*", SearchOption.AllDirectories).Any())
                {
                    di.Delete(true);
                }
            }
        }
#endregion

#region UI Methods
        public DockPanel GetDockPanel() => dockPanel;
        /// <summary>
        /// Closes all the "file documents", resets modexplorer and clears the output.
        /// </summary>
        public void ResetWindows()
        {
            if (isDockPanelInitialized)
                SaveDockPanelLayout();

            if (!CloseWindows()) return;

            InitDockPanel();
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateInstaller()
        {
            //TODO:UPDATE
            //var cif = new frmCreateInstaller();
            //cif.ShowDialog();
        }

        //TODO: rework this for ViewModels
        /// <summary>
        /// Closes and saves all the "file documents", resets modexplorer.
        /// </summary>
        private bool CloseWindows()
        {
            if (!vm.CloseAllDocuments())
                return false;

            MockKernel.Get().CloseMainWindows();

            
            //TODO: unused
            //foreach (var t in OpenImages.ToList())
            //{
            //    t.Close();
            //}

            foreach (var window in dockPanel.FloatWindows.ToList())
            {
                window.Close();
                window.Dispose();
            }

            // close misc doc windows (like a docked asset browser)
            foreach (var doc in dockPanel.Documents.ToList())
            {
                if (!(doc is DockContent dc)) continue;
                dc.Close();
                dc.Dispose();
            }

            return true;
        }

        //TODO: rework this for ViewModels
        private IWolvenkitView TryOpenExisting(string key)
        {
            // check if already open
            var opendocs2 = dockPanel.Documents
                .Where(_ => _ is IWolvenkitView);

            foreach (var dockContent in opendocs2)
            {
                if (dockContent is IWolvenkitView iview && iview.FileName == key)
                {
                    iview?.Activate();
                    return iview;
                }
            }

            // check on the viewmodel
            //foreach (var t in GetOpenDocuments().Values.Where(_ => _.FileName == key))
            //{
            //    t.Activate();
            //    return true;
            //}

            return null;
        }

        private void SaveDockPanelLayout() => dockPanel.SaveAsXml(Path.Combine(Path.GetDirectoryName(Configuration.ConfigurationPath), "main_layout.xml"));
        private bool isDockPanelInitialized;
        private void InitDockPanel()
        {
            ApplyCustomTheme();
            string config = Path.Combine(Path.GetDirectoryName(Configuration.ConfigurationPath), "main_layout.xml");
            if (System.IO.File.Exists(config))
            {
                try
                {
                    dockPanel.LoadFromXml(config, m_deserializeDockContent);
                    dockPanel.Theme.Extender.FloatWindowFactory = new CustomFloatWindowFactory();
                }
                catch (Exception exception)
                {
                    MockKernel.Get().ShowWindows();
                    System.Console.WriteLine(exception);
                }
            }
            else
            {
                MockKernel.Get().ShowWindows();
                SaveDockPanelLayout();
            }

            isDockPanelInitialized = true;

            
        }

        public void GlobalApplyTheme()
        {
            ResetWindows();
        }
        private void ApplyCustomTheme()
        {
            var theme = UIController.GetThemeBase();
            this.dockPanel.Theme = theme;
            visualStudioToolStripExtender1.SetStyle(menuStrip1, VisualStudioToolStripExtender.VsVersion.Vs2015, theme);

            //visualStudioToolStripExtender1.SetStyle(statusToolStrip, VisualStudioToolStripExtender.VsVersion.Vs2015, new VS2015LightTheme());
            //statusToolStrip.BackColor = SystemColors.HotTrack;

            visualStudioToolStripExtender1.SetStyle(toolbarToolStrip, VisualStudioToolStripExtender.VsVersion.Vs2015, theme);

            switch (UIController.GetColorTheme)
            {
                case EColorThemes.VS2015Light:
                case EColorThemes.VS2015Blue:
                    this.iconToolStripMenuItem.Image = new Bitmap(UIController.GetIconByKey(EAppIcons.Wkit_dark));
                    this.aboutRedkit2ToolStripMenuItem.Image = new Bitmap(UIController.GetIconByKey(EAppIcons.Wkit_dark));
                    break;
                case EColorThemes.VS2015Dark:
                    this.iconToolStripMenuItem.Image = new Bitmap(UIController.GetIconByKey(EAppIcons.Wkit_light));
                    this.aboutRedkit2ToolStripMenuItem.Image = new Bitmap(UIController.GetIconByKey(EAppIcons.Wkit_light));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#region UI formborderstyle none

        private const long WS_SYSMENU = 0x00080000L;
        private const long WS_BORDER = 0x00800000L;
        private const long WS_SIZEBOX = 0x00040000L;
        private const long WS_CHILD = 0x40000000L;
        private const long WS_DLGFRAME = 0x00400000L;
        private const long WS_MAXIMIZEBOX = 0x00010000L;
        private const long WS_CAPTION = 0x00C00000L;
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32")]
        public static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [System.Runtime.InteropServices.DllImport("user32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                {
                    cp.Style |= (int)WS_BORDER;
                    cp.Style |= (int)WS_SYSMENU;
                    cp.Style |= (int)WS_SIZEBOX;
                }
                return cp;
            }
        }
        private void MinimizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void RestoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Screen screen = Screen.FromControl(this);
            //int x = screen.WorkingArea.X - screen.Bounds.X;
            //int y = screen.WorkingArea.Y - screen.Bounds.Y;
            //this.MaximizedBounds = new Rectangle(x, y,
            //    screen.WorkingArea.Width, screen.WorkingArea.Height);

            if (this.WindowState != FormWindowState.Maximized)
                WindowState = FormWindowState.Maximized;
            else
                WindowState = FormWindowState.Normal;
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        // https://stackoverflow.com/a/42806834
        protected override void WndProc(ref Message m)
        {
            Boolean handled = false;

            switch (m.Msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(m.HWnd, m.LParam);

                    handled = true;
                    break;
            }
            m.Result = IntPtr.Zero;
            if (handled) DefWndProc(ref m); else base.WndProc(ref m);
        }

        private const int WINDOW_BORDER_BUFFER = 10;
        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left) - WINDOW_BORDER_BUFFER;
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top) - WINDOW_BORDER_BUFFER;
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left) + (2 * WINDOW_BORDER_BUFFER);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top) + (2 * WINDOW_BORDER_BUFFER);
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>x coordinate of point.</summary>
            public int x;
            /// <summary>y coordinate of point.</summary>
            public int y;
            /// <summary>Construct a point of coordinates (x,y).</summary>
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            public static readonly RECT Empty = new RECT();
            public int Width { get { return Math.Abs(right - left); } }
            public int Height { get { return bottom - top; } }
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
            public RECT(RECT rcSrc)
            {
                left = rcSrc.left;
                top = rcSrc.top;
                right = rcSrc.right;
                bottom = rcSrc.bottom;
            }
            public bool IsEmpty { get { return left >= right || top >= bottom; } }
            public override string ToString()
            {
                if (this == Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }
            public override bool Equals(object obj)
            {
                if (!(obj is System.Windows.Rect)) { return false; }
                return (this == (RECT)obj);
            }
            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode() => left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2) { return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom); }
            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2) { return !(rect1 == rect2); }
        }

        private void UpdateTitle()
        {
            var compileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc);
            wkitVersionToolStripLabel.Text = $"v{Version}: {compileTime.ToString("yyyy MMMM dd")}";

            modNameToolStripLabel.Text = ActiveMod != null ? ActiveMod.Name : "No Mod Loaded!";

            Text = BaseTitle;
            if (ActiveMod != null)
            {
                Text += " [" + ActiveMod.Name + "] ";
            }

            if (vm.ActiveDocument != null)
            {
                Text += Path.GetFileName(vm.ActiveDocument.FileName);
            }
        }

#endregion
#endregion

#region BackGroundWorker
        Func<object, DoWorkEventArgs, object> workerAction;
        //Func<object, object> workerCompletedAction;
        void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bwAsync = sender as BackgroundWorker;
            e.Result = workerAction(sender, e);

            // add a result
            //e.Result
        }
        void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressForm?.SetProgressBarValue(e.ProgressPercentage, e.UserState);
        }
        //IWolvenkitView HACK_bwform = null;
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // has errors
            if (e.Error != null)
            {
                // do not continue to the completed action
            }
            else // has completed successfully
            {
                //if (workerCompletedAction != null)
                //{
                //    HACK_bwform = (IWolvenkitView)workerCompletedAction(e.Result);
                //}
                //workerCompletedAction = null;
            }

            ProgressForm.Close();
        }

#endregion

#region HotKeys
        private void HKRun(HotKeyEventArgs e)
        {
            var pack = vm.PackAndInstallMod();
            while (!pack.IsCompleted)
                Application.DoEvents();
        }
        private void HKRunAndLaunch(HotKeyEventArgs e)
        {
            var pack = vm.PackAndInstallMod();
            while (!pack.IsCompleted)
                Application.DoEvents();

            if (!pack.Result)
                return;
            MainViewModel.ExecuteGame();
        }
        private void HKCloseTab(HotKeyEventArgs e)
        {
            if (vm.ActiveDocument != null)
            {
                lastClosedTab.Enqueue(vm.ActiveDocument.FileName);
                vm.ActiveDocument.Close();
            }
            //if (ActiveDocument != null)
            //{
            //    lastClosedTab.Enqueue(ActiveDocument.FileName);
            //    ActiveDocument.Close();
            //}
        }
        private void HKReopenTab(HotKeyEventArgs e)
        {
            if (lastClosedTab.Count > 0)
            {
                string filetoopen = lastClosedTab.Dequeue();
                if (!string.IsNullOrEmpty(filetoopen))
                    LoadDocument(filetoopen);
            }
        }
        private void HKSave(HotKeyEventArgs e) => vm.SaveActiveFile();

        private void HKSaveAll(HotKeyEventArgs e) => vm.SaveAllFiles();

        private static void HKHelp(HotKeyEventArgs e) => Process.Start("https://github.com/Traderain/Wolven-kit/wiki");

#endregion

#region Events
        //private void Welcome_FormClosed(object sender, FormClosedEventArgs e)
        //{
        //    Welcome = null;
        //}

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vm = (MainViewModel) sender;

            switch (e.PropertyName)
            {
                case "IsToolStripBtnPackEnabled":
                {
                    Invoke(new BoolDelegate(ToolStripBtnEnableToggle), vm.IsToolStripBtnPackEnabled);
                    break;
                }
            }

            void ToolStripBtnEnableToggle(bool v) => toolStripBtnPack.Enabled = v;
        }

        /// <summary>
        /// Deprecated. Use MainController.QueueLog 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoggerUpdated(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Log":
                    Invoke(new LogDelegate(AddOutput), ((WolvenKit.Common.Services.LoggerService) sender).Log + "\n",
                        ((WolvenKit.Common.Services.LoggerService) sender).Logtype);
                    break;
                case "Progress":
                {
                    if (MainBackgroundWorker != null)
                    {
                        if (string.IsNullOrEmpty(Logger.Progress.Item2))
                            MainBackgroundWorker.ReportProgress((int)Logger.Progress.Item1);
                        else
                            MainBackgroundWorker.ReportProgress((int)Logger.Progress.Item1, Logger.Progress.Item2);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Occurs when something in the maincontroller is updated that is INotifyProeprtyChanged
        /// Thread safe and always should be
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainControllerUpdated(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ProjectStatus":
                    switch (((MainController)sender).ProjectStatus)
                    {
                        
                        case EProjectStatus.Busy:
                            Invoke(new ColorDelegate(SetStatusBarColor), Color.FromArgb(202, 81, 0));
                            break;
                        case EProjectStatus.Errored:
                        case EProjectStatus.Idle:
                        case EProjectStatus.Ready:
                        default:
                            Invoke(new ColorDelegate(SetStatusBarColor), SystemColors.HotTrack);
                            break;
                    }
                    Invoke(new StrDelegate(SetStatusLabelText), ((MainController)sender).ProjectStatus.ToString());
                    break;
                case "LogMessage":
                    Invoke(new LogDelegate(AddOutput), ((MainController)sender).LogMessage.Key + "\n",
                        ((MainController)sender).LogMessage.Value);
                    break;
                case "StatusProgress":
                    Invoke(new IntDelegate(SetStatusProgressbarValue), ((MainController)sender).StatusProgress);
                    break;
            }

            void SetStatusLabelText(string text) => statusLBL.Text = text;
            void SetStatusProgressbarValue(int val) => toolStripProgressBar1.Value = val;
            void SetStatusBarColor(Color color) => this.statusToolStrip.BackColor = color;
        }
        private void AddOutput(string text, WolvenKit.Common.Services.Logtype type = WolvenKit.Common.Services.Logtype.Normal)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            ((frmOutput)MockKernel.Get().GetOutput()).AddText(text, type);

            //if (Output != null && !Output.IsDisposed)
            //{
            //    Output.AddText(text, type);
            //}
        }
        private void ClearOutput()
        {
            ((frmOutput)MockKernel.Get().GetOutput()).Clear();
            //if (Output != null && !Output.IsDisposed)
            //{
            //    Output.Clear();
            //}
        }

        /// <summary>
        /// Opens the asset browser in the background
        /// </summary>
        /// <param name="loadmods">Load the mod files</param>
        /// <param name="browseToPath">The path to browse to</param>
        private void OpenAssetBrowser(bool loadmods, string browseToPath = "")
        {
            if (ActiveMod == null)
            {
                MessageBox.Show(@"Please create a new mod project."
                    , "Missing Mod Project"
                    , MessageBoxButtons.OK
                    , MessageBoxIcon.Information);
                return;
            }
            if (Application.OpenForms.OfType<frmAssetBrowser>().Any())
            {
                var frm = Application.OpenForms.OfType<frmAssetBrowser>().First();
                if (!string.IsNullOrEmpty(browseToPath))
                    frm.OpenPath(browseToPath);
                frm.WindowState = FormWindowState.Minimized;
                frm.Show();
                frm.WindowState = FormWindowState.Normal;
                return;
            }
            var managers = MainController.Get().GetManagers(loadmods);

            //if (MainController.Get().ModCollisionManager != null) managers.Add(MainController.Get().ModCollisionManager);

            var explorer = new frmAssetBrowser(managers);
            explorer.RequestFileAdd += Assetbrowser_FileAdd;
            explorer.OpenPath(browseToPath);
            Point location = dockPanel.Location;
            location.X += (dockPanel.Size.Width / 2 - explorer.Size.Width / 2);
            location.Y += (dockPanel.Size.Height / 2 - explorer.Size.Height / 2);
            Rectangle floatWindowBounds = new Rectangle() { Location = location, Width = 827, Height = 564 };
            explorer.Show(dockPanel, floatWindowBounds);

        }

        private void Assetbrowser_FileAdd(object sender, AddFileArgs Details)
        {

            if (Process.GetProcessesByName("Witcher3").Length != 0)
            {
                MessageBox.Show(@"Please close The Witcher 3 before tinkering with the files!", "", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return;
            }

            MainController.Get().ProjectStatus = EProjectStatus.Busy;

            // Backgroundworker
            if (!MainBackgroundWorker.IsBusy)
            {
                PauseMonitoring();

                // progress bar
                ProgressForm = new frmProgress()
                {
                    Text = "Adding Assets",
                    StartPosition = FormStartPosition.CenterParent,
                };

                // background worker action
                workerAction = WorkerAssetBrowserAddFiles;
                MainBackgroundWorker.RunWorkerAsync(Details);

                // cancellation dialog
                DialogResult dr = ProgressForm.ShowDialog(this);
                switch (dr)
                {
                    case DialogResult.Cancel:
                        {
                            MainBackgroundWorker.CancelAsync();
                            ProgressForm.Cancel = true;
                            break;
                        }
                    case DialogResult.None:
                    case DialogResult.OK:
                    case DialogResult.Abort:
                    case DialogResult.Retry:
                    case DialogResult.Ignore:
                    case DialogResult.Yes:
                    case DialogResult.No:
                    default:
                        break;
                }
                ResumeMonitoring();
                vm.SaveMod();
                this.BringToFront();
                MainController.Get().ProjectStatus = EProjectStatus.Ready;
            }
            else
            {
                Logger.LogString("The background worker is currently busy.\r\n", Common.Services.Logtype.Error);
                MainController.Get().ProjectStatus = EProjectStatus.Errored;
            }

            

        }

        private object WorkerAssetBrowserAddFiles(object sender, DoWorkEventArgs e)
        {
            object arg = e.Argument;
            if (!(arg is AddFileArgs))
                throw new NotImplementedException();
            var Details = (AddFileArgs)arg;
            BackgroundWorker bwAsync = sender as BackgroundWorker;

            // setup working dir
            if (Directory.Exists(Path.GetFullPath(MainController.WorkDir)))
                Directory.Delete(Path.GetFullPath(MainController.WorkDir), true);
            Directory.CreateDirectory(Path.GetFullPath(MainController.WorkDir));

            var count = Details.SelectedPaths.Count;
            List<string> prioritizedBundles = new List<string>();
			
            BundleManager BundleManager = (BundleManager)Details.Managers.First(_ => _.TypeName == EBundleType.Bundle);
            if (Details.UseLastBundle && BundleManager != null)
            {
                foreach (var bundle in BundleManager.Bundles.Values)
                {
                    prioritizedBundles.Add(bundle.ArchiveAbsolutePath);
                }
                prioritizedBundles.Sort((a, b) => b.CompareTo(a)); // descending sort -> first will be selected
            }

            for (int i = 0; i < count; i++)
            {
                if (bwAsync.CancellationPending || ProgressForm.Cancel)
                {
                    Logger.LogString("Background worker cancelled.\r\n", Common.Services.Logtype.Error);
                    e.Cancel = true;
                    return false;
                }

                WitcherListViewItem item = Details.SelectedPaths[i];
                IWitcherArchiveManager manager = Details.Managers.First(_ => _.TypeName == item.BundleType);

                vm.AddToMod(item.RelativePath, manager, prioritizedBundles, Details.AddAsDLC, Details.Uncook, Details.Export);

                int percentprogress = (int)((float)i / (float)count * 100.0);
                MainBackgroundWorker.ReportProgress(percentprogress, item.Text);
            }
            return true;
        }

        /// <summary>
        ///  Fires when the ModExplorer FilesystemWatcher triggers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFileChange(object sender, RequestFilesChangeArgs e)
        {
            // Trigger re-compilation of custom user classes
            if (e.Files.Any(_ => Path.GetExtension(_) == ".ws"))
                CR2WManager.ReloadAssembly(MainController.Get().Logger);

            // Update the Import Utility
            MockKernel.Get().GetImportViewModel().UseLocalResourcesCommand.SafeExecute();
        }

        public void ModExplorer_RequestFileDelete(object sender, RequestFileDeleteArgs e)
        {
            PauseMonitoring();

            // ignore the main directories
            var deletablefiles = new List<string>();
            foreach (var item in e.Files)
            {
                if (!(item == ActiveMod.ModDirectory
                      || item == ActiveMod.DlcDirectory
                      || item == ActiveMod.RawDirectory
                      || item == ActiveMod.RadishDirectory
                      || item == ActiveMod.ModCookedDirectory
                      || item == ActiveMod.ModUncookedDirectory
                      || item == ActiveMod.DlcCookedDirectory
                      || item == ActiveMod.DlcUncookedDirectory
                    ))
                {
                    deletablefiles.Add(item);
                }
            }

            // delete the rest
            foreach (var filename in deletablefiles)
            {
                // Close open documents
                foreach (var t in vm.GetOpenDocuments().Values.Where(t => t.FileName == filename))
                {
                    t.Close();
                    break;
                }

                // Delete from file structure
                var fullpath = Path.Combine(ActiveMod.FileDirectory, filename);

                if (File.Exists(fullpath))
                {
                    try
                    {
                        FileSystem.DeleteFile(fullpath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        //File.Delete(fullpath);
                    }
                    catch (Exception exception)
                    {
                        MainController.LogString("Failed to delete " + fullpath + "!\r\n", Common.Services.Logtype.Error);
                        throw;
                    }
                }
                else if (Directory.Exists(fullpath))
                {
                    try
                    {
                        FileSystem.DeleteDirectory(fullpath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        //Directory.Delete(fullpath, true);
                    }
                    catch (Exception exception)
                    {
                        MainController.LogString("Failed to delete " + fullpath + "!\r\n", Common.Services.Logtype.Error);
                        throw;
                    }
                }
                else
                {
                    MainController.LogString("Request delete " + fullpath + "!\r\n", Common.Services.Logtype.Important);
                }
            }

            ResumeMonitoring();
            vm.SaveMod();
        }

        public void ModExplorer_RequestFileRename(object sender, RequestFileOpenArgs e)
        {
            
            var filename = e.File;

            if (!File.Exists(filename))
                return;

            var dlg = new frmRenameDialog() { FileName = filename };
            if (dlg.ShowDialog() == DialogResult.OK && dlg.FileName != filename)
            {
                var newfullpath = Path.Combine(ActiveMod.FileDirectory, dlg.FileName);

                if (File.Exists(newfullpath))
                    return;

                // Rename file in file structure
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newfullpath));
                }
                catch
                {
                }
                File.Move(filename, newfullpath);
            }
        }

        public void ModExplorer_RequestFastRender(object sender, RequestFileOpenArgs e)
        {
#if !USE_RENDER
            Render.FastRender.frmFastRender ren = new Render.FastRender.frmFastRender(e.File, Logger, ActiveMod);
            ren.Show(this.dockPanel, DockState.Document);
#endif
        }

        public void ModExplorer_RequestAssetBrowser(object sender, RequestFileOpenArgs e) => OpenAssetBrowser(false, e.File);

        public void ModExplorer_RequestFileOpen(object sender, RequestFileOpenArgs e)
        {
            var fullpath = e.File;

            var ext = Path.GetExtension(fullpath).ToUpper();

            // click
            if (e.Inspect)
            {
                switch (ext)
                {
                    case ".CSV":
                    case ".XML":
                    case ".TXT":
                    case ".BAT":
                    case ".WS":
                    case ".YML":
                    case ".LOG":
                    case ".INI":
                        {
                            var existing = TryOpenExisting(fullpath);
                            if (existing == null)
                            {
                                MockKernel.Get().ShowScriptPreview().LoadFile(fullpath);
                            }
                            break;
                        }
                    case ".PNG":
                    case ".JPG":
                    case ".TGA":
                    case ".BMP":
                    case ".JPEG":
                    case ".DDS":
                        {
                            //TODO: unused
                            //if (OpenImages.Any(_ => _.Text == Path.GetFileName(fullpath)))
                            //{
                            //    OpenImages.First(_ => _.Text == Path.GetFileName(fullpath)).Activate();
                            //}
                            //else
                            {
                                MockKernel.Get().ShowImagePreview().SetImage(fullpath);
                            }
                            break;
                        }
                    default:
                        break;
                }
                return;
            }

            // double click
            switch (ext)
            {
                // images
                case ".PNG":
                case ".JPG":
                case ".TGA":
                case ".BMP":
                case ".JPEG":
                case ".DDS":
                //text
                case ".CSV":
                case ".XML":
                case ".TXT":
                case ".WS":
                // other
                case ".FBX":
                case ".XCF":
                case ".PSD":
                case ".APB":
                case ".APX":
                case ".CTW":
                case ".BLEND":
                case ".ZIP":
                case ".RAR":
                case ".BAT":
                case ".YML":
                case ".LOG":
                case ".INI":
                    ShellExecute(fullpath);
                    break;

                case ".WEM":
                    {
                        using (var sp = new frmAudioPlayer(fullpath))
                        {
                            sp.ShowDialog();
                        }
                        break;
                    }
                case ".SUBS":
                    PolymorphExecute(fullpath, ".txt");
                    break;
                case ".USM":
                    LoadUsmFile(fullpath);
                    break;
                case ".SRT":
                    LoadSrtFile(fullpath);
                    break;
                case ".BNK":
                    {
                        using (var sp = new frmAudioPlayer(fullpath))
                        {
                            sp.ShowDialog();
                        }
                        break;
                    }
                default:
                    LoadDocument(fullpath);
                    break;
            }

            void ShellExecute(string path)
            {
                try
                {
                    var proc = new ProcessStartInfo(path) {UseShellExecute = true};
                    Process.Start(proc);
                }
                catch (Win32Exception winex)
                {
                    // eat this: no default app set for filetype
                    Logger.LogString($"No default prgram set in Windows to open file extension {Path.GetExtension(path)}", Common.Services.Logtype.Error);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            void PolymorphExecute(string path, string extension)
            {
                File.WriteAllBytes(Path.GetTempPath() + "asd." + extension, new byte[] { 0x01 });
                var programname = new StringBuilder();
                NativeMethods.FindExecutable("asd." + extension, Path.GetTempPath(), programname);
                if (programname.ToString().ToUpper().Contains(".EXE"))
                {
                    Process.Start(programname.ToString(), path);
                }
                else
                {
                    throw new InvalidFileTypeException("Invalid file type");
                }
            }

            void LoadUsmFile(string path)
            {
                if (!File.Exists(path) || Path.GetExtension(path) != ".usm")
                    return;
                var usmplayer = new frmUsmPlayer(path);
                usmplayer.Show(dockPanel, DockState.Document);

            }
            void LoadSrtFile(string path)
            {
                if (!File.Exists(path) || Path.GetExtension(path) != ".srt")
                    return;
                var srtEditor = new frmSrtEditor(path);
                srtEditor.Show(dockPanel, DockState.Document);

            }
        }
#endregion

#region UI Events


        private void frmMain_Load(object sender, EventArgs e)
        {
            //Load/Setup the config
            var exit = false;
            var config = MainController.Get().Configuration;
            while (!File.Exists(config.ExecutablePath) || !Directory.Exists(config.GameModDir) || !Directory.Exists(config.GameDlcDir))
            {
                var sets = new frmSettings();
                if (sets.ShowDialog() != DialogResult.OK)
                {
                    exit = true;
                    break;
                }
            }

            MainController.Get().ProjectStatus = EProjectStatus.Ready;
            if (exit)
            {
                Visible = false;
                Close();
            }

            //Start loading if everything is set up.
            using (var frmload = new frmLoading())
            {
                var result = frmload.ShowDialog();
            }

            Logger = MainController.Get().Logger;
            Logger.PropertyChanged += LoggerUpdated;


            // Initialize DockPanel
            MockKernel.Get().InitWindows();

            //Update check should be after we are all set up. It goes on in the background.
            switch (MainController.Get().Configuration.UpdateChannel)
            {
                case EUpdateChannel.Nightly:
                    AutoUpdater.Start("https://raw.githubusercontent.com/Traderain/Wolven-kit/master/NightlyUpdate.xml");
                    break;
                case EUpdateChannel.Stable:
                default:
                    AutoUpdater.Start("https://raw.githubusercontent.com/Traderain/Wolven-kit/master/Update.xml");
                    break;
            }

            
            richpresenceworker.RunWorkerAsync();

            ResetWindows();


            if (MainController.Get().Configuration.IsWelcomeFormDisabled)
            {
                return;
            }

            if (!string.IsNullOrEmpty(MainController.Get().InitialFilePath))
            {
                return;
            }

            using (var frmwelcome = new frmWelcome(this))
            {
                var result = frmwelcome.ShowDialog();
                switch (result)
                {
                    case DialogResult.Abort:
                        Close();
                        break;
                    default:
                        break;
                }
            }

            MainController.Get().StatusProgress = 100;

        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            richpresenceworker.CancelAsync();
            if (MainController.Get().ProjectUnsaved)
            {
                var res = MessageBox.Show("There are unsaved changes in your project. Would you like to save them?", "WolvenKit",
                    System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                    System.Windows.Forms.MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    vm.SaveAllFiles();
                    vm.SaveMod();
                }
                else if (res == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
                else
                {

                }
            }

        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            var uiconfig = UIController.Get().Configuration;

            uiconfig.MainState = WindowState;

            WindowState = FormWindowState.Normal;
            uiconfig.MainSize = Size;
            uiconfig.MainLocation = Location;

            uiconfig.Save();
            MainController.Get().Configuration.Save();

            SaveDockPanelLayout();
            ToolStripManager.SaveSettings(this);

            foreach (var dc in dockPanel.DocumentsToArray())
            {
                dc.DockHandler.DockPanel = null;
                dc.DockHandler.Close();
            }
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            var config = UIController.Get().Configuration;
            Size = config.MainSize;
            Location = config.MainLocation;
            WindowState = config.MainState;

            if (!string.IsNullOrEmpty(MainController.Get().InitialModProject))
            {
                OpenMod(MainController.Get().InitialModProject);
            }
            else if (!string.IsNullOrEmpty(MainController.Get().InitialWKP))
            {
                using (var pi = new frmInstallPackage(MainController.Get().InitialWKP))
                    pi.ShowDialog();
            }
            else if (!string.IsNullOrEmpty(MainController.Get().InitialFilePath))
            {
                OpenCr2wFile(MainController.Get().InitialFilePath);
            }

            // hack to set toolbar to visible
             toolbarToolStrip.Visible = true;
             statusToolStrip.Visible = true;
        }

        private void frmMain_MdiChildActivate(object sender, EventArgs e)
        {
            if (sender is IWolvenkitView)
            {
                doc_Activated(sender, e);
            }
        }

        private void dockPanel_ActiveDocumentChanged(object sender, EventArgs e)
        {
            if (dockPanel.ActiveDocument is IWolvenkitView)
            {
                doc_Activated(dockPanel.ActiveDocument, e);
            }
        }

        private void menuStrip1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void menuStrip1_MouseDown_1(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

#region Discord
        private void richpresenceworker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            string project = "non";

            Discord.EventHandlers handlers = new Discord.EventHandlers();
            Discord.Initialize("482179494862651402", handlers);
            while (!richpresenceworker.CancellationPending)
            {
                Thread.Sleep(1000);
                if (MainController.Get().ActiveMod != null)
                {
                    if (project != MainController.Get().ActiveMod.Name.ToString())
                    {
                        project = MainController.Get().ActiveMod.Name.ToString();
                        Discord.RichPresence rp = new Discord.RichPresence();
                        rp.state = "";
                        rp.details = "Developing " + project;
                        rp.largeImageKey = "logo_wkit";
                        Discord.UpdatePresence(rp);
                    }
                }
            }
        }

        private void richpresenceworker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {

        }

        private void richpresenceworker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {

        }
#endregion

        private void doc_Activated(object sender, EventArgs e)
        {
            if (sender is IWolvenkitView doc)
            {
                vm.ActiveDocument = doc.GetViewModel();
            }
        }

        private void doc_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (sender is IWolvenkitView doc)
            {
                lastClosedTab.Enqueue(doc.FileName);
                vm.RemoveOpenDocument(doc.FileName);
            }
        }


#endregion

#region MenuStrip
        private void iconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //new frmLoading().Show();
        }

#region Context menus
        private void modToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            packAndInstallModToolStripMenuItem.Enabled = ActiveMod != null;
            createPackedInstallerToolStripMenuItem.Enabled = ActiveMod != null;
            reloadProjectToolStripMenuItem.Enabled = ActiveMod != null;
            settingsToolStripMenuItem.Enabled = ActiveMod != null;
        }

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            exportToolStripMenuItem.Enabled = ActiveMod != null;
            importToolStripMenuItem.Enabled = ActiveMod != null;

            newFileToolStripMenuItem.Enabled = ActiveMod != null;
            addFileFromBundleToolStripMenuItem.Enabled = ActiveMod != null;
            addFileFromOtherModToolStripMenuItem.Enabled = ActiveMod != null;
            addFileToolStripMenuItem.Enabled = ActiveMod != null;

            saveToolStripMenuItem.Enabled = vm.ActiveDocument != null;
            saveAllToolStripMenuItem.Enabled = vm.GetOpenDocuments().Count > 0;

            RepopulateRecentFiles();
        }

        private void RepopulateRecentFiles(string file = "")
        {
#region Load recent files into toolstrip

            // Update the recent files.
            recentFilesToolStripMenuItem.DropDownItems.Clear();
            var files = new List<string>();
            if (File.Exists("recent_files.xml"))
            {
                var doc = XDocument.Load("recent_files.xml");
                int maxRecentFiles = 10;

                if (!string.IsNullOrEmpty(file))
                    files.Add(file);
                foreach (var f in doc.Descendants("recentfile"))
                {
                    maxRecentFiles--;
                    if (maxRecentFiles <= 0) break;
                    if (File.Exists(f.Value))
                    {
                        files.Add(f.Value);
                        recentFilesToolStripMenuItem.DropDownItems.Add(new ToolStripMenuItem(f.Value, null, RecentFile_click));
                    }
                }
                recentFilesToolStripMenuItem.Enabled = files.Any();
            }
            else
            {
                recentFilesToolStripMenuItem.Enabled = false;
            }
            new XDocument(new XElement("RecentFiles", files.Distinct().Select(x => new XElement("recentfile", x)))).Save("recent_files.xml");
#endregion
        }


        private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            //verifyFileToolStripMenuItem.Enabled = ActiveMod != null;
            //renderW2meshToolStripMenuItem.Enabled = ActiveMod != null;
        }

        private void toolsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            packageInstallerToolStripMenuItem.Enabled = ActiveMod != null;
            stringsEncoderGUIToolStripMenuItem.Enabled = ActiveMod != null;
            menuCreatorToolStripMenuItem.Enabled = ActiveMod != null;
            bulkEditorToolStripMenuItem.Enabled = ActiveMod != null;
            cR2WToTextToolStripMenuItem.Enabled = ActiveMod != null;
            experimentalToolStripMenuItem.Enabled = ActiveMod != null;
            
            //launchModkitToolStripMenuItem.Enabled = ActiveMod != null;
        }

        private void viewToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            radishUtilitytoolStripMenuItem.Enabled = ActiveMod != null;
            //importUtilityToolStripMenuItem.Enabled = ActiveMod != null;
        }

        private void gameToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            GameDebuggerToolStripMenuItem.Enabled = ActiveMod != null;
            saveExplorerToolStripMenuItem.Enabled = ActiveMod != null;
        }

#endregion

#region File
        private void tbtNewMod_Click(object sender, EventArgs e) => CreateNewMod();

        private void tbtOpenMod_Click(object sender, EventArgs e) => OpenMod();

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog() { Title = "Open CR2W File" };
            dlg.InitialDirectory = MainController.Get().Configuration.InitialFileDirectory;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                OpenCr2wFile(dlg.FileName);
            }
        }

        private void OpenCr2wFile(string path)
        {
            MainController.Get().Configuration.InitialFileDirectory = Path.GetDirectoryName(path);
            LoadDocument(path);
        }

        private void RecentFile_click(object sender, EventArgs e) => OpenMod(sender.ToString());

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var sf = new SaveFileDialog())
            {
                sf.Title = "Please select a location to save the json dump of the cr2w file";
                sf.Filter = "JSON Files | *.json";
                if (sf.ShowDialog() == DialogResult.OK)
                {
                    throw new NotImplementedException("TODO");
                }
            }
        }

        private void extractCollisioncacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var of = new OpenFileDialog())
            {
                of.Title = "Please select the collision.cache file to extract";
                of.Filter = "Collision caches | collision.cache";
                if (of.ShowDialog() == DialogResult.OK)
                {
                    using (var sf = new FolderBrowserDialog())
                    {
                        sf.Description = "Please specify a location to save the extracted files";
                        if (sf.ShowDialog() == DialogResult.OK)
                        {
                            var ccf = new Cache.CollisionCache(of.FileName);
                            var outdir = sf.SelectedPath.EndsWith("\\") ? sf.SelectedPath : sf.SelectedPath + "\\";
                            foreach (var f in ccf.Files)
                            {
                                string extractedfilename = Path.ChangeExtension(Path.Combine(outdir, f.Name), "apb");
                                f.Extract(new BundleFileExtractArgs(extractedfilename, MainController.Get().Configuration.UncookExtension));
                                Logger.LogString($"Extracted {extractedfilename}.\n");
                            }
                        }
                    }
                }
            }
        }

        private void w2rigjsonToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if !USE_RENDER
            //MessageBox.Show(@"Select w2rig JSON.", "Information about importing rigs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            using (var of = new OpenFileDialog())
            {
                of.Title = "Please select your w2rig.json file";
                of.Filter = "w2rig JSON files | *w2rig.json";
                if (of.ShowDialog() == DialogResult.OK)
                {
                    using (var sf = new SaveFileDialog())
                    {
                        sf.Filter = "Witcher 3 rig file | *.w2rig";
                        sf.Title = "Please specify a location to save the imported file";
                        sf.InitialDirectory = MainController.Get().Configuration.InitialFileDirectory;
                        sf.FileName = of.FileName;
                        if (sf.ShowDialog() == DialogResult.OK)
                        {
                            MainController.Get().ProjectStatus = EProjectStatus.Busy;

                            try
                            {
                                ConvertRig rig = new ConvertRig();
                                rig.Load(of.FileName);
                                rig.SaveToFile(sf.FileName);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogString(ex.ToString() + "\n", Common.Services.Logtype.Error);
                            }

                            MainController.Get().ProjectStatus = EProjectStatus.Ready;
                        }
                    }
                }
            }
#endif
        }

        private void w2animsjsonToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if !USE_RENDER
            //MessageBox.Show(@"Select w2anims JSON.", "Information about importing rigs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            using (var of = new OpenFileDialog())
            {
                of.Title = "Please select your w2anims.json file";
                of.Filter = "anims JSON files | *w2anims.json";
                if (of.ShowDialog() == DialogResult.OK)
                {
                    using (var sf = new SaveFileDialog())
                    {
                        sf.Filter = "Witcher 3 w2anims file | *.w2anims";
                        sf.Title = "Please specify a location to save the imported file";
                        sf.InitialDirectory = MainController.Get().Configuration.InitialFileDirectory;
                        sf.FileName = of.FileName;
                        if (sf.ShowDialog() == DialogResult.OK)
                        {
                            MainController.Get().ProjectStatus = EProjectStatus.Busy;

                            try
                            {
                                ConvertAnimation anim = new ConvertAnimation();
                                anim.Load(new List<string>() { of.FileName }, sf.FileName);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogString(ex.ToString() + "\n", Common.Services.Logtype.Error);
                            }

                            MainController.Get().ProjectStatus = EProjectStatus.Ready;
                        }
                    }
                }
            }
#endif
        }

        private void DLCScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveMod == null)
                return;

            var scriptsdirectory = (ActiveMod.DlcDirectory + "\\scripts\\local");
            if (!Directory.Exists(scriptsdirectory))
            {
                Directory.CreateDirectory(scriptsdirectory);
            }
            var fullPath = scriptsdirectory + "\\" + "blank_script.ws";
            var count = 1;
            var fileNameOnly = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);
            var path = Path.GetDirectoryName(fullPath);
            var newFullPath = fullPath;
            while (File.Exists(newFullPath))
            {
                string tempFileName = $"{fileNameOnly}({count++})";
                if (path != null) newFullPath = Path.Combine(path, tempFileName + extension);
            }
            File.WriteAllLines(newFullPath, new[] { @"/*", $"Wolven kit - {Version}", DateTime.Now.ToString("d"), @"*/" });
        }

        private void ModscriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveMod == null)
                return;

            var scriptsdirectory = (ActiveMod.ModDirectory + "\\scripts\\local");
            if (!Directory.Exists(scriptsdirectory))
            {
                Directory.CreateDirectory(scriptsdirectory);
            }
            var fullPath = scriptsdirectory + "\\" + "blank_script.ws";
            var count = 1;
            var fileNameOnly = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);
            var path = Path.GetDirectoryName(fullPath);
            var newFullPath = fullPath;
            while (File.Exists(newFullPath))
            {
                string tempFileName = $"{fileNameOnly}({count++})";
                if (path != null) newFullPath = Path.Combine(path, tempFileName + extension);
            }
            File.WriteAllLines(newFullPath, new[] { @"/*", $"Wolven kit - {Version}", DateTime.Now.ToString("d"), @"*/" });
        }

        private void ModWwiseNew_Click(object sender, EventArgs e)
        {
            using (var of = new OpenFileDialog())
            {
                of.Multiselect = true;
                of.Filter = "Wwise files | *.wem;*.bnk";
                of.Title = "Please select the wwise bank and sound files for importing them into your mod";
                if (of.ShowDialog() == DialogResult.OK)
                {
                    foreach (var f in of.FileNames)
                    {
                        var newfilepath = Path.Combine(ActiveMod.ModDirectory, EBundleType.SoundCache.ToString(), Path.GetFileName(f));
                        //Create the directory because it will crash if it doesn't exist.
                        Directory.CreateDirectory(Path.GetDirectoryName(newfilepath));
                        File.Copy(f, newfilepath, true);
                    }
                }
            }
        }

        private void DLCWwise_Click(object sender, EventArgs e)
        {
            using (var of = new OpenFileDialog())
            {
                of.Multiselect = true;
                of.Filter = "Wwise files | *.wem;*.bnk";
                of.Title = "Please select the wwise bank and sound files for importing them into your DLC";
                if (of.ShowDialog() == DialogResult.OK)
                {
                    foreach (var f in of.FileNames)
                    {
                        var newfilepath = Path.Combine(ActiveMod.DlcDirectory, EBundleType.SoundCache.ToString(), "dlc", ActiveMod.Name, Path.GetFileName(f));
                        //Create the directory because it will crash if it doesn't exist.
                        Directory.CreateDirectory(Path.GetDirectoryName(newfilepath));
                        File.Copy(f, newfilepath, true);
                    }
                }
            }
        }

        private void OpenDepotAssetBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenAssetBrowser(false);
        }

        private void OpenModAssetBrowserToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            OpenAssetBrowser(true);
        }

        private void addFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog() { Title = "Add File to Project" };
            dlg.InitialDirectory = MainController.Get().Configuration.InitialFileDirectory;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                MainController.Get().Configuration.InitialFileDirectory = Path.GetDirectoryName(dlg.FileName);
                try
                {
                    FileInfo fi = new FileInfo(dlg.FileName);
                    var newfilepath = Path.Combine(ActiveMod.RawDirectory, fi.Name);

                    var explorer = (frmModExplorer)MockKernel.Get().GetModExplorer();
                    if (explorer?.GetSelectedObject() != null)
                    {
                        var fsi = explorer.GetSelectedObject();
                        newfilepath = Path.Combine(fsi.IsDirectory() 
                            ? fsi.FullName 
                            : fsi.GetParent().FullName, fi.Name);
                    }

                    
                    if (File.Exists(newfilepath))
                        newfilepath = $"{newfilepath.TrimEnd(fi.Extension.ToCharArray())} - copy{fi.Extension}";

                    fi.CopyToAndCreate(newfilepath, false);
                }
                catch (Exception ex)
                {
                }


            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            vm.SaveActiveFile();
        }

        private void SaveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            vm.SaveAllFiles();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
#endregion

#region Project
        private void createPackedInstallerToolStripMenuItem_Click(object sender, EventArgs e) => CreateInstaller();

        private void ReloadProjectToolStripMenuItem_Click(object sender, EventArgs e) => OpenMod(MainController.Get().ActiveMod?.FileName);

        private void modSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveMod == null) return;
            //Close all docs so they won't cause problems
            if (!vm.CloseAllDocuments()) return;

            //With this cloned it won't get modified when we change it in dlg
            var oldmod = (W3Mod)ActiveMod.Clone();
            using (var dlg = new frmModSettings())
            {
                dlg.Mod = ActiveMod;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (oldmod.Name != dlg.Mod.Name)
                    {
                        try
                        {
                            PauseMonitoring();
                            
                            //Move the files directory
                            Directory.Move(oldmod.ProjectDirectory, Path.Combine(Path.GetDirectoryName(oldmod.ProjectDirectory), dlg.Mod.Name));
                            //Delete the old directory
                            if (Directory.Exists(oldmod.ProjectDirectory))
                                Commonfunctions.DeleteFilesAndFoldersRecursively(oldmod.ProjectDirectory);
                            //Delete the old mod project file
                            if (File.Exists(oldmod.FileName))
                                File.Delete(oldmod.FileName);
                        }
                        catch (System.IO.IOException ex)
                        {
                            MessageBox.Show("Please check that you don't have Windows Explorer open at the old mod's path and that no folder/mod with that name already exists.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                            return;
                        }
                    }
                    //Save the new settings and update the title
                    UpdateTitle();
                    vm.SaveMod();
                    OpenMod(MainController.Get().ActiveMod?.FileName);

                    CommonUIFunctions.SendNotification("Succesfully updated mod settings!");
                    ResumeMonitoring();
                }
            }
        }

#endregion

#region Tools
        private void packageInstallerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var of = new OpenFileDialog())
            {
                of.Filter = "WolvenKit Package | *.wkp";
                if (of.ShowDialog() == DialogResult.OK)
                {
                    using (var pi = new frmInstallPackage(of.FileName))
                        pi.ShowDialog();
                }
                else
                    CommonUIFunctions.SendNotification("Invalid file!");
            }
        }

        private void StringsGUIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UIController.Get().WindowFactory.ShowStringsGUIModal();
        }

        private void menuCreatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var fmc = new frmMenuCreator())
            {
                fmc.ShowDialog();
            }
        }

        private void renderW2meshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bool.Parse(renderW2meshToolStripMenuItem.Tag.ToString()))
            {
                renderW2meshToolStripMenuItem.Tag = false;
                renderW2meshToolStripMenuItem.Image = Properties.Resources.ui_check_box_uncheck;
            }
            else
            {
                renderW2meshToolStripMenuItem.Tag = true;
                renderW2meshToolStripMenuItem.Image = Properties.Resources.ui_check_box;
            }
        }

        private void cR2WToTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fctt = new frmCR2WtoText();
            fctt.ShowDialog();
        }

        private void verifyFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var of = new OpenFileDialog())
            {
                of.Multiselect = true;
                of.Filter = "Cr2w files | *.*";
                of.Title = "Please select the Cr2w files for verifying.";
                if (of.ShowDialog() == DialogResult.OK)
                {
                    foreach (var f in of.FileNames)
                    {
                        CR2WVerify.VerifyFile(f);
                    }
                }
            }
        }

        private void terrainViewerToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
#if !USE_RENDER
            Render.frmTerrain ter = new Render.frmTerrain();
            ter.Show(this.dockPanel, DockState.Document);
#endif
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var settings = new frmSettings();
            //settings.RequestApplyTheme += GlobalApplyTheme;
            settings.ShowDialog();
        }

        private void witcher3ModkitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MockKernel.Get().ShowModKit();
        }

        private void bulkEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var be = new frmBulkEditor();
            be.ShowDialog();
        }
#endregion

#region View
        private void modExplorerToolStripMenuItem_Click(object sender, EventArgs e) => MockKernel.Get().ShowModExplorer();

        private void OutputToolStripMenuItem_Click(object sender, EventArgs e) => MockKernel.Get().ShowOutput();

        private void consoleToolStripMenuItem_Click(object sender, EventArgs e) => MockKernel.Get().ShowConsole();

        private void importUtilityToolStripMenuItem_Click(object sender, EventArgs e) => MockKernel.Get().ShowImportUtility();

        private void RadishUtilitytoolStripMenuItem_Click(object sender, EventArgs e) => MockKernel.Get().ShowRadishUtility();

        private void scriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }
#endregion

#region Game
        private void unbundleGameToolStripMenuItem_Click(object sender, EventArgs e) => SetupUnbundling();

        private void SetupUnbundling()
        {
            // Backgroundworker
            if (MainBackgroundWorker.IsBusy)
            {
                return;
            }

            // query free disk space
            DriveInfo drive = new DriveInfo(new DirectoryInfo(MainController.Get().Configuration.DepotPath).Root.FullName);
            if (!drive.IsReady)
                return;

            // check path length
            //if (MainController.Get().Configuration.DepotPath.Length > 38)
            if (MainController.Get().Configuration.DepotPath.Length > 255)
            {
                MainController.LogString("Wcc probably does not support path lengths > 255. " +
                                         "Please move your wcc_lite Modkit directory closer to root, e.g. C:\\Modkit\\.", Common.Services.Logtype.Error);
                return;
            }

            // check free space
            var freespace = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            var totalsize = drive.TotalSize / (1024 * 1024 * 1024);
            bool overwrite;
            switch (MessageBox.Show(
                        $"Unbundling the game will take about {30}GB of space, available space: {freespace}GB on drive {drive.Name}.\n" +
                        $"Depot directory: {MainController.Get().Configuration.DepotPath}\n\n" +
                        $"Would you like to continue?",
                        "Unbundle Game Files",
                        System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question))
            {
                default:
                    return;
                case DialogResult.Yes:
                    {
                        switch (MessageBox.Show(
                            $"Would you like to overwrite existing files in the depot directory?",
                            "Overwrite Files",
                            System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question))
                        {
                            default:
                                return;
                            case DialogResult.Yes:
                            {
                                overwrite = true;
                                break;
                            }
                            case DialogResult.No:
                            {
                                overwrite = false;
                                break;
                            }
                        }


                        break;
                    }
                case DialogResult.No:
                    {
                        return;
                    }
            }


            // Backgroundworker
            if (!MainBackgroundWorker.IsBusy)
            {
                // progress bar
                ProgressForm = new frmProgress()
                {
                    Text = "Unbundling Game Assets",
                    StartPosition = FormStartPosition.CenterParent,
                };

                // background worker action
                workerAction = UnbundleGame;
                MainBackgroundWorker.RunWorkerAsync(new UnbundleGameArgs(overwrite));

                // cancellation dialog
                DialogResult dr = ProgressForm.ShowDialog(this);
                switch (dr)
                {
                    case DialogResult.Cancel:
                        {
                            MainBackgroundWorker.CancelAsync();
                            ProgressForm.Cancel = true;
                            break;
                        }
                    case DialogResult.None:
                    case DialogResult.OK:
                    case DialogResult.Abort:
                    case DialogResult.Retry:
                    case DialogResult.Ignore:
                    case DialogResult.Yes:
                    case DialogResult.No:
                    default:
                        break;
                }


                this.BringToFront();
            }
            else
                Logger.LogString("The background worker is currently busy.\r\n", Common.Services.Logtype.Error);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private object UnbundleGame(object sender, DoWorkEventArgs e)
        {
            object arg = e.Argument;
            if (!(arg is UnbundleGameArgs))
                throw new NotImplementedException();
            var Details = (UnbundleGameArgs)arg;

            BackgroundWorker bwAsync = sender as BackgroundWorker;

            //Load MemoryMapped Bundles
            var memorymappedbundles = new Dictionary<string, MemoryMappedFile>();
            var bm = new BundleManager();
            bm.LoadAll(Path.GetDirectoryName(MainController.Get().Configuration.ExecutablePath));

 
            foreach (var b in bm.Bundles.Values)
            {
                var hash = b.ArchiveAbsolutePath.GetHashMD5();
                memorymappedbundles.Add(hash, MemoryMappedFile.CreateFromFile(b.ArchiveAbsolutePath, FileMode.Open, hash, 0, MemoryMappedFileAccess.Read));
            }

            var files = bm.FileList
                    .GroupBy(p => p.Name)
                    .Select(g => g.Last())
                    .ToList();

            var orderedList = files.OrderBy(_ => _.Name.Length).ToList();

            int finishedcount = 0;
            var count = files.Count;
            Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = (int)(Environment.ProcessorCount * 0.8) + 1 }, i =>
            {
                if (bwAsync.CancellationPending || ProgressForm.Cancel)
                {
                    MainController.LogString("Background worker cancelled.\r\n", Common.Services.Logtype.Error);
                    e.Cancel = true;
                    return;
                }

                IWitcherFile f = orderedList[i];
                if (f is BundleItem bi)
                {
                    var newpath = Path.Combine(MainController.Get().Configuration.DepotPath, bi.Name);


                    // overwrite existing files
                    if (File.Exists(newpath))
                    {
                        if (Details.Overwrite)
                        {
                            File.Delete(newpath);

                            var fi = new FileInfo(newpath);
                            var newdir = Path.GetDirectoryName(newpath);
                            Directory.CreateDirectory(newdir);

                            using (var ms = new MemoryStream())
                            using (FileStream file = new FileStream(newpath, FileMode.Create, System.IO.FileAccess.Write))
                            {
                                try
                                {
                                    bi.ExtractExistingMMF(ms);
                                }
                                catch (Exception ex)
                                {
                                    foreach (var val in memorymappedbundles.Values)
                                        MainController.LogString(val.GetHashCode().ToString());
                                    MainController.LogString(ex.Message);
                                }

                                ms.Seek(0, SeekOrigin.Begin);

                                ms.CopyTo(file);
                                Interlocked.Increment(ref finishedcount);
                            }
                        }
                        else
                        {
                            MainController.LogString("tabernak");
                            // do nothing
                        }
                    }
                    else
                    {
                        var fi = new FileInfo(newpath);
                        var newdir = Path.GetDirectoryName(newpath);
                        Directory.CreateDirectory(newdir);

                        using (var ms = new MemoryStream())
                        using (FileStream file = new FileStream(fi.FullName, FileMode.Create, System.IO.FileAccess.Write))
                        {
                            bi.ExtractExistingMMF(ms);
                            ms.Seek(0, SeekOrigin.Begin);

                            ms.CopyTo(file);
                        }
                        Interlocked.Increment(ref finishedcount);
                    }



                    int percentprogress = (int)((float)finishedcount / (float)count * 100.0);
                    MainBackgroundWorker.ReportProgress(percentprogress, bi.Name);
                }
            });
            
            foreach(var val in memorymappedbundles.Values)
            {
                val.Dispose();
            }

            MainController.LogString($"Sucessfully unbundled {finishedcount} files to {MainController.Get().Configuration.DepotPath}", Common.Services.Logtype.Success);
            return true;

        }

        private async void uncookGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // query free disk space
            DriveInfo drive = new DriveInfo(new DirectoryInfo(MainController.Get().Configuration.DepotPath).Root.FullName);
            if (!drive.IsReady)
                return;
            var freespace = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            var totalsize = drive.TotalSize / (1024 * 1024 * 1024);
            switch (MessageBox.Show(
                        $"Uncooking the game takes a very long time and is usually not needed, consider unbundling the game instead.\n\n" +
                        $"Uncooking the game will take about {60}GB of space, available space: {freespace}GB on drive {drive.Name}.\n" +
                        $"Depot directory: {MainController.Get().Configuration.DepotPath}\n\n" +
                        $"Click Yes to unbundle or click No to continue uncooking.",
                        "Uncook Game Files",
                        System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Question))
            {
                default:
                    return;
                case DialogResult.Yes:
                    {
                        SetupUnbundling();
                        return;
                    }
                case DialogResult.No:
                    {
                        // Not implemented
                        await SetupUncooking();
                        return;
                    }
                case DialogResult.Cancel:
                    {
                        return;
                    }
            }
        }

        private async Task SetupUncooking()
        {
            var content = Path.Combine(new FileInfo(MainController.Get().Configuration.ExecutablePath).Directory.Parent.Parent.FullName, "content");
            await UncookInternal(content).ContinueWith(antecedent =>
            {

            });
#if NGE_VERSION
            var dlc = Path.Combine(new FileInfo(MainController.Get().Configuration.ExecutablePath).Directory.Parent.Parent.FullName, "dlc");
            string[] vanillaDLClist = new string[] { "dlc1", "dlc2", "dlc3", "dlc4", "dlc5", "dlc6", "dlc7", "dlc8", "dlc9", "dlc10", "dlc11", "dlc12", "dlc13", "dlc14", "dlc15", "dlc16", "dlc17", "dlc18", "dlc20", "bob", "ep1" };
#else
            var dlc = Path.Combine(new FileInfo(MainController.Get().Configuration.ExecutablePath).Directory.Parent.Parent.FullName, "DLC");
            string[] vanillaDLClist = new string[] { "DLC1", "DLC2", "DLC3", "DLC4", "DLC5", "DLC6", "DLC7", "DLC8", "DLC9", "DLC10", "DLC11", "DLC12", "DLC13", "DLC14", "DLC15", "DLC16", "bob", "ep1" };
#endif
            foreach (var item in vanillaDLClist)
            {
                var dlcdir = Path.Combine(dlc, item);
                await UncookInternal(dlcdir).ContinueWith(antecedent =>
                {

                });
            }



            async Task UncookInternal(string inputpath)
            {
                var depot = MainController.Get().Configuration.DepotPath;

                var cmd = new Wcc_lite.uncook()
                {
                    InputDirectory = inputpath,
                    OutputDirectory = depot,
                    Imgfmt = imageformat.tga,
                    Skiperrors = true,
                    Dumpswf = true
                };
                await Task.Run(() => MainController.Get().WccHelper.RunCommand(cmd));
            }


        }

        private void openUncookedFolderToolStripMenuItem_Click(object sender, EventArgs e) =>
            Commonfunctions.ShowFolderInExplorer(MainController.Get().Configuration.DepotPath);

        private void saveExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var sef = new frmSaveEditor())
                sef.ShowDialog();
        }

        private void GameDebuggerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var gdb = new frmDebug();
            Rectangle floatWindowBounds = new Rectangle() { Width = 827, Height = 564 };
            gdb.Show(dockPanel, floatWindowBounds);
        }

#endregion

#region Help
        private void donateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Thank you! Every last bit helps and everything donated is distributed between the core developers evenly.", "Thank you", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            System.Diagnostics.Process.Start("https://www.patreon.com/bePatron?u=5458437");
        }

        private void creditsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var cf = new frmAbout())
                cf.ShowDialog();
        }

        private void joinOurDiscordToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (MessageBox.Show(@"Are you sure you would like to join the modding discord?", @"Confirmation", System.Windows.Forms.MessageBoxButtons.YesNo) == DialogResult.Yes)
                Process.Start("https://discord.gg/KnPMmBz");
        }

        private void WitcherScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://witcherscript.readthedocs.io");
        }

        private void witcherIIIModdingToolLicenseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var wcclicense = new frmWCCLicense();
            wcclicense.Show();
        }

        private void RecordStepsToReproduceBugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(@"This will launch an app that will help you record the steps needed to reproduce the bug/problem.
After its done it saves a zip file.
Please send that to hambalko.bence@gmail.com with a short description about the problem.
Would you like to open the problem steps recorder?", "Bug reporting", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Process.Start("psr.exe");
            }
        }

        private void ReportABugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("When reporting a bug please create a reproducion file at Help->Record steps to reproduce.",
                "Bug reporting",
                System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            Process.Start($"mailto:{"hambalko.bence@gmail.com"}?Subject={"WolvenKit bug report"}&Body={"Short description of bug:"}");
        }
#endregion
#endregion

#region ToolBar
        private void newModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewMod();
        }

        private void openModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenMod();
        }

        private void tbtOpen_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Open CR2W File", InitialDirectory = MainController.Get().Configuration.InitialFileDirectory
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                MainController.Get().Configuration.InitialFileDirectory = Path.GetDirectoryName(dlg.FileName);
                LoadDocument(dlg.FileName);
            }
        }

        private void tbtSave_Click(object sender, EventArgs e) => vm.SaveActiveFile();

        private void tbtSaveAll_Click(object sender, EventArgs e)
        {
            if (ActiveMod == null)
            {
                return;
            }
            MainController.Get().ProjectStatus = EProjectStatus.Busy;
            vm.SaveAllFiles();
            MainController.Get().ProjectStatus = EProjectStatus.Ready;
            Logger.LogString("Saved!\n", Common.Services.Logtype.Success);
        }



        private void toolStripBtnPack_Click(object sender, EventArgs e) => vm.PackProject();

        private void toolStripButtonRadishUtil_Click(object sender, EventArgs e) => MockKernel.Get().ShowRadishUtility();

        private void toolStripButtonImportUtil_Click(object sender, EventArgs e) => MockKernel.Get().ShowImportUtility();

        private void launchWithCostumParametersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var getparams = new Input("Please give the commands to launch the game with!");
            if (getparams.ShowDialog() == DialogResult.OK)
            {
                MainViewModel.ExecuteGame(getparams.Resulttext);
            }
        }

        private void LaunchGameForDebuggingToolStripMenuItem_Click(object sender, EventArgs e) => MainViewModel.ExecuteGame();

        private void packProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pack = vm.PackAndInstallMod();
            while (!pack.IsCompleted)
                Application.DoEvents();
        }

        private void packProjectAndLaunchGameCustomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pack = vm.PackAndInstallMod();
            while (!pack.IsCompleted)
                Application.DoEvents();

            if (!pack.Result)
                return;

            var getparams = new Input("Please give the commands to launch the game with!");
            if (getparams.ShowDialog() == DialogResult.OK)
            {
                MainViewModel.ExecuteGame(getparams.Resulttext);
            }
        }

        private void PackProjectAndRunGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pack = vm.PackAndInstallMod();
            while (!pack.IsCompleted)
                Application.DoEvents();

            if (!pack.Result)
                return;
            MainViewModel.ExecuteGame();
        }

        private void ModchunkToolStripMenuItem_Click(object sender, EventArgs e) => vm.CreateCr2wFileCommand.SafeExecute(false);

        private void DLCChunkToolStripMenuItem_Click(object sender, EventArgs e) => vm.CreateCr2wFileCommand.SafeExecute(true);

        private void sceneViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if !USE_RENDER
            var dlg = new CommonOpenFileDialog {Title = "Select file", Multiselect = false};
            dlg.Filters.Add(new CommonFileDialogFilter("Files", ".w2w,.w2l"));
            dlg.InitialDirectory = MainController.Get().Configuration.InitialFileDirectory;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                // parse the w2w and provide information to the scene
                var sceneView = new Render.frmLevelScene(dlg.FileName, MainController.Get().Configuration.DepotPath, MainController.Get().TextureManager);
                sceneView.Show(this.dockPanel, DockState.Document);
            }
#endif
        }

        private void toolStripDropDownButtonGit_Paint(object sender, PaintEventArgs e)
        {
            if (toolStripDropDownButtonGit.Pressed)
            {
                //e.Graphics.FillRectangle(Brushes.Transparent, e.ClipRectangle);
            }
        }

        private async void backupModProjectToolStripMenuItem_Click(object sender, EventArgs e) => vm.BackupProjectCommand.SafeExecute();

        private void openBackupFolderToolStripMenuItem_Click(object sender, EventArgs e) => Commonfunctions.ShowFolderInExplorer(ActiveMod?.BackupDirectory);

        private void commandPromptHereToolStripMenuItem_Click(object sender, EventArgs e) => Commonfunctions.OpenConsoleAtPath(ActiveMod.ProjectDirectory);

        private void dDStoTextureCacheToolStripMenuItem_Click(object sender, EventArgs e) => vm.DdsToCacheCommand.SafeExecute();

        private void resetDocumentLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (vm.GetOpenDocuments().Any())
            {
                MainController.LogString($"Please close all open documents.", Common.Services.Logtype.Error);
                return;
            }

            try
            {
                var doclayoutconfig = Path.Combine(Path.GetDirectoryName(Configuration.ConfigurationPath),
                    "cr2wdocument_layout.xml");
                if (File.Exists(doclayoutconfig))
                    File.Delete(doclayoutconfig);
                MainController.LogString($"Reset document layout.", Common.Services.Logtype.Success);
            }
            catch (Exception exception)
            {

            }
        }

        private void openModFolderToolStripMenuItem_Click(object sender, EventArgs e) => Commonfunctions.ShowFolderInExplorer(MainController.Get().Configuration.GameModDir);
        
        private void openDlcFolderToolStripMenuItem_Click(object sender, EventArgs e) => Commonfunctions.ShowFolderInExplorer(MainController.Get().Configuration.GameDlcDir);

#endregion
    }
}
