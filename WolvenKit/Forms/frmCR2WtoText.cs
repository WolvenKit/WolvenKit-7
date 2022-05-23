using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WolvenKit.App;
using WolvenKit.Common.Model;
using WolvenKit.Common.Services;
using WolvenKit.CR2W;
using MessageBoxButtons = System.Windows.Forms.MessageBoxButtons;
using MessageBoxIcon = System.Windows.Forms.MessageBoxIcon;

namespace WolvenKit.Forms
{
    public partial class frmCR2WtoText : Form
    {
        private readonly string[] extExclude = { ".txt", ".json", ".csv", ".xml", ".jpg", ".png", ".buffer", ".navgraph", ".navtile",
                                                 ".usm", ".wem", ".dds", ".bnk", ".xbm", ".bundle", ".w3strings", ".store", ".navconfig",
                                                 ".srt", ".naviobstacles", ".navmesh", ".sav", ".subs", ".yml" };
        private StatusController statusController;
        private readonly List<string> Files = new List<string>();

        private CancellationTokenSource Cancel;

        private bool _outputSingleFile = false;
        private bool OutputSingleFile
        {
            get => _outputSingleFile;
            set
            {
                _outputSingleFile = value;
                txtOutputDestination.Clear();
            }
        }

        private bool _running = false;
        private bool _stopping = false;

        public frmCR2WtoText()
        {
            InitializeComponent();
            SetDefaults();
            InitDataGrid();
            InitStatusController();
            foreach (var ext in extExclude)
            {   // Add the list of excluded file extensions to info box so user knows what files will never be opened.
                rtfDescription.AppendText(ext + " ");
            }

            this.Icon = UIController.GetThemedWkitIcon();

        }
        private void InitDataGrid()
        {
            object[] row = {0, 0, 0, 0, 0, 0};
            if (dataStatus.Rows.Count == 0)
                dataStatus.Rows.Add(row);
            else
                dataStatus.Rows[0].SetValues(row);
        }
        private void InitStatusController()
        {
            statusController = new StatusController();
            statusController.OnTotalFilesUpdated += (x) => { UpdateStatusCell(0, x); };
            statusController.OnNonCR2WUpdated += (x) => { UpdateStatusCell(1, x); };
            statusController.OnMatchingUpdated += (x) => { UpdateStatusCell(2, x); };
            statusController.OnProcessedUpdated += (x) => { UpdateStatusCell(3, x); };
            statusController.OnProcessedUpdated += UpdateProgressBarStatic;
            statusController.OnSkippedUpdated += (x) => { UpdateStatusCell(4, x); };
            statusController.OnExceptionsUpdated += (x) => { UpdateStatusCell(5, x); };
        }
        private void UpdateStatusCell(int cell, int value)
        {
            dataStatus.Rows[0].Cells[cell].Value = value;
        }
        private void SetDefaults()
        {
            radExistingOverwrite.Checked = true;
            radExistingSkip.Checked = !radExistingOverwrite.Checked;
            radOutputModeSingleFile.Checked = OutputSingleFile;
            radOutputModeSeparateFiles.Checked = !OutputSingleFile;
            chkDumpSDB.Checked = true;
            chkDumpYML.Checked = true;
            chkDumpOnlyEdited.Checked = true;
            chkDumpFCD.Checked = false;
            chkDumpEmbedded.Checked = false;
            numThreads.Value = Environment.ProcessorCount;
            numThreads.Maximum = Environment.ProcessorCount * 2;
        }
        // Delegates for cross-thread updating of progress bar and console textbox.
        private delegate void UpdateProgressBarDelegate(int processed);
        private delegate void LogLineDelegate(string line);
        private void UpdateProgressBarStatic(int processed)
        {
            Invoke(new UpdateProgressBarDelegate(UpdateProgressBar), processed);
        }
        private void UpdateProgressBar(int processed)
        {
            if (processed > prgProgressBar.Value)
                prgProgressBar.PerformStep();
        }
        private void LogLineStatic(string line)
        {
            Invoke(new LogLineDelegate(LogLine), line);
        }
        private void LogLine(string line)
        {
            txtLog.AppendText(DateTime.Now + ": " + line + "\r\n");
        }
        private void btnChoosePath_Click(object sender, EventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Choose path containing unbundled CR2W files."
            };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtPath.Text = dlg.FileName;
            }
        }
        private void btnRun_Click(object sender, EventArgs e)
        {
            if (!_running)
                StartRun();
            else
                StopRun();
        }
        private void StopRun()
        {
            if (!_stopping)
            {
                btnRun.Text = "Stopping...";
                LogLineStatic("Stopping dump once in-progress files complete.");
                Cancel.Cancel();
                _stopping = true;
            }
        }
        private void ControlsEnabledDuringRun(bool b)
        {
            // During run, all controls are disabled, and the run button changes to abort
            pnlControls.Enabled = b;
            btnRun.Text = b ? "Run CR2W Dump" : "Abort";
        }
        private async void StartRun()
        {
            ControlsEnabledDuringRun(false);
            prgProgressBar.Value = 0;
            prgProgressBar.Visible = true;
            LogLine($"Dump starting ({txtPath.Text})");
            _running = true;

            await Task.Run(DoRun);

            _running = false;
            _stopping = false;
            LogLine($"Dump finished ({txtPath.Text})");
            prgProgressBar.Visible = false;
            ControlsEnabledDuringRun(true);
        }
        private async Task DoRun()
        {
            // Clear status prior to new run.
            statusController.Processed = statusController.Skipped = statusController.Exceptions = 0;
            if (Files.Any())
            {
                string sourcePath = txtPath.Text;

                var cr2wOptions = new LoggerCR2WOptions
                {
                    StartingIndentLevel = 1,
                    ListEmbedded = chkDumpEmbedded.Checked,
                    DumpFCD = chkDumpFCD.Checked,
                    DumpSDB = chkDumpSDB.Checked,
                    DumpYML = chkDumpYML.Checked,
                    DumpTXT = true,
                    DumpOnlyEdited = chkDumpOnlyEdited.Checked,
                    LocalizeStrings = chkLocalizedString.Checked
                };

                using (Cancel = new CancellationTokenSource())
                {
                    var loggerOptions = new LoggerWriterData
                    {
                        CancelToken = Cancel.Token,
                        Status = statusController,
                        OutputSingleFile = this.OutputSingleFile,
                        NumThreads = (int) numThreads.Value,
                        SourcePath = sourcePath,
                        OutputLocation = txtOutputDestination.Text,
                        CreateFolders = chkCreateFolders.Checked,
                        PrefixFileName = chkPrefixFileName.Checked,
                        OverwriteFiles = radExistingOverwrite.Checked
                    };

                    LoggerWriter writer;

                    if (OutputSingleFile)
                        writer = new LoggerWriterSingle(Files, loggerOptions, cr2wOptions);
                    else
                        writer = new LoggerWriterSeparate(Files, loggerOptions, cr2wOptions);

                    writer.OnExceptionFile += (fileName, msg) =>
                    {
                        var fileNameNoSource = LoggerWriter.FileNameNoSourcePath(fileName, sourcePath);
                        msg = msg.Replace("\r\n", " ");
                        LogLineStatic($"Exception: {fileNameNoSource} : {msg}");
                    };
                    writer.OnNonCR2WFile += fileName =>
                    {
                        var fileNameNoSource = LoggerWriter.FileNameNoSourcePath(fileName, sourcePath);
                        LogLineStatic($"Non CR2W file: {fileNameNoSource}");
                    };

                    LogLineStatic("Starting counts: " + statusLogMessage(false));

                    await writer.StartDump();

                    LogLineStatic("Completed dump stats: " + statusLogMessage(true));
                }
            }
        }
        private string statusLogMessage(bool after)
        {
            var log = $"Files in source folder: {statusController.TotalFiles}, " +
                            $"Non CR2W: {statusController.NonCR2W}. " +
                            $"To be processed: {statusController.Matching}. ";
            if (after)
            {
                log += $"Processed: {statusController.Processed}, " +
                       $"Skipped: {statusController.Skipped}, " +
                       $"Exceptions in/while reading: {statusController.Exceptions}. " +
                       $"Not processed: {statusController.Matching - statusController.Processed - statusController.Skipped}.";
            }
            return log;
        }
        private void CheckEnableRunButton()
        {
            btnRun.Enabled = txtPath.Text != "" && txtOutputDestination.Text != "" && Files.Any();
        }
        private async Task UpdateSourceFolder(string path)
        {
            Files.Clear();
            statusController.UpdateAll(0,0,0,0,0); // Clear stats
            if (Directory.Exists(path))
            {
                btnRun.Enabled = false;
                pnlControls.Enabled = false;
                LogLine("Reading source folder...");
                await Task.Run(() =>
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                    {
                        if (!extExclude.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                        {
                            Files.Add(file);
                            statusController.Matching++;
                        }
                        else
                            statusController.NonCR2W++;
                    }
                });

                ResetProgressBar(Files.Count());

                LogLine("Finished reading source folder.");
                pnlControls.Enabled = true;
            }
            CheckEnableRunButton();
        }
        private void ResetProgressBar(int filesCount)
        {
            prgProgressBar.Maximum = filesCount;
            prgProgressBar.Value = 0;
            prgProgressBar.Step = 1;
        }
        private void txtPath_TextChanged(object sender, EventArgs e)
        {
            UpdateSourceFolder(txtPath.Text);
        }
        private void txtOutputDestination_TextChanged(object sender, EventArgs e)
        {
            CheckEnableRunButton();
        }
        private void btnPickOutput_Click(object sender, EventArgs e)
        {
            if (OutputSingleFile)
            {
                SaveFileDialog fileDlg = new SaveFileDialog
                {
                    Title = "Enter the filename to output to",
                    Filter = "txt | *.txt"
                };
                if (fileDlg.ShowDialog() == DialogResult.OK)
                    txtOutputDestination.Text = fileDlg.FileName;
            }
            else
            {
                CommonOpenFileDialog folderDlg = new CommonOpenFileDialog
                {
                    Title = "Select folder to write text dumps to",
                    IsFolderPicker = true,
                    EnsurePathExists = false,
                    EnsureFileExists = false,
                    EnsureValidNames = true
                };

                if (folderDlg.ShowDialog() == CommonFileDialogResult.Ok)
                    txtOutputDestination.Text = folderDlg.FileName;
            }
        }
        private void RadioOutputModeChanged(object sender, EventArgs e)
        {
            OutputSingleFile = radOutputModeSingleFile.Checked;
            // In Separate Files mode, enable controls for:
            //   Creating intermediate folders; choosing file overwrite/skip; multi-threaded operation
            chkCreateFolders.Enabled = radOutputModeSeparateFiles.Checked;
            grpExistingFiles.Enabled = radOutputModeSeparateFiles.Checked;
            numThreads.Enabled = radOutputModeSeparateFiles.Checked;
        }
        private void frmCR2WtoText_FormClosing(object sender, FormClosingEventArgs e)
        {   // If user clicks form close while dump is running, abort close and offer chance to cancel dump.
            if (_running)
            {
                if (!_stopping)
                {
                    var msg = "A dump is running. Did you want to cancel this operation?";
                    var caption = "Cancel running dump?";
                    var icon = MessageBoxIcon.Exclamation;
                    var buttons = MessageBoxButtons.YesNo;

                    if (MessageBox.Show(msg, caption, buttons, icon) == DialogResult.Yes)
                        StopRun();
                }
                e.Cancel = true;
            }
            else
                e.Cancel = false;
        }

        private void frmCR2WtoText_Load(object sender, EventArgs e)
        {

        }
    }
    internal class StatusController
    {
        public delegate void StatusDelegate(int value);
        public StatusDelegate OnTotalFilesUpdated;
        public StatusDelegate OnNonCR2WUpdated;
        public StatusDelegate OnMatchingUpdated;
        public StatusDelegate OnProcessedUpdated;
        public StatusDelegate OnSkippedUpdated;
        public StatusDelegate OnExceptionsUpdated;
        public void UpdateAll(int nonCR2W, int matching, int processed, int skipped, int exceptions)
        {
            NonCR2W = nonCR2W;
            Matching = matching;
            Processed = processed;
            Skipped = skipped;
            Exceptions = exceptions;
        }
        public int TotalFiles
        {
            get => Matching + NonCR2W;
        }
        private int _nonCR2W;
        public int NonCR2W
        {
            get => _nonCR2W;
            set
            {
                _nonCR2W = value;
                OnNonCR2WUpdated?.Invoke(_nonCR2W);
                OnTotalFilesUpdated?.Invoke(TotalFiles);
            }
        }
        private int _matching;
        public int Matching
        {
            get => _matching;
            set
            {
                _matching = value;
                OnMatchingUpdated?.Invoke(_matching);
                OnTotalFilesUpdated?.Invoke(TotalFiles);
            }
        }
        private int _processed;
        public int Processed
        {
            get => _processed;
            set
            {
                _processed = value;
                OnProcessedUpdated?.Invoke(_processed);
            }
        }
        private int _skipped;
        public int Skipped
        {
            get => _skipped;
            set
            {
                _skipped = value;
                OnSkippedUpdated?.Invoke(_skipped);
            }
        }
        private int _exceptions;
        public int Exceptions
        {
            get => _exceptions;
            set
            {
                _exceptions = value;
                OnExceptionsUpdated?.Invoke(_exceptions);
            }
        }
    }
    internal class LoggerWriterSeparate : LoggerWriter
    {
        internal LoggerWriterSeparate(List<string> files, LoggerWriterData writerData, LoggerCR2WOptions cr2wOptions)
            : base(files, writerData, cr2wOptions) { }
#pragma warning disable CS1998
        public override async Task StartDump()
#pragma warning restore CS1998
        {
            var parOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = WriterData.NumThreads,
                CancellationToken = WriterData.CancelToken
            };

            if (!Directory.Exists(WriterData.OutputLocation))
                Directory.CreateDirectory(WriterData.OutputLocation);

            try
            {
                Parallel.ForEach(Files, parOptions, async fileName =>
                    {
                        string outputDestination;
                        string outputDestinationTxt;
                        string outputDestinationYml;
                        string fileBaseName = Path.GetFileName(fileName);
                        string fileNameNoSourcePath = FileNameNoSourcePath(fileName, WriterData.SourcePath);

                        if (WriterData.CreateFolders)
                        {   // Recreate the file structure of the source folder in the destination folder.
                            // Strip sourcePath from the start of the filename, strip filename from end, then create the remaining folders.
                            var i = fileNameNoSourcePath.LastIndexOf(fileBaseName, StringComparison.Ordinal);
                            outputDestination = WriterData.OutputLocation + fileNameNoSourcePath.Substring(0, i);

                            Directory.CreateDirectory(outputDestination);
                        }
                        else
                            outputDestination = WriterData.OutputLocation;

                        outputDestinationTxt = outputDestination + "\\" + fileBaseName + ".txt";
                        outputDestinationYml = outputDestination + "\\" + fileBaseName + ".yml";

                        try
                        {
                            bool skip = false;
                            if (File.Exists(outputDestinationTxt))
                                if (WriterData.OverwriteFiles)
                                    File.Delete(outputDestinationTxt);
                                else
                                    skip = true;

                            if (!skip)
                            {
                                using (StreamWriter streamDestination = new StreamWriter(outputDestinationTxt, false))
                                using (StreamWriter streamDestinationYml = new StreamWriter(outputDestinationYml, false))
                                {
                                    await Dump(streamDestination, streamDestinationYml, fileName);
                                    lock (statusLock)
                                        WriterData.Status.Processed++;
                                }
                            }
                            else
                                lock (statusLock)
                                    WriterData.Status.Skipped++;
                        }
                        catch (UnauthorizedAccessException)
                        {   // Couldn't write to destination file for some reason, eg read-only
                            string msg = "Could not write to file - is it readonly? Skipping.";
                            ExceptionOccurred(fileName, msg);
                        }
                    });
            }
            catch (OperationCanceledException)
            {
                //TODO: Do we need to do anything here?
            }
        }
    }
    internal class LoggerWriterSingle : LoggerWriter
    {
        internal LoggerWriterSingle(List<string> files, LoggerWriterData writerData, LoggerCR2WOptions cr2wOptions)
            : base(files, writerData, cr2wOptions) { }
        public override async Task StartDump()
        {
            string outputDestination = WriterData.OutputLocation;
            string outputDestinationYml = WriterData.OutputLocation + ".yml";
            if (File.Exists(outputDestination))
                File.Delete(outputDestination);

            using (StreamWriter streamDestination = new StreamWriter(outputDestination, false))
            using (StreamWriter streamDestinationYml = new StreamWriter(outputDestinationYml, false))
                foreach (var fileName in Files)
                {
                    if (WriterData.CancelToken.IsCancellationRequested)
                        break;
                    await Dump(streamDestination, streamDestinationYml, fileName);
                    WriterData.Status.Processed++;
                }
        }
    }
    internal abstract class LoggerWriter
    {
        protected readonly object statusLock = new object();
        public delegate void OnExceptionFileDelegate(string fileName, string msg);
        public delegate void OnNonCR2WFileDelegate(string msg);
        public OnExceptionFileDelegate OnExceptionFile;
        public OnNonCR2WFileDelegate OnNonCR2WFile;
        protected LoggerCR2WOptions CR2WOptions { get; }
        protected List<string> Files { get; }
        protected LoggerWriterData WriterData { get; }
        internal LoggerWriter(List<string> files, LoggerWriterData writerData, LoggerCR2WOptions cr2wOptions)
        {
            Files = files;
            WriterData = writerData;
            CR2WOptions = cr2wOptions;
        }
        public abstract Task StartDump();

        public static string FileNameNoSourcePath(string fileName, string sourcePath)
        {
            //TODO:UPDATE
            var fileNameNoSourcePath = fileName.Replace(sourcePath, "");
            return fileNameNoSourcePath;
        }
        protected void ExceptionOccurred(string fileName, string msg)
        {
            lock (statusLock)
            {
                WriterData.Status.Skipped++;
                WriterData.Status.Exceptions++;
            }
            OnExceptionFile?.Invoke(fileName, msg);
        }
#pragma warning disable CS1998
        protected async Task Dump(StreamWriter streamDestination, StreamWriter streamDestinationYml, string fileName)
#pragma warning restore CS1998
        {
            LoggerOutputFileTxt outputFile = new LoggerOutputFileTxt(streamDestination, WriterData.PrefixFileName,
                Path.GetFileName(fileName));
            LoggerOutputFileYml outputFileYml = new LoggerOutputFileYml(streamDestinationYml, WriterData.PrefixFileName,
                Path.GetFileName(fileName));
            try
            {
                var lCR2W = new LoggerCR2W(fileName, outputFile, outputFileYml, CR2WOptions);
                outputFile.WriteLine("FILE: " + fileName);
                if (CR2WOptions.DumpYML)
                {
                    outputFileYml.WriteLine("templates:");
                    outputFileYml.WriteLine("  ### FILE: " + fileName);
                }
                lCR2W.OnException += (msg, ex) =>
                {
                    OnExceptionFile?.Invoke(fileName, msg + ex.Message);
                };
                lCR2W.processCR2W();
                if (lCR2W.ExceptionCount > 0)
                    lock(statusLock)
                        WriterData.Status.Exceptions++;
            }
            catch (FormatException)
            {   // Non CR2W file.
                string msg = fileName + ": Not a valid CR2W file, or file is damaged.";
                Console.WriteLine(msg);
                lock (statusLock)
                {   // File wasn't CR2W, so move its count from Matching to NonCR2W.
                    WriterData.Status.NonCR2W++;
                    WriterData.Status.Matching--;
                }
                var fileNameNoSourcePath = FileNameNoSourcePath(fileName, WriterData.SourcePath);
                OnNonCR2WFile?.Invoke(fileNameNoSourcePath);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                string msg = "Could not find file or directory - did it get deleted? Skipping.";
                ExceptionOccurred(fileName, msg);
            }
            catch (Exception ex)
            {
                string msg = fileName + ": Exception: " + ex.ToString();
                outputFile.WriteLine(msg);
                Console.WriteLine(msg);
                ExceptionOccurred(fileName, "An exception occurred processing this file. Skipping. Details: " + ex.Message);
            }
        }
    }

    internal class LoggerOutputFileYml : LoggerOutputFile
    {
        private string Prefix { get; }
        private bool PrefixLine { get; }
        private string TempLine { get; set; }
        internal LoggerOutputFileYml(StreamWriter streamDestination, bool prefixLine, string prefix)
            : base(streamDestination)
        {
            PrefixLine = prefixLine;
            Prefix = prefix;
        }
        public void WriteLine(string line)
        {
            OutputFile.WriteLine(line);
        }
        public override void Write(string text, int level = 0)
        {
            string line;
            string indent = "";

            for (int i = 0; i < level; i++)
                indent += "  ";

            line = indent + text;

            WriteLine(line);
        }
    }

    internal class LoggerOutputFileTxt : LoggerOutputFile
    {
        private string Prefix { get; }
        private bool PrefixLine { get; }
        internal LoggerOutputFileTxt(StreamWriter streamDestination, bool prefixLine, string prefix)
            : base(streamDestination)
        {
            PrefixLine = prefixLine;
            Prefix = prefix;
        }
        public void WriteLine(string line)
        {
            OutputFile.WriteLine(line);
        }
        public override void Write(string text, int level = 0)
        {
            string line;
            string indent = "";

            for (int i = 0; i < level; i++)
                indent += "    ";

            if (PrefixLine)
                line = Prefix + ":" + indent + text;
            else
                line = indent + text;

            WriteLine(line);
        }
    }
    internal abstract class LoggerOutputFile
    {
        protected StreamWriter OutputFile { get; }
        internal LoggerOutputFile(StreamWriter streamDestination)
        {
            OutputFile = streamDestination;
        }
        public abstract void Write(string text, int level = 0);
    }

    internal class LoggerWriterData
    {
        public CancellationToken CancelToken { get; set; }
        public StatusController Status { get; set; }
        public List<string> Files { get; set; }
        public bool OutputSingleFile { get; set; }
        public int NumThreads { get; set; }
        public string SourcePath { get; set; }
        public string OutputLocation { get; set; }
        public bool CreateFolders { get; set; }
        public bool PrefixFileName { get; set; }
        public bool OverwriteFiles { get; set; }
    }
    internal struct LoggerCR2WOptions
    {
        public int StartingIndentLevel { get; set; }
        public bool ListEmbedded { get; set; }
        public bool DumpSDB { get; set; }
        public bool DumpFCD { get; set; }
        public bool DumpYML { get; set; }
        public bool DumpTXT { get; set; }
        public bool DumpOnlyEdited { get; set; }
        public bool LocalizeStrings { get; set; }
    }
    internal class LoggerCR2W
    {
        public int ExceptionCount = 0;
        public delegate void OnExceptionDelegate(string msg, Exception e);
        public OnExceptionDelegate OnException;
        private CR2WFile CR2W { get; }
        private string CR2WFilePath { get; set; }
        private LoggerOutputFile Writer { get; }
        private LoggerOutputFile WriterYml { get; }
        private LoggerCR2WOptions Options { get; }
        private List<CR2WExportWrapper> Chunks { get; }
        private List<CR2WEmbeddedWrapper> Embedded { get; }
        private HashSet<string> wasDumped = new HashSet<string>();
        private Dictionary<string, CR2WExportWrapper> chunkByREDName = new Dictionary<string, CR2WExportWrapper>();
        private List<List<string>> mapInArray = new List<List<string>>();
        private HashSet<string> enumTypes = new HashSet<string> { "EDrawableFlags", "ELightChannel", "EInterpMethodType", "EInterpCurveMode", "ECompareOp", "ESpaceFillMode", "EComboAttackResponse", "ECameraState", "ECameraShakeState", "ECameraShakeMagnitude", "EDismembermentEffectTypeFlag", "ETriggerChannel", "EDayPart", "EGameplayMimicMode", "EPlayerGameplayMimicMode", "ESoundGameState", "ESoundEventSaveBehavior", "EStaticCameraAnimState", "EStaticCameraGuiEffect", "ECharacterPowerStats", "ECharacterRegenStats", "EDirection", "EDirectionZ", "EMoonState", "EWeatherEffect", "EScriptedEventCategory", "EScriptedEventType", "EInputDeviceType", "EncumbranceBoyMode", "EActorImmortalityMode", "EActorImmortalityChanel", "ETerrainType", "EAreaName", "EDlcAreaName", "EZoneName", "EHitReactionType", "EFocusHitReaction", "EAttackSwingType", "EAttackSwingDirection", 
            "EManageGravity", "ECounterAttackSwitch", "EAttitudeGroupPriority", "ETimescaleSource", "EMonsterCategory", "EButtonStage", "EStaminaActionType", "EFocusModeSoundEffectType", "EStatistic", "EAchievement", "ETutorialHintDurationType", "ETutorialHintPositionType", "ESpeedType", "EBloodType", "EStatOwner", "ETestSubject", "ETargetName", "EMonsterTactic", "EOperator", "ESpawnPositionPattern", "ESpawnRotation", "EFlyingCheck", "ECriticalEffectCounterType", "EFairytaleWitchAction", "EActionInfoType", "EBossAction", "EBossSpecialAttacks", "EEredinPhaseChangeAction", "ESpawnCondition", "ENPCCollisionStance", "ENPCBaseType", "EGuardState", "ENPCType", "EChosenTarget", "ETeleportType", "ECameraAnimPriority", "ECameraBlendSpeedMode", "EMerchantMapPinType", "EScriptedDetroyableComponentState", "EFoodGroup", "EClimbProbeUsed", "ESideSelected", "EPlayerCollisionStance", "EMovementCorrectionType", "EGameplayContextInput", "EExplorationStateType", 
            "EBehGraphConfirmationState", "EAirCollisionSide", "EClimbRequirementType", "EClimbRequirementVault", "EClimbRequirementPlatform", "EClimbHeightType", "EClimbDistanceType", "EClimbEndReady", "EOutsideCapsuleState", "EPlayerIdleSubstate", "ExplorationInteractionType", "EJumpSubState", "EJumpType", "ELandPredictionType", "ELandType", "ELandRunForcedMode", "EPushSide", "ESlidingSubState", "ESlideCameraShakeState", "EFallType", "ECollisionTrajecoryStatus", "ECollisionTrajecoryExplorationStatus", "ECollisionTrajectoryPart", "ECollisionTrajectoryToWaterState", 
            "EDoorMarkingState", "EntityType", "ESkillColor", "ESkillPath", "ESkillSubPath", "EPlayerMutationType", "EActionHitAnim", "EAlchemyExceptions", "EAlchemyCookedItemType", "EBirdType", "EWhaleMovementPatern", "EJobTreeType", "ECraftsmanType", "ECraftingException", "ECraftsmanLevel", "EItemUpgradeException", "EElevatorSwitchType", "ETrapOperation", "EEffectInteract", "EEffectType", "ECriticalHandling", "EEncounterMonitorCounterType", "EEncounterSpawnGroup", "EFocusModeChooseEntityStrategy", "ETriggeredDamageType", "EIllusionDiscoveredOneliner", "EDoorOperation", "EMonsterNestType", "ENestType", "ENewDoorOperation", "EShrineBuffs", "EToxicCloudOperation", "EOilBarrelOperation", 
            "EArmorType", "EEquipmentSlots", "EItemGroup", "EInventoryFilterType", "EInventoryActionType", "ECompareType", "ESpendablePointType", "EEP2PoiType", "EPhysicalDamagemechanismOperation", "ESwitchState", "EResetSwitchMode", "ERequiredSwitchState", "EEncounterOperation", "EFactOperation", "EOcurrenceTime", "ELogicalOperator", "EBoidClueState", "EMonsterCluesTypes", "EMonsterSize", "EMonsterEmittedSound", "EMonsterDamageMarks", "EMonsterVictimState", "EMonsterApperance", "EMonsterSkinFacture", "EMonsterMovement", "EMonsterBehaviour", "EMonsterAttitude", "EMonsterAttackTime", "EMonsterHideout", 
            "EFocusClueAttributeAction", "EClueOperation", "EFocusClueMedallionReaction", "EPlayerVoicesetType", "EMonsterClueAnim", "EReputationLevel", "EFactionName", "ETutorialMessageType", "EUITutorialTriggerCondition", "EUserDialogButtons", "EUniqueMessageIDs", "ELockedControlScheme", "EGamepadType", "ECursorType", "EGuiSceneControllerRenderFocus", "EQuantityTransferFunction", "EHudVisibilitySource", "EFloatingValueType", "EUpdateEventType", "EMutationFeedbackType", "EIngameMenuConstants", "EMutationResourceType", "EBonusSkillSlot", "EInventoryMenuState", "ENotificationType", "EItemSelectionPopupMode", "EUserMessageAction", 
            "EUserMessageProgressType", "EPreporationItemType", "EBackgroundNPCWork_Single", "EBackgroundNPCWork_Paired", "EBgNPCType", "EBackgroundNPCWomanWork", "EConverserType", "EDeathType", "EFinisherDeathType", "EActionFail", "ETauntType", "EBehaviorGraph", "EExplorationMode", "EAgonyType", "ENPCFightStage", "ECriticalStateType", "EHitReactionDirection", "EHitReactionSide", "EDetailedHitType", "EAttackType", "EChargeAttackType", "EDodgeType", "EDodgeDirection", "ETurnDirection", "ETargetDirection", "ENpcPose", "EFlightStance", "ENPCRightItemType", "ENPCLeftItemType", "EInventoryFundsType", 
            "EWeaponSubType1Handed", "EWeaponSubType2Handed", "EWeaponSubTypeRanged", "ENpcWeapons", "ENpcFightingStyles", "EAnimalType", "EPlayerDeathType", "EAimType", "EPlayerMode", "EForceCombatModeReason", "EGeneralEnum", "EPlayerExplorationAction", "EPlayerBoatMountFacing", "EPlayerAttackType", "ESkill", "EItemSetBonus", "EItemSetType", "EPlayerCommentary", "EPlayerWeapon", "EPlayerRangedWeapon", "EPlayerCombatStance", "ESignType", "EMoveSwitchDirection", "EPlayerEvadeType", "EPlayerEvadeDirection", "EPlayerParryDirection", "EPlayerRepelType", "ERotationRate", "EItemType", "ESpecialAbilityInput", 
            "EThrowStage", "EParryStage", "EParryType", "EAttackSwingRange", "EInputActionBlock", "EPlayerMoveType", "EPlayerActionToRestore", "EPlayerInteractionLock", "EPlayerPreviewInventory", "EDismembermentWoundTypes", "ERecoilLevel", "EPlayerMovementLockType", "EHorseMode", "ECustomCameraType", "ECustomCameraController", "EInitialAction", "EDir", "EPlayerStopPose", "EVehicleCombatAction", "EBookDirection", "EQuestSword", "EFactValueChangeMethod", "EMapPinStatus", "EFocusEffectActivationAction", "ECameraEffect", "EQuestReplacerEntities", "EItemSelectionType", "EQuestNPCStates", "EDrawWeaponQuestType", "ESwarmStateOnArrival", "EAnimalReaction", 
            "EDoorQuestState", "EGeraltPath", "EDM_MappinType", "EGwentCardFaction", "EGwentDeckUnlock", "EEnableMode", "EHudTimeOutAction", "EQuestPadVibrationStrength", "ELanguageCheckType", "ECheckedLanguage", "EQuestConditionDLCType", "EContainerMode", "EQuestPlayerSkillLevel", "EQuestPlayerSkillCondition", "EQuestConditionPlayerState", "ESwitchStateCondition", "EPlayerReplacerType", "EStorySceneOutputAction", "EStorySceneGameplayAction", "ENegotiationResult", "ECollectItemsRes", "ECollectItemsCustomRes", "EHorseWaterTestResult" };
        private static CR2WFile LoadCR2W(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                var cr2wfile = new CR2WFile();
                cr2wfile.Read(reader);
                return cr2wfile;
            }
                     
        }
        internal LoggerCR2W(string fileName, LoggerOutputFile writer, LoggerOutputFile writerYml, LoggerCR2WOptions options)
            : this(LoadCR2W(fileName), fileName, writer, writerYml, options) {}
        internal LoggerCR2W(CR2WFile cr2wFile, string filePath, 
            LoggerOutputFile writer, LoggerOutputFile writerYml, LoggerCR2WOptions options)
        {
            CR2W = cr2wFile;
            CR2WFilePath = filePath;
            Chunks = CR2W.chunks;
            Embedded = CR2W.embedded;
            Writer = writer;
            WriterYml = writerYml;
            Options = options;
            if (Options.LocalizeStrings)
                CR2W.LocalizedStringSource = MainController.Get();

            setupYmlMapCases();
            foreach (var chunk in Chunks)
            {
                try
                {
                    chunkByREDName.Add(chunk.REDName, chunk);
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("processCR2W::Null chunk!");
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("processCR2W::Already existing chunk name!");
                }
            }
        }
        private void setupYmlMapCases()
        {
            // --- dlc mounters
            mapInArray.Add(new List<string> { "CR4WorldDLCMounter", "worlds", "worldName" });
            // --- entity template
            mapInArray.Add(new List<string> { "CEntityTemplate", "appearances", "name" });
            mapInArray.Add(new List<string> { "CEntityTemplate", "bodyParts", "name" });
            mapInArray.Add(new List<string> { "CEntityTemplate", "slots", "name" });
            mapInArray.Add(new List<string> { "CEntityTemplate", "effects", "name" });
            mapInArray.Add(new List<string> { "CByteArray", "streamingDataBuffer", "name" });
            // --- cutscenes
            mapInArray.Add(new List<string> { "CCutsceneTemplate", "effects", "name" });
            // --- entities
            mapInArray.Add(new List<string> { "CAnimSlotsParam", "animationSlots", "name" });
            mapInArray.Add(new List<string> { "CEntityDismemberment", "wounds", "name" });
            mapInArray.Add(new List<string> { "SDismembermentWoundSingleSpawn", "additionalEffects", "name" });
            mapInArray.Add(new List<string> { "CAttackRangeParam", "attackRanges", "name" });
            mapInArray.Add(new List<string> { "CR4LootParam", "containers", "name" });
            mapInArray.Add(new List<string> { "CVoicesetParam", "slots", "name" });
            mapInArray.Add(new List<string> { "CEntity", "components", "name" });
            mapInArray.Add(new List<string> { "CActionPoint", "events", "eventName" });
            mapInArray.Add(new List<string> { "CFXDefinition", "trackGroups", "name" });
            mapInArray.Add(new List<string> { "CFXTrackGroup", "tracks", "name" });
            // --- components
            mapInArray.Add(new List<string> { "CSoundAmbientAreaComponent", "dynamicEvents", "eventName" });
            mapInArray.Add(new List<string> { "CDropPhysicsComponent", "dropSetups", "name" });
            mapInArray.Add(new List<string> { "CSlotComponent", "slots", "slotName" });
            mapInArray.Add(new List<string> { "CSoundEmitterComponent", "switchesOnAttach", "SoundSwitch" });
            mapInArray.Add(new List<string> { "CSoundEmitterComponent", "rtpcsOnAttach", "SoundProperty" });
            mapInArray.Add(new List<string> { "CAdvancedVehicleComponent", "passengerSeats", "name" });
            // --- particles
            mapInArray.Add(new List<string> { "CParticleSystem", "emitters", "editorName" });
            mapInArray.Add(new List<string> { "CParticleEmitter", "modules", "editorName" });
        }
        public void processCR2W(int overrideLevel = 0)
        {
            int level = (overrideLevel > 0) ? overrideLevel : Options.StartingIndentLevel;
            if (Options.ListEmbedded && Embedded != null && Embedded.Any())
            {
                Writer.Write("Embedded files:", level);
                ProcessEmbedded(level);
            }
            Writer.Write("Chunks:", level);

            foreach (var chunk in Chunks)
            {
                var dumpYml = Options.DumpYML && chunkByREDName.ContainsKey(chunk.REDName);
                chunkByREDName.Remove(chunk.REDName);

                //var node = GetNodes(chunk);
                if (Options.DumpTXT)
                {
                    Writer.Write(chunk.REDName + " (" + chunk.REDType + ") : " + chunk.Preview, level);
                    foreach (var item in chunk.GetEditableVariables())
                    {
                        if (!Options.DumpOnlyEdited || item.IsSerialized)
                            ProcessNode(item, level + 1);
                    }
                }
                if (dumpYml)
                {
                    WriterYml.Write(CR2WFilePath.Split('\\').Last() + ":", level);
                    foreach (var item in chunk.GetEditableVariables())
                    {
                        if (!Options.DumpOnlyEdited || item.IsSerialized)
                            ProcessNodeYml(item, level + 1);
                    }
                }
                    
            }
        }
        private void ProcessEmbedded(int level)
        {
            //TODO: Also dump the embedded files? (optionally)
            int fileCounter = 1;
            foreach (CR2WEmbeddedWrapper embed in Embedded)
            {
                Writer.Write("(" + fileCounter++ + "):", level);
                Writer.Write("Index: " + embed.Embedded.importIndex, level + 1);
                Writer.Write("ImportPath: " + embed.ImportPath, level + 1);
                Writer.Write("ImportClass: " + embed.ImportClass, level + 1);
                Writer.Write("Size: " + embed.Embedded.dataSize, level + 1);
                Writer.Write("ClassName: " + embed.ClassName, level + 1);
                Writer.Write("Handle: " + embed.Handle, level + 1);
            }
        }
        private void ProcessDataBuffer(IEditableVariable node, int level, bool ymlDump)
        {
            try
            {
                var ls = new LoggerService();
                CR2WFile embedcr2w = new CR2WFile(ls);

                CR2W.Types.CByteArray bArray = null;
                if (node.REDType == "array:2,0,Uint8")
                {
                    bArray = (CR2W.Types.CByteArray)node;
                }
                else if (node.REDType == "SharedDataBuffer")
                {
                    CR2W.Types.SharedDataBuffer SDB = (CR2W.Types.SharedDataBuffer)node;
                    if (SDB != null)
                        bArray = SDB.Bufferdata;
                }
                else if (node.REDType == "DataBuffer")
                {
                    CR2W.Types.DataBuffer DB = (CR2W.Types.DataBuffer)node;
                    if (DB != null)
                        bArray = DB.Bufferdata;
                }
                if (bArray == null || bArray.GetBytes() == null)
                    return;

                switch (embedcr2w.Read(bArray.GetBytes()))
                {
                    case EFileReadErrorCodes.NoError:
                        LoggerCR2WOptions newOptions = Options;
                        if (ymlDump)
                            newOptions.DumpTXT = false;
                        else
                            newOptions.DumpYML = false;
                        var lc = new LoggerCR2W(embedcr2w, node.REDName, Writer, WriterYml, newOptions);
                        lc.processCR2W(level);

                        break;
                    case EFileReadErrorCodes.NoCr2w:
                        break;
                    case EFileReadErrorCodes.UnsupportedVersion:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (FormatException)
            {
                // Embedded buffer/array:2,0,Uint8 was not a CR2W file. Do nothing.
            }
            catch (Exception e)
            {
                string msg = node.REDName + ":" + node.REDType + ": ";
                string logMsg = msg + ": Buffer or 'array:2,0,Uint8' caught exception: ";
                Writer.Write(logMsg + e, level);
                Console.WriteLine(logMsg + e);
                OnException?.Invoke(msg, e);
                ExceptionCount++;
            }
        }
        private void ProcessNode(IEditableVariable node, int level)
        {
            //Console.WriteLine(new String(' ', level) + "> node: " + node.REDName + ", type: " + node.REDType + ", value: " + node.REDValue);
            if (node.REDName == "unknownBytes" && node.ToString() == "0 bytes"
                || node.REDName == "unk1" && node.ToString() == "0")
                return;

            if (node.REDName == "Parent" && node.ToString() == "NULL")
                return;

            if (node.REDName != node.ToString()) // Chunk node is already printed in processCR2W, so don't print it again.
            {
                Writer.Write(node.REDName + " (" + node.REDType + ") : " + node.ToString(), level);
                level++;
            }

            if (node.GetEditableVariables().Count > 0)
                foreach (var child in node.GetEditableVariables())
                {
                    if (!Options.DumpOnlyEdited || child.IsSerialized)
                        ProcessNode(child, level);
                }


            if ( ( (node.REDType == "SharedDataBuffer" || node.REDType == "DataBuffer") && Options.DumpSDB) 
                || (node.REDType == "array:2,0,Uint8" && node.REDName != "deltaTimes") ) 
            {   // Embedded CR2W dump:
                // Dump SharedDataBuffer if option is set.
                // Dump "array:2,0,Uint8", unless it's called "deltaTimes" (not CR2W)
                // And dump FCD only if options.dumpFCD is set.
                if (node.REDName != "flatCompiledData" || Options.DumpFCD)
                {
                    ProcessDataBuffer(node, level, false);
                }
            }
        }
        List<string> getUnsupportedVarsByType(string type)
        {
            if (type.StartsWith("CFXTrackItem"))
                type = "CFXTrackItem";
            if (type.EndsWith("Attachment"))
                type = "IAttachment";
            List<string> ret = new List<string>();
            switch (type)
            {
                case "IAttachment":
                    ret.Add("parent");
                    break;
                case "CEntityTemplate":
                    //? ret.Add("overrides");
                    ret.Add("entityClass");
                    ret.Add("properOverrides");
                    ret.Add("cookedEffectsVersion");
                    break;
                case "SAppearanceAttachment":
                    ret.Add("Data");
                    break;
                case "CFXTrackItem":
                    ret.Add("count");
                    ret.Add("unk");
                    ret.Add("buffername");
                    break;
                case "CFXDefinition":
                case "CFXTrack":
                case "CFXTrackGroup":
                    ret.Add("name");
                    break;
            }
            return ret;
        }
        string prepareName(string name)
        {
            if (name.All(Char.IsDigit))
            {
                name = '\"' + name + '\"';
            }
            int pos = name.IndexOf('#');
            if (pos > 0)
                return name.Substring(0, pos - 1);
            else
                return name;
        }
        bool isArrayType(string type)
        {
            return !string.IsNullOrEmpty(type) && (type.StartsWith("array:") || type.StartsWith("CBuffer"));
        }
        string prepareValue(IEditableVariable node, string type = "")
        {
            if (type == "")
                type = node.REDType;
            string value = string.IsNullOrEmpty(node.REDValue) ? "" : node.REDValue;

            Func<string, string> wrapValue = x =>
            {
                string ret = x.Replace('\\', '/');
                int pos = ret.LastIndexOf(": ");
                if (pos > 0)
                    ret = ret.Substring(pos + 2);
                return ret;
            };
            if (string.IsNullOrEmpty(type))
            {
                return wrapValue(value);
            }
            else if (isArrayType(type))
            {
                return "# " + type;
            }
            else if (type == "Bool")
            {
                return (value == "True" ? "true" : "false");
            }
            else if (type == "Float")
            {
                string ret = value.Replace(',', '.');
                foreach (char c in ret) // check if it is correct number
                {
                    if ((c < '0' || c > '9') && c != '-' && c != '.' && c != 'E')
                    {
                        ret = "0.0";
                        break;
                    }
                }
                if (!ret.Contains('.'))
                {
                    ret += ".0";
                }
                return ret;
            }
            else if (type.StartsWith("Int") || type.StartsWith("Uint"))
            {
                foreach (char c in value) // check if it is correct number
                {
                    if ((c < '0' || c > '9') && c != '-')
                    {
                        value = "0";
                        break;
                    }
                }
                return value;
            }
            else if (type == "IdTag")
            {
                int pos = value.LastIndexOf("] ");
                if (pos > 0)
                    value = value.Substring(pos + 1);
                return value;
            }
            else if (type == "String")
            {
                return "\"" + wrapValue(value) + "\"";
            }
            else if (type == "CName")
            {
                return wrapValue(value);
            }
            else
            {
                return wrapValue(value);
            }
        }
        private void ProcessNodeYml_EngineTransform(IEditableVariable node, int cur_level, bool isArrayElement = false)
        {
            if (isArrayElement)
                WriterYml.Write("- \".type\": " + node.REDType, cur_level);
            else
                WriterYml.Write(prepareName(node.REDName) + ":", cur_level);
            string x = "0.0", y = "0.0", z = "0.0", pitch = "0.0", yaw = "0.0", roll = "0.0", scale_x = "1.0", scale_y = "1.0", scale_z = "1.0";
            List<IEditableVariable> children = node.GetEditableVariables();
            
            foreach (var child in children)
            {
                switch (child.REDName)
                {
                    case "X":
                        x = prepareValue(child);
                        break;
                    case "Y":
                        y = prepareValue(child);
                        break;
                    case "Z":
                        z = prepareValue(child);
                        break;
                    case "Pitch":
                        pitch = prepareValue(child);
                        break;
                    case "Yaw":
                        yaw = prepareValue(child);
                        break;
                    case "Roll":
                        roll = prepareValue(child);
                        break;
                    case "Scale_x":
                        scale_x = prepareValue(child);
                        break;
                    case "Scale_y":
                        scale_y = prepareValue(child);
                        break;
                    case "Scale_z":
                        scale_z = prepareValue(child);
                        break;
                }
            }
            WriterYml.Write("pos: [ " + x + ", " + y + ", " + z + " ]", cur_level + 1);
            WriterYml.Write("rot: [ " + pitch + ", " + yaw + ", " + roll + " ]", cur_level + 1);
            WriterYml.Write("scale: [ " + scale_x + ", " + scale_y + ", " + scale_z + " ]", cur_level + 1);
        }
        private void ProcessNodeYml_Vector(IEditableVariable node, int cur_level, bool isArrayElement = false)
        {
            string X = "0.0", Y = "0.0", Z = "0.0", W = "0.0";
            List<IEditableVariable> children = node.GetEditableVariables();

            foreach (var child in children)
            {
                switch (child.REDName)
                {
                    case "X":
                        X = prepareValue(child);
                        break;
                    case "Y":
                        Y = prepareValue(child);
                        break;
                    case "Z":
                        Z = prepareValue(child);
                        break;
                    case "W":
                        W = prepareValue(child);
                        break;
                }
            }
            WriterYml.Write((isArrayElement ? "- " : (prepareName(node.REDName) + ": ")) + "[ " + X + ", " + Y + ", " + Z + ", " + W + " ]", cur_level);
        }
        private void ProcessNodeYml_EulerAngles(IEditableVariable node, int cur_level, bool isArrayElement = false)
        {
            string pitch = "0.0", yaw = "0.0", roll = "0.0";
            List<IEditableVariable> children = node.GetEditableVariables();

            foreach (var child in children)
            {
                switch (child.REDName)
                {
                    case "Pitch":
                        pitch = prepareValue(child);
                        break;
                    case "Yaw":
                        yaw = prepareValue(child);
                        break;
                    case "Roll":
                        roll = prepareValue(child);
                        break;
                }
            }
            WriterYml.Write((isArrayElement ? "- " : (prepareName(node.REDName) + ": ")) + "[ " + pitch + ", " + yaw + ", " + roll + " ]", cur_level);
        }
        private void ProcessNodeYml_Color(IEditableVariable node, int cur_level, bool isArrayElement = false)
        {
            string Red = "0.0", Green = "0.0", Blue = "0.0", Alpha = "-1.0";
            List<IEditableVariable> children = node.GetEditableVariables();

            foreach (var child in children)
            {
                switch (child.REDName)
                {
                    case "Red":
                        Red = prepareValue(child);
                        break;
                    case "Green":
                        Green = prepareValue(child);
                        break;
                    case "Blue":
                        Blue = prepareValue(child);
                        break;
                    case "Alpha":
                        Alpha = prepareValue(child);
                        break;
                }
            }
            WriterYml.Write((isArrayElement ? "- " : (prepareName(node.REDName) + ": ")) + "[ " + Red + ", " + Green + ", " + Blue + ((Alpha == "-1.0") ? "" : (", " + Alpha)) + " ]", cur_level);
        }
        private void ProcessNodeYml_cookedEffects(IEditableVariable node, int cur_level, bool isArrayElement = false)
        {
            List<IEditableVariable> children = node.GetEditableVariables();

            if (children.Count() < 1)
                return;
            WriterYml.Write("effects:", cur_level);
            ++cur_level;
            int nonameEffectsCount = 0;

            foreach (var i_child in children)
            {
                var i_children = i_child.GetEditableVariables();
                if (i_children.Count() < 1)
                    return;
                string name = "";
                string buffer_size = "";
                IEditableVariable bufferNode = null;
                foreach (var j_child in i_children)
                {
                    if (j_child.REDName == "name")
                        name = j_child.REDValue;
                    else if (j_child.REDType == "SharedDataBuffer")
                    {
                        bufferNode = j_child;
                    }
                }
                if (bufferNode != null)
                {
                    if (name == "")
                        name = "noname_effect_" + nonameEffectsCount++;
                    CR2W.Types.SharedDataBuffer SDB = (CR2W.Types.SharedDataBuffer)bufferNode;
                    if (SDB != null)
                        buffer_size = SDB.Bufferdata.ToString();
                    WriterYml.Write("#buffer: " + buffer_size, cur_level);
                    bufferNode.SetREDName(name);
                    ProcessDataBuffer(bufferNode, cur_level, true);
                }
            }
        }
        private void ProcessNodeYml_interpolationBuffer(IEditableVariable node, int cur_level, bool isArrayElement = false)
        {
            List<IEditableVariable> children = node.GetEditableVariables();

            if (children.Count() < 1)
                return;
            string[,] vars = new string[16, 4];
            int rows = 0;

            foreach (var child in children)
            {
                List<IEditableVariable> i_children = child.GetEditableVariables();
                if (i_children.Count() < 16)
                    return;
                for (int j = 0; j < i_children.Count() && j < 16; ++j)
                {
                    vars[j, rows] = prepareValue(i_children[j], "Float");
                }
                ++rows;
            }

            WriterYml.Write("interpolation:", cur_level);
            for (int j = 0; j < 16; ++j)
            {
                string tmp_var = "- [ " + vars[j, 0];
                for (int i = 1; i < rows; ++i)
                {
                    tmp_var += ", " + vars[j, i];
                }
                tmp_var += " ]";

                if (rows == 1)
                    tmp_var = "- " + vars[j, 0];

                WriterYml.Write(tmp_var, cur_level + 1);
            }
        }
        private void ProcessNodeYml_Enum(IEditableVariable node, int cur_level, bool isArrayElement = false)
        {
            string value = node.REDValue ?? "";
            string[] enum_values = value.Split(',');

            if (enum_values.Count() < 1)
                return;

            //Console.WriteLine(" > Enum value: " + node.REDName + ", type: " + node.REDType + ", class: " + node.GetType());
            WriterYml.Write(prepareName(node.REDName) + ":", cur_level);
            foreach (var enum_value in enum_values)
            {
                WriterYml.Write("- " + enum_value, cur_level + 1);
            }
        }
        private void ProcessNodeYml_TagList(IEditableVariable node, int cur_level, bool isArrayElement = false)
        {
            CR2W.Types.CBufferVLQInt32<CR2W.Types.CName> tags = ((CR2W.Types.TagList) node).tags;

            if (tags == null || tags.Count() < 1)
                return;

            WriterYml.Write(prepareName(node.REDName) + ":", cur_level);
            foreach (var tag in tags)
            {
                WriterYml.Write("- " + tag, cur_level + 1);
            }
        }
        string getSpecialYmlMapNameVar(IEditableVariable node)
        {
            if (node.ParentVar == null)
                return "";

            List<int> pos = new List<int>();
            for (int i = 0; i < mapInArray.Count(); ++i)
            {
                if (mapInArray[i][1] == node.REDName)
                {
                    pos.Add(i);
                }
            }
            if (pos.Count() > 0)
            {
                //Console.WriteLine(" ^getSpecialYmlMapNameVar: ok [" + node.REDName + "]");
                Type classInfo = node.ParentVar.GetType();
                List<string> parentClasses = new List<string>();
                while (classInfo != null && classInfo.Name != "CVariable")
                {
                    parentClasses.Add(classInfo.Name);
                    classInfo = classInfo.BaseType;
                }
                
                for (int i = 0; i < pos.Count(); ++i)
                {
                    if (parentClasses.Contains(mapInArray[pos[i]][0]))
                    {
                        //Console.WriteLine(" ^getSpecialYmlMapNameVar: Found! [" + mapInArray[pos[i]][2] + "]");
                        return mapInArray[pos[i]][2];
                    }
                }
            }
            return "";
        }

        private void ProcessNodeYml(IEditableVariable node, int level, bool isArrayElement = false, string useNameVar = "")
        {
            Console.WriteLine(new String(' ', level) + "> node: " + node.REDName + ", type: " + node.REDType + ", value: " + node.REDValue);
            // special cases for rmemr encoder
            if (node.REDType == "EngineTransform")
            {
                ProcessNodeYml_EngineTransform(node, level, isArrayElement);
                return;
            }
            if (node.REDType == "Vector" || node.REDType == "SVector4D")
            {
                ProcessNodeYml_Vector(node, level, isArrayElement);
                return;
            }
            if (node.REDType == "EulerAngles")
            {
                ProcessNodeYml_EulerAngles(node, level, isArrayElement);
                return;
            }
            if (node.REDType == "Color")
            {
                ProcessNodeYml_Color(node, level, isArrayElement);
                return;
            }
            if (node.REDName == "cookedEffects")
            {
                ProcessNodeYml_cookedEffects(node, level);
                return;
            }
            if (node.REDName == "buffer" && node.REDType == "CCompressedBuffer:CBufferUInt16:CFloat")
            {
                ProcessNodeYml_interpolationBuffer(node, level);
                return;
            }
            if (enumTypes.Contains(node.REDType))
            {
                ProcessNodeYml_Enum(node, level);
                return;
            }
            if (node.REDType == "TagList")
            {
                ProcessNodeYml_TagList(node, level);
                return;
            }
            if (node.REDName == "AttachmentsChild")
            {
                node.SetREDName("attachments");
            }

            if (node.REDName == "BufferV1" || node.REDName == "BufferV2" || node.REDName == "unknownBytes"
                || node.REDName == "flatCompiledData" || node.REDName == "AttachmentsReference" || (node.REDName == "Unk1" && node.REDValue == "0"))
                return;

            //if (node.REDName == "Parent" && node.REDValue == "NULL") - Not actual for 0.7
            //    return;

            bool isMeArray = isArrayType(node.REDType);
            List<IEditableVariable> children = node.GetEditableVariables();
            List<string> unsupportedVars = getUnsupportedVarsByType(node.REDType);
            string value = node.REDValue ?? "";
            string mapNameVar = getSpecialYmlMapNameVar(node);
            if (useNameVar == "")
                useNameVar = mapNameVar;

            Console.WriteLine(new String(' ', level) + "> nodeExtra: Childs: " + children.Count());

            if (node.REDName == "AttachmentsChild" && children.Count() == 0)
                return;

            if (node.REDName != node.REDType) // Chunk node is already printed in processCR2W, so don't print it again.
            {
                /* CHUNK-REFERENCE TYPE (ptr, handle) */
                if (value.Contains("#") && !isMeArray)
                {
                    if (chunkByREDName.ContainsKey(value))
                    {
                        var chunk = chunkByREDName[value];
                        chunkByREDName.Remove(chunk.REDName);

                        if (!isArrayElement) // avoid "0" excess chunks
                        {
                            WriterYml.Write(prepareName(node.REDName) + ":", level);
                        }

                        foreach (var item in chunk.GetEditableVariables())
                        {
                            if (!Options.DumpOnlyEdited || item.IsSerialized)
                            {
                                if (unsupportedVars.Contains(item.REDName))
                                {
                                    WriterYml.Write("## " + prepareName(item.REDName) + ": " + prepareValue(item), level + (isArrayElement ? 0 : 1));
                                    continue;
                                }
                                if (isArrayElement)
                                {
                                    ProcessNodeYml(item, level, true, useNameVar);
                                }
                                else
                                {
                                    ProcessNodeYml(item, level + 1, false, useNameVar);
                                }
                            }
                        }
                    } else
                    {
                        WriterYml.Write("#" + prepareName(node.REDName) + ": (looped reference) " + value, level);
                    }
                    //return;
                }
                /* SIMPLE TYPE (int, float, cname, string, guid, handle-path) */
                else if (children.Count() == 0)
                {
                    if (string.IsNullOrEmpty(node.REDValue) || value == "NULL") // empty value, ignore
                    {
                        WriterYml.Write("#" + prepareName(node.REDName) + ": <empty value>", level);
                        return;
                    }

                    if (isArrayElement)
                    {
                        WriterYml.Write("- " + prepareValue(node), level);
                    }
                    else
                    {
                        WriterYml.Write(prepareName(node.REDName) + ": " + prepareValue(node), level);
                    }
                    return;
                }
                /* STRUCT/CLASS TYPE */
                else
                {
                    if (isArrayElement)
                    {
                        if (useNameVar != "")
                        {
                            string mapName = node.REDType + "0";
                            foreach (var child in children)
                            {
                                if (child.REDName == useNameVar)
                                {
                                    mapName = child.REDValue ?? mapName;
                                    break;
                                }
                            }
                            WriterYml.Write(prepareName(mapName) + ":", level);
                            WriterYml.Write("\".type\": " + node.REDType, level + 1);

                            unsupportedVars.Add(useNameVar);
                            useNameVar = "";
                        } else
                        {
                            WriterYml.Write("- \".type\": " + node.REDType, level);
                        }
                    }
                    else
                    {
                        if (isMeArray && children.Count() < 1)
                            WriterYml.Write("# " + prepareName(node.REDName) + ": <null value>  #" + node.REDType, level);
                        else
                            WriterYml.Write(prepareName(node.REDName) + ":  #" + node.REDType, level);
                    }   
                }
            } else
            {   // chunk header - write type/name
                if (isArrayElement)
                {
                    if (useNameVar != "")
                    {
                        string mapName = node.REDType + "0";
                        foreach (var child in children)
                        {
                            if (child.REDName == useNameVar)
                            {
                                mapName = child.REDValue ?? mapName;
                                node.RemoveVariable(child);
                                break;
                            }
                        }
                        WriterYml.Write(prepareName(mapName) + ":", level);
                        WriterYml.Write("\".type\": " + node.REDType, level + 1);

                        unsupportedVars.Add(useNameVar);
                        useNameVar = "";
                    }
                    else
                    {
                        WriterYml.Write("- \".type\": " + node.REDType, level);
                    }
                }
                else
                {
                    WriterYml.Write("\".type\": " + node.REDType, level);
                    --level;
                }
            }

            foreach (var child in children)
            {
                if (!Options.DumpOnlyEdited || child.IsSerialized)
                {
                    if (unsupportedVars.Contains(child.REDName))
                    {
                        WriterYml.Write("## " + prepareName(child.REDName) + ": " + prepareValue(child), level + 1);
                        continue;
                    }
                    ProcessNodeYml(child, level + 1, isMeArray, useNameVar);
                }
            }


            if (((node.REDType == "SharedDataBuffer" || node.REDType == "DataBuffer") && Options.DumpSDB)
                || (node.REDType == "array:2,0,Uint8" && node.REDName != "deltaTimes"))
            {   // Embedded CR2W dump:
                // Dump SharedDataBuffer if option is set.
                // Dump "array:2,0,Uint8", unless it's called "deltaTimes" (not CR2W)
                // And dump FCD only if options.dumpFCD is set.
                if (node.REDName != "flatCompiledData" || Options.DumpFCD)
                {
                    ProcessDataBuffer(node, level + 1, true);
                }
            }
        }
        
        //private VariableListNode GetNodes(CR2WExportWrapper chunk)
        //{
        //    return frmChunkProperties.AddListViewItems(chunk);
        //}
    }
}
