using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using log4net.Config;
using Log2Console.Log;
using Log2Console.Properties;
using Log2Console.Receiver;
using Log2Console.Settings;
using Log2Console.UI;
using Log2Console.UI.ControlExtenders;
using Log2Console.Win32ApiCodePack;
using Timer = System.Threading.Timer;

// Configure log4net using the .config file
[assembly: XmlConfigurator(Watch = true)]


namespace Log2Console
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class MainForm : Form, ILogMessageNotifiable
    {
        private const int _taskbarProgressTimerPeriod = 2000;


        private const int WM_SIZE = 0x0005;
        private const int SIZE_MINIMIZED = 1;
        private readonly ThumbnailToolbarButton _autoScrollWinbarBtn;
        private readonly ThumbnailToolbarButton _clearAllWinbarBtn;

        private readonly DockExtender _dockExtender;

        private readonly Queue<LogMessage> _eventQueue;
        private readonly bool _firstStartup;
        private readonly bool _isWin7orLater;
        private readonly IFloaty _logDetailsPanelFloaty;
        private readonly IFloaty _loggersPanelFloaty;
        private readonly ThumbnailToolbarButton _pauseWinbarBtn;
        private readonly WindowRestorer _windowRestorer;
        private bool _addedLogMessage;
        private bool _ignoreEvents;
        private LoggerItem _lastHighlightedLogger;
        private LoggerItem _lastHighlightedLogMsgs;
        private Timer _logMsgTimer;

        private string _msgDetailText = string.Empty;
        private bool _pauseLog;

        private Timer _taskbarProgressTimer;


        public MainForm()
        {
            InitializeComponent();

            appNotifyIcon.Text = AboutForm.AssemblyTitle;

            levelComboBox.SelectedIndex = 0;

            Minimized += OnMinimized;

            // Init Log Manager Singleton
            LogManager.Instance.Initialize(new TreeViewLoggerView(loggerTreeView), logListView);


            _dockExtender = new DockExtender(this);

            // Dockable Log Detail View
            _logDetailsPanelFloaty = _dockExtender.Attach(logDetailPanel, logDetailToolStrip, logDetailSplitter);
            _logDetailsPanelFloaty.DontHideHandle = true;
            _logDetailsPanelFloaty.Docking += OnFloatyDocking;

            // Dockable Logger Tree
            _loggersPanelFloaty = _dockExtender.Attach(loggerPanel, loggersToolStrip, loggerSplitter);
            _loggersPanelFloaty.DontHideHandle = true;
            _loggersPanelFloaty.Docking += OnFloatyDocking;

            // Settings
            _firstStartup = !UserSettings.Load();
            //TODO: if (_firstStartup)
            if (_firstStartup && !LogFileIsSpecifiedOnTheCommandLine())
            {
                // Initialize default layout
                UserSettings.Instance.Layout.Set(DesktopBounds, WindowState, logDetailPanel, loggerPanel);

                // Force panel to visible
                UserSettings.Instance.Layout.ShowLogDetailView = true;
                UserSettings.Instance.Layout.ShowLoggerTree = true;
                UserSettings.Instance.DefaultFont = Environment.OSVersion.Version.Major >= 6
                    ? new Font("Segoe UI", 9F)
                    : new Font("Tahoma", 8.25F);
            }

            Font = UserSettings.Instance.DefaultFont ?? Font;

            _windowRestorer = new WindowRestorer(this, UserSettings.Instance.Layout.WindowPosition,
                UserSettings.Instance.Layout.WindowState);

            // Windows 7 CodePack (Taskbar icons and progress)
            _isWin7orLater = TaskbarManager.IsPlatformSupported;

            if (_isWin7orLater)
                try
                {
                    // Taskbar Progress
                    TaskbarManager.Instance.ApplicationId = Text;
                    _taskbarProgressTimer = new Timer(OnTaskbarProgressTimer, null, _taskbarProgressTimerPeriod,
                        _taskbarProgressTimerPeriod);

                    // Pause Btn
                    _pauseWinbarBtn = new ThumbnailToolbarButton(Icon.FromHandle(((Bitmap) pauseBtn.Image).GetHicon()),
                        pauseBtn.ToolTipText);
                    _pauseWinbarBtn.Click += pauseBtn_Click;

                    // Auto Scroll Btn
                    _autoScrollWinbarBtn =
                        new ThumbnailToolbarButton(Icon.FromHandle(((Bitmap) autoLogToggleBtn.Image).GetHicon()),
                            autoLogToggleBtn.ToolTipText);
                    _autoScrollWinbarBtn.Click += autoLogToggleBtn_Click;

                    // Clear All Btn
                    _clearAllWinbarBtn =
                        new ThumbnailToolbarButton(Icon.FromHandle(((Bitmap) clearLoggersBtn.Image).GetHicon()),
                            clearLoggersBtn.ToolTipText);
                    _clearAllWinbarBtn.Click += clearAll_Click;

                    // Add Btns
                    TaskbarManager.Instance.ThumbnailToolbars.AddButtons(Handle, _pauseWinbarBtn, _autoScrollWinbarBtn,
                        _clearAllWinbarBtn);
                }
                catch
                {
                    // Not running on Win 7?
                    _isWin7orLater = false;
                }

            ApplySettings(true);

            _eventQueue = new Queue<LogMessage>();

            // Initialize Receivers
            foreach (var receiver in UserSettings.Instance.Receivers)
                InitializeReceiver(receiver);

            // Start the timer to process event logs in batch mode
            _logMsgTimer = new Timer(OnLogMessageTimer, null, 1000, 100);
        }

        public string LogFile { get; set; }

        /// <summary>
        ///     Transforms the notification into an asynchronous call.
        ///     The actual method called to add log messages is 'AddLogMessages'.
        /// </summary>
        public void Notify(LogMessage[] logMsgs)
        {
            //// InvokeRequired required compares the thread ID of the
            //// calling thread to the thread ID of the creating thread.
            //// If these threads are different, it returns true.
            //if (logListView.InvokeRequired)
            //{
            //    NotifyLogMsgsCallback d = AddLogMessages;
            //    Invoke(d, new object[] { logMsgs });
            //}
            //else
            //{
            //    AddLogMessages(logMsgs);
            //}

            lock (_eventQueue)
            {
                foreach (var logMessage in logMsgs) _eventQueue.Enqueue(logMessage);
            }
        }

        /// <summary>
        ///     Transforms the notification into an asynchronous call.
        ///     The actual method called to add a log message is 'AddLogMessage'.
        /// </summary>
        public void Notify(LogMessage logMsg)
        {
            //// InvokeRequired required compares the thread ID of the
            //// calling thread to the thread ID of the creating thread.
            //// If these threads are different, it returns true.
            //if (logListView.InvokeRequired)
            //{
            //    NotifyLogMsgCallback d = AddLogMessage;
            //    Invoke(d, new object[] { logMsg });
            //}
            //else
            //{
            //    AddLogMessage(logMsg);
            //}

            lock (_eventQueue)
            {
                _eventQueue.Enqueue(logMsg);
            }
        }

        // Specific event handler on minimized action
        public event EventHandler Minimized;

        /// <summary>
        ///     Catch on minimize event
        ///     @author : Asbjørn Ulsberg -=|=- asbjornu@hotmail.com
        /// </summary>
        /// <param name="msg"></param>
        protected override void WndProc(ref Message msg)
        {
            if (msg.Msg == WM_SIZE
                && (int) msg.WParam == SIZE_MINIMIZED)
                Minimized?.Invoke(this, EventArgs.Empty);

            base.WndProc(ref msg);
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);

            _windowRestorer?.TrackWindow();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            _windowRestorer?.TrackWindow();
        }

        protected override void OnShown(EventArgs e)
        {
            //TODO:if (_firstStartup)
            if (_firstStartup && !LogFileIsSpecifiedOnTheCommandLine())
            {
                MessageBox.Show(
                    this,
                    @"Welcome to Log2Console! You must configure some Receivers in order to use the tool.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                ShowReceiversForm();
            }
        }

        private bool LogFileIsSpecifiedOnTheCommandLine()
        {
            return !string.IsNullOrWhiteSpace(LogFile) && File.Exists(LogFile);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_logMsgTimer != null)
                {
                    _logMsgTimer.Dispose();
                    _logMsgTimer = null;
                }

                if (_taskbarProgressTimer != null)
                {
                    _taskbarProgressTimer.Dispose();
                    _taskbarProgressTimer = null;
                }

                if (UserSettings.Instance.Layout.LogListViewColumnsWidths == null ||
                    UserSettings.Instance.Layout.LogListViewColumnsWidths.Length != logListView.Columns.Count)
                    UserSettings.Instance.Layout.LogListViewColumnsWidths = new int[logListView.Columns.Count];

                for (var i = 0; i < logListView.Columns.Count; i++)
                    UserSettings.Instance.Layout.LogListViewColumnsWidths[i] = logListView.Columns[i].Width;

                UserSettings.Instance.Layout.Set(
                    _windowRestorer.WindowPosition, _windowRestorer.WindowState, logDetailPanel, loggerPanel);

                UserSettings.Instance.Save();
                UserSettings.Instance.Close();
            }
            catch
            {
                // ignored
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            // Display Version
            versionLabel.Text = AboutForm.AssemblyTitle + @" v" + AboutForm.AssemblyVersion;

            //Load log file if specified as first argument on the command line
            if (!string.IsNullOrEmpty(LogFile) && File.Exists(LogFile))
            {
                var fileReceiver = new FileReceiver
                {
                    FileToWatch = LogFile,
                    FileFormat = FileReceiver.FileFormatEnums.Log4jXml,
                    ShowFromBeginning = true
                };
                InitializeReceiver(fileReceiver);
            }

            DoubleBuffered = true;
            base.OnLoad(e);
        }

        private void OnFloatyDocking(object sender, EventArgs e)
        {
            // make sure the ZOrder remains intact
            logListView.BringToFront();
            BringToFront();
        }

        private void ApplySettings(bool noCheck)
        {
            Opacity = (double) UserSettings.Instance.Transparency / 100;
            ShowInTaskbar = !UserSettings.Instance.HideTaskbarIcon;

            TopMost = UserSettings.Instance.AlwaysOnTop;
            pinOnTopBtn.Checked = UserSettings.Instance.AlwaysOnTop;
            autoLogToggleBtn.Checked = UserSettings.Instance.AutoScrollToLastLog;

            logListView.Font = UserSettings.Instance.LogListFont;
            logDetailTextBox.Font = UserSettings.Instance.LogDetailFont;
            loggerTreeView.Font = UserSettings.Instance.LoggerTreeFont;

            logListView.BackColor = UserSettings.Instance.LogListBackColor;
            logDetailTextBox.BackColor = UserSettings.Instance.LogMessageBackColor;

            LogLevels.Instance.LogLevelInfos[(int) LogLevel.Trace].Color = UserSettings.Instance.TraceLevelColor;
            LogLevels.Instance.LogLevelInfos[(int) LogLevel.Debug].Color = UserSettings.Instance.DebugLevelColor;
            LogLevels.Instance.LogLevelInfos[(int) LogLevel.Info].Color = UserSettings.Instance.InfoLevelColor;
            LogLevels.Instance.LogLevelInfos[(int) LogLevel.Warn].Color = UserSettings.Instance.WarnLevelColor;
            LogLevels.Instance.LogLevelInfos[(int) LogLevel.Error].Color = UserSettings.Instance.ErrorLevelColor;
            LogLevels.Instance.LogLevelInfos[(int) LogLevel.Fatal].Color = UserSettings.Instance.FatalLevelColor;

            levelComboBox.SelectedIndex = (int) UserSettings.Instance.LogLevelInfo.Level;

            if (logListView.ShowGroups != UserSettings.Instance.GroupLogMessages)
            {
                if (noCheck)
                {
                    logListView.ShowGroups = UserSettings.Instance.GroupLogMessages;
                }
                else
                {
                    var res = MessageBox.Show(
                        this,
                        @"You changed the Message Grouping setting, the Log Message List must be cleared, OK?",
                        Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                    if (res == DialogResult.OK)
                    {
                        ClearAll();
                        logListView.ShowGroups = UserSettings.Instance.GroupLogMessages;
                    }
                    else
                    {
                        UserSettings.Instance.GroupLogMessages = !UserSettings.Instance.GroupLogMessages;
                    }
                }
            }

            //See if the Columns Changed
            var columnsChanged = false;

            if (logListView.Columns.Count != UserSettings.Instance.ColumnConfiguration.Length)
                columnsChanged = true;
            else
                for (var i = 0; i < UserSettings.Instance.ColumnConfiguration.Length; i++)
                    if (!UserSettings.Instance.ColumnConfiguration[i].Name.Equals(logListView.Columns[i].Text))
                    {
                        columnsChanged = true;
                        break;
                    }

            if (columnsChanged)
            {
                logListView.Columns.Clear();
                foreach (var column in UserSettings.Instance.ColumnConfiguration) logListView.Columns.Add(column.Name);
            }

            // Layout
            if (noCheck)
            {
                DesktopBounds = UserSettings.Instance.Layout.WindowPosition;
                WindowState = UserSettings.Instance.Layout.WindowState;

                ShowDetailsPanel(UserSettings.Instance.Layout.ShowLogDetailView);
                logDetailPanel.Size = UserSettings.Instance.Layout.LogDetailViewSize;

                ShowLoggersPanel(UserSettings.Instance.Layout.ShowLoggerTree);
                loggerPanel.Size = UserSettings.Instance.Layout.LoggerTreeSize;

                if (UserSettings.Instance.Layout.LogListViewColumnsWidths != null)
                    for (var i = 0; i < UserSettings.Instance.Layout.LogListViewColumnsWidths.Length; i++)
                        if (i < logListView.Columns.Count)
                            logListView.Columns[i].Width = UserSettings.Instance.Layout.LogListViewColumnsWidths[i];
            }
        }

        private void InitializeReceiver(IReceiver receiver)
        {
            try
            {
                receiver.Initialize();
                receiver.Attach(this);

                //LogManager.Instance.SetRootLoggerName(String.Format("Root [{0}]", receiver));
            }
            catch (Exception ex)
            {
                try
                {
                    receiver.Terminate();
                }
                catch
                {
                    // ignored
                }

                ShowErrorBox("Failed to Initialize Receiver: " + ex.Message);
            }
        }

        private void TerminateReceiver(IReceiver receiver)
        {
            try
            {
                receiver.Detach();
                receiver.Terminate();
            }
            catch (Exception ex)
            {
                ShowErrorBox("Failed to Terminate Receiver: " + ex.Message);
            }
        }

        private void Quit()
        {
            Close();
        }

        private void ClearLogMessages()
        {
            SetLogMessageDetail(null);
            LogManager.Instance.ClearLogMessages();
        }

        private void ClearLoggers()
        {
            SetLogMessageDetail(null);
            LogManager.Instance.ClearAll();
        }

        private void CollapseLoggers()
        {
            loggerTreeView.CollapseAll();
            loggerTreeView.TopNode.Expand();
        }

        private void DeactivateSelectedLoggers()
        {
            logListView.BeginUpdate();
            RemovedLoggerHighlight();

            if (logListView.SelectedItems.Count > 0)
                foreach (ListViewItem selectedItem in logListView.SelectedItems)
                {
                    if (selectedItem.Tag is LogMessageItem logMsgItem)
                    {
                        logMsgItem.Parent.Enabled = false;
                        _lastHighlightedLogger = null;
                    }
                }

            logListView.EndUpdate();
        }

        private void DeactivateNotSelectedLoggers()
        {
            logListView.BeginUpdate();

            RemovedLoggerHighlight();

            if (logListView.SelectedItems.Count > 0)
            {
                var selectedItems = new List<LogMessageItem>();
                foreach (var item in logListView.SelectedItems.Cast<ListViewItem>())
                {
                    if (item.Tag is LogMessageItem logMsgItem) selectedItems.Add(logMsgItem);
                }

                LogManager.Instance.DeactivateLogger();
                foreach (var logMessageItem in selectedItems) logMessageItem.Parent.Enabled = true;
            }

            logListView.EndUpdate();
        }

        private void ClearAll()
        {
            ClearLogMessages();
            ClearLoggers();
        }

        protected void ShowBalloonTip(string msg)
        {
            appNotifyIcon.BalloonTipTitle = AboutForm.AssemblyTitle;
            appNotifyIcon.BalloonTipText = msg;
            appNotifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            appNotifyIcon.ShowBalloonTip(3000);
        }

        private void ShowErrorBox(string msg)
        {
            MessageBox.Show(this, msg, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowSettingsForm()
        {
            // Make a copy of the settings in case the user cancels.
            var copy = UserSettings.Instance.Clone();
            var form = new SettingsForm(copy);
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            UserSettings.Instance = copy;
            UserSettings.Instance.Save();

            ApplySettings(false);
        }

        private void ShowReceiversForm()
        {
            var form = new ReceiversForm(UserSettings.Instance.Receivers);
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            foreach (var receiver in form.RemovedReceivers)
            {
                TerminateReceiver(receiver);
                UserSettings.Instance.Receivers.Remove(receiver);
            }

            foreach (var receiver in form.AddedReceivers)
            {
                UserSettings.Instance.Receivers.Add(receiver);
                InitializeReceiver(receiver);
            }

            UserSettings.Instance.Save();
        }


        private void ShowAboutForm()
        {
            var aboutBox = new AboutForm();
            aboutBox.ShowDialog(this);
        }

        private void RestoreWindow()
        {
            // Make the form visible and activate it. We need to bring the form
            // the front so the user will see it. Otherwise the user would have
            // to find it in the task bar and click on it.

            Visible = true;
            Activate();
            BringToFront();

            if (WindowState == FormWindowState.Minimized)
                WindowState = _windowRestorer.WindowState;
        }

        /// <summary>
        ///     Adds a new log message, synchronously.
        /// </summary>
        private void AddLogMessages(IEnumerable<LogMessage> logMsgs)
        {
            if (_pauseLog)
                return;

            logListView.BeginUpdate();

            foreach (var msg in logMsgs)
                AddLogMessage(msg);

            logListView.EndUpdate();
        }

        /// <summary>
        ///     Adds a new log message, synchronously.
        /// </summary>
        private void AddLogMessage(LogMessage logMsg)
        {
            try
            {
                if (_pauseLog)
                    return;

                RemovedLogMsgsHighlight();

                _addedLogMessage = true;

                LogManager.Instance.ProcessLogMessage(logMsg);

                if (!Visible && UserSettings.Instance.NotifyNewLogWhenHidden)
                    ShowBalloonTip("A new message has been received...");
            }
            catch
            {
                // ignored
            }
        }


        private void OnLogMessageTimer(object sender)
        {
            LogMessage[] messages;

            lock (_eventQueue)
            {
                // Do a local copy to minimize the lock
                messages = _eventQueue.ToArray();
                _eventQueue.Clear();
            }

            // Process logs if any
            if (messages.Length > 0)
            {
                // InvokeRequired required compares the thread ID of the
                // calling thread to the thread ID of the creating thread.
                // If these threads are different, it returns true.
                if (logListView.InvokeRequired)
                {
                    NotifyLogMsgsCallback d = AddLogMessages;
                    Invoke(d, new object[] {messages});
                }
                else
                {
                    AddLogMessages(messages);
                }
            }
        }


        private void OnTaskbarProgressTimer(object o)
        {
            if (_isWin7orLater)
                TaskbarManager.Instance.SetProgressState(_addedLogMessage
                    ? TaskbarProgressBarState.Indeterminate
                    : TaskbarProgressBarState.NoProgress);
            _addedLogMessage = false;
        }

        private void quitBtn_Click(object sender, EventArgs e)
        {
            try
            {
                Quit();
            }
            catch
            {
                Environment.Exit(0);
            }
        }

        private void logListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            RemovedLoggerHighlight();

            LogMessageItem logMsgItem = null;
            if (logListView.SelectedItems.Count > 0)
                logMsgItem = logListView.SelectedItems[0].Tag as LogMessageItem;

            SetLogMessageDetail(logMsgItem);

            // Highlight Logger in the Tree View
            if (logMsgItem != null && UserSettings.Instance.HighlightLogger)
            {
                logMsgItem.Parent.Highlight = true;
                _lastHighlightedLogger = logMsgItem.Parent;
            }
        }

        private void SetLogMessageDetail(LogMessageItem logMsgItem)
        {
            // Store the text to avoid editing without settings the control
            // as readonly... kind of ugly trick...

            if (logMsgItem == null)
            {
                logDetailTextBox.Text = string.Empty;
                PopulateExceptions(null);
                OpenSourceFile(null, 0);
            }
            else
            {
                var sb = new StringBuilder();

                sb.Append(logMsgItem.GetMessageDetails());

                if (UserSettings.Instance.ShowMsgDetailsProperties)
                    foreach (var kvp in logMsgItem.Message.Properties)
                        sb.AppendFormat("{0} = {1}{2}", kvp.Key, kvp.Value, Environment.NewLine);


                // Append exception
                tbExceptions.Text = string.Empty;
                if (UserSettings.Instance.ShowMsgDetailsException &&
                    !string.IsNullOrEmpty(logMsgItem.Message.ExceptionString))
                    if (!string.IsNullOrEmpty(logMsgItem.Message.ExceptionString))
                        PopulateExceptions(logMsgItem.Message.ExceptionString);


                logDetailTextBox.ForeColor = logMsgItem.Message.Level.Color;
                logDetailTextBox.Rtf = sb.ToString();
                //TODO: logDetailTextBox.Text = sb.ToString();

                OpenSourceFile(logMsgItem.Message.SourceFileName, logMsgItem.Message.SourceFileLineNr);
            }
        }

        private void OpenSourceFile(string fileName, uint line)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                textEditorSourceCode.Visible = false;
                lbFileName.Text = string.Empty;
                return;
            }

            textEditorSourceCode.Visible = true;
            try
            {
                if (!File.Exists(fileName))
                {
                    //If the file cannot be found, try to locate it using the source code mapping configuration
                    var mappedFile = TryToLocateSourceFile(fileName);
                    if (string.IsNullOrEmpty(mappedFile))
                    {
                        textEditorSourceCode.Visible = false;
                        lbFileName.Text = fileName + " not found...";
                        return;
                    }

                    if (!File.Exists(mappedFile))
                    {
                        textEditorSourceCode.Visible = false;
                        lbFileName.Text = mappedFile + " not found...";
                        return;
                    }

                    fileName = mappedFile;
                }

                if (line > 1)
                    line--;
                textEditorSourceCode.LoadFile(fileName);
                textEditorSourceCode.ActiveTextAreaControl.TextArea.Caret.Line = (int) line;
                textEditorSourceCode.ActiveTextAreaControl.TextArea.Caret.UpdateCaretPosition();
                lbFileName.Text = fileName + ":" + line;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Message: {ex.Message}, Stack Trace: {ex.StackTrace}",
                    "Error opening source file");
            }
        }

        private string TryToLocateSourceFile(string file)
        {
            if (UserSettings.Instance.SourceLocationMapConfiguration != null)
                foreach (var sourceMap in UserSettings.Instance.SourceLocationMapConfiguration)
                    if (file.StartsWith(sourceMap.LogSource))
                    {
                        file = sourceMap.LocalSource + file.Remove(0, sourceMap.LogSource.Length);
                        return file;
                    }

            return null;
        }

        private void PopulateExceptions(string exceptions)
        {
            if (string.IsNullOrEmpty(exceptions))
            {
                tbExceptions.Text = string.Empty;
                return;
            }

            var lines = exceptions.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (!ParseCSharpStackTraceLine(line)) tbExceptions.SelectedText = line;
                //else if (Add other Parsers Here...)

                tbExceptions.SelectedText = "\r\n";
            }
        }

        private bool ParseCSharpStackTraceLine(string line)
        {
            var stackTraceFileDetected = false;

            //Detect a C Sharp File                
            var endOfFileIndex = line.ToLower().LastIndexOf(".cs", StringComparison.Ordinal);
            if (endOfFileIndex != -1)
            {
                var leftTruncatedFile = line.Substring(0, endOfFileIndex + 3);
                var startOfFileIndex = leftTruncatedFile.LastIndexOf(":", StringComparison.Ordinal) - 1;
                if (startOfFileIndex >= 0)
                {
                    var fileName =
                        leftTruncatedFile.Substring(startOfFileIndex, leftTruncatedFile.Length - startOfFileIndex);

                    const string lineSignature = ":line ";
                    var lineIndex = line.ToLower().LastIndexOf(lineSignature, StringComparison.Ordinal);
                    if (lineIndex != -1)
                    {
                        var lineSignatureLength = lineSignature.Length;
                        var lineNrString = line.Substring(lineIndex + lineSignatureLength,
                            line.Length - lineIndex - lineSignatureLength);
                        lineNrString = lineNrString.TrimEnd(',');
                        if (!string.IsNullOrEmpty(lineNrString))
                            if (uint.TryParse(lineNrString, out var parsedLineNr))
                            {
                                var fileLine = (int) parsedLineNr;
                                stackTraceFileDetected = true;

                                tbExceptions.SelectedText = line.Substring(0, startOfFileIndex - 1) + " ";
                                tbExceptions.InsertLink($"{fileName} line:{fileLine}");
                            }
                    }
                }
            }

            return stackTraceFileDetected;
        }


        private void clearBtn_Click(object sender, EventArgs e)
        {
            ClearLogMessages();
        }

        private void closeLoggersPanelBtn_Click(object sender, EventArgs e)
        {
            ShowLoggersPanel(false);
        }

        private void loggersPanelToggleBtn_Click(object sender, EventArgs e)
        {
            // Toggle check state
            ShowLoggersPanel(!loggersPanelToggleBtn.Checked);
        }

        private void ShowLoggersPanel(bool show)
        {
            loggersPanelToggleBtn.Checked = show;

            if (show)
                _dockExtender.Show(loggerPanel);
            else
                _dockExtender.Hide(loggerPanel);
        }

        private void clearLoggersBtn_Click(object sender, EventArgs e)
        {
            ClearLoggers();
        }

        private void collapseAllBtn_Click(object sender, EventArgs e)
        {
            CollapseLoggers();
        }

        private void deactivatedsourcesBtn_Click(object sender, EventArgs e)
        {
            DeactivateSelectedLoggers();
        }

        private void deactivatedUnselectSourcesBtn_Click(object sender, EventArgs e)
        {
            DeactivateNotSelectedLoggers();
        }

        private void closeLogDetailPanelBtn_Click(object sender, EventArgs e)
        {
            ShowDetailsPanel(false);
        }

        private void logDetailsPanelToggleBtn_Click(object sender, EventArgs e)
        {
            // Toggle check state
            ShowDetailsPanel(!logDetailsPanelToggleBtn.Checked);
        }

        private void ShowDetailsPanel(bool show)
        {
            logDetailsPanelToggleBtn.Checked = show;

            if (show)
                _dockExtender.Show(logDetailPanel);
            else
                _dockExtender.Hide(logDetailPanel);
        }

        private void copyLogDetailBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(logDetailTextBox.Text))
                return;

            Clipboard.SetDataObject(logDetailTextBox.Text, false, 5, 200);
        }

        private void aboutBtn_Click(object sender, EventArgs e)
        {
            ShowAboutForm();
        }

        private void settingsBtn_Click(object sender, EventArgs e)
        {
            ShowSettingsForm();
        }

        private void receiversBtn_Click(object sender, EventArgs e)
        {
            ShowReceiversForm();
        }

        private void appNotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            RestoreWindow();
        }

        private void OnMinimized(object sender, EventArgs e)
        {
            if (!ShowInTaskbar)
                Visible = false;
        }

        private void restoreTrayMenuItem_Click(object sender, EventArgs e)
        {
            RestoreWindow();
        }

        private void settingsTrayMenuItem_Click(object sender, EventArgs e)
        {
            ShowSettingsForm();
        }

        private void aboutTrayMenuItem_Click(object sender, EventArgs e)
        {
            ShowAboutForm();
        }

        private void exitTrayMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Quit();
            }
            catch
            {
                Environment.Exit(0);
            }
        }

        private void searchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Return || e.Alt || e.Control)
                return;
            using (new AutoWaitCursor())
            {
                LogManager.Instance.SearchText(searchTextBox.Text);
            }
        }

        private void zoomOutLogListBtn_Click(object sender, EventArgs e)
        {
            ZoomControlFont(logListView, false);
        }

        private void zoomInLogListBtn_Click(object sender, EventArgs e)
        {
            ZoomControlFont(logListView, true);
        }

        private void zoomOutLogDetailsBtn_Click(object sender, EventArgs e)
        {
            ZoomControlFont(logDetailTextBox, false);
        }

        private void zoomInLogDetailsBtn_Click(object sender, EventArgs e)
        {
            ZoomControlFont(logDetailTextBox, true);
        }

        private void pinOnTopBtn_Click(object sender, EventArgs e)
        {
            // Toggle check state
            pinOnTopBtn.Checked = !pinOnTopBtn.Checked;

            // Save and apply setting
            UserSettings.Instance.AlwaysOnTop = pinOnTopBtn.Checked;
            TopMost = pinOnTopBtn.Checked;
        }

        private static void ZoomControlFont(Control ctrl, bool zoomIn)
        {
            // Limit to a minimum size
            var newSize = Math.Max(0.5f, ctrl.Font.SizeInPoints + (zoomIn ? +1 : -1));
            ctrl.Font = new Font(ctrl.Font.FontFamily, newSize);
        }


        private void deleteLoggerTreeMenuItem_Click(object sender, EventArgs e)
        {
            var logger = (LoggerItem) loggerTreeView.SelectedNode.Tag;

            logger?.Remove();
        }

        private void deleteAllLoggerTreeMenuItem_Click(object sender, EventArgs e)
        {
            ClearAll();
        }

        private void loggerTreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Select the clicked node
                loggerTreeView.SelectedNode = loggerTreeView.GetNodeAt(e.X, e.Y);

                deleteLoggerTreeMenuItem.Enabled = loggerTreeView.SelectedNode != null;

                loggerTreeContextMenuStrip.Show(loggerTreeView, e.Location);
            }
        }

        private void loggerTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // If we are suppose to ignore events right now, then just return.
            if (_ignoreEvents)
                return;

            // Set a flag to ignore future events while processing this event. We have
            // to do this because it may be possbile that this event gets fired again
            // during a recursive call. To avoid more processing than necessary, we should
            // set a flag and clear it when we're done.
            _ignoreEvents = true;

            using (new AutoWaitCursor())
            {
                try
                {
                    // Enable/disable the logger item that is represented by the checked node.
                    ((LoggerItem) e.Node.Tag).Enabled = e.Node.Checked;
                }
                finally
                {
                    _ignoreEvents = false;
                }
            }
        }

        private void levelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsHandleCreated)
                return;

            using (new AutoWaitCursor())
            {
                UserSettings.Instance.LogLevelInfo =
                    LogUtils.GetLogLevelInfo((LogLevel) levelComboBox.SelectedIndex);
                LogManager.Instance.UpdateLogLevel();
            }
        }

        private void loggerTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (!(e.Node?.Tag is LoggerItem))
                return;

            if (UserSettings.Instance.HighlightLogMessages)
            {
                _lastHighlightedLogMsgs = (LoggerItem) e.Node.Tag;
                _lastHighlightedLogMsgs.HighlightLogMessages = true;
            }
        }

        private void loggerTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            RemovedLogMsgsHighlight();
        }

        private void RemovedLogMsgsHighlight()
        {
            if (_lastHighlightedLogMsgs != null)
            {
                _lastHighlightedLogMsgs.HighlightLogMessages = false;
                _lastHighlightedLogMsgs = null;
            }
        }

        private void RemovedLoggerHighlight()
        {
            if (_lastHighlightedLogger != null)
            {
                _lastHighlightedLogger.Highlight = false;
                _lastHighlightedLogger = null;
            }
        }

        private void pauseBtn_Click(object sender, EventArgs e)
        {
            _pauseLog = !_pauseLog;

            pauseBtn.Image = _pauseLog ? Resources.Go16 : Resources.Pause16;
            pauseBtn.Checked = _pauseLog;

            if (_isWin7orLater)
            {
                _pauseWinbarBtn.Icon = Icon.FromHandle(((Bitmap) pauseBtn.Image).GetHicon());

                TaskbarManager.Instance.SetOverlayIcon(
                    _pauseLog ? Icon.FromHandle(Resources.Pause16.GetHicon()) : null, string.Empty);
            }
        }

        private void goToFirstLogBtn_Click(object sender, EventArgs e)
        {
            if (logListView.Items.Count == 0)
                return;

            logListView.Items[0].EnsureVisible();
        }

        private void goToLastLogBtn_Click(object sender, EventArgs e)
        {
            if (logListView.Items.Count == 0)
                return;

            logListView.Items[logListView.Items.Count - 1].EnsureVisible();
        }

        private void autoLogToggleBtn_Click(object sender, EventArgs e)
        {
            UserSettings.Instance.AutoScrollToLastLog = !UserSettings.Instance.AutoScrollToLastLog;

            autoLogToggleBtn.Checked = UserSettings.Instance.AutoScrollToLastLog;
        }

        private void clearAll_Click(object sender, EventArgs e)
        {
            ClearAll();
        }


        /// <summary>
        ///     Quick and dirty implementation of an export function...
        /// </summary>
        private void saveBtn_Click(object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "csv files (*.csv)|*.csv",
                FileName = "logs",
                Title = "Export to Excel"
            };
            if (dlg.ShowDialog(this) == DialogResult.Cancel)
                return;

            utils.Export2Excel(logListView, dlg.FileName);
        }


        private void btnOpenFileInVS_Click(object sender, EventArgs e)
        {
            try
            {
                var processInfo = new ProcessStartInfo("devenv",
                    $"/edit \"{textEditorSourceCode.FileName}\" /command \"Edit.Goto {0}\"");
                var process = Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error opening file in Visual Studio");
            }
        }

        private void TbExceptionsLinkClicked(object sender, LinkClickedEventArgs e)
        {
            var exception = e.LinkText;
            if (exception != null)
            {
                var exceptionPair = exception.Split(new[] {" line:"}, StringSplitOptions.None);
                if (exceptionPair.Length == 2)
                {
                    int.TryParse(exceptionPair[1], out var lineNr);

                    OpenSourceFile(exceptionPair[0], (uint) lineNr);
                    tabControlDetail.SelectedTab = tabSource;
                }
            }
        }

        private void quickLoadBtn_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (!File.Exists(openFileDialog1.FileName))
                {
                    MessageBox.Show($"File: {openFileDialog1.FileName} does not exists",
                        "Error Opening Log File");
                    return;
                }

                var fileReceivers = new List<IReceiver>();
                foreach (var receiver in UserSettings.Instance.Receivers)
                    if (receiver is CsvFileReceiver)
                        fileReceivers.Add(receiver);

                var form = new ReceiversForm(fileReceivers, true);
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                foreach (var receiver in form.AddedReceivers)
                {
                    UserSettings.Instance.Receivers.Add(receiver);
                    InitializeReceiver(receiver);
                }

                UserSettings.Instance.Save();

                var fileReceiver = form.SelectedReceiver as CsvFileReceiver;
                if (fileReceiver == null)
                    return;

                fileReceiver.ShowFromBeginning = true;
                fileReceiver.FileToWatch = openFileDialog1.FileName;
                fileReceiver.Attach(this);

                /*
            var fileReceiver = new CsvFileReceiver();

            fileReceiver.FileToWatch = openFileDialog1.FileName;
            fileReceiver.ReadHeaderFromFile = true;
            fileReceiver.ShowFromBeginning = true;
    
            fileReceiver.Initialize();
            fileReceiver.Attach(this);
            */
            }
        }

        private void logListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete) DeactivateSelectedLoggers();

            if (e.Control && e.KeyCode == Keys.C)
                CopySelectedValuesToClipboard();
        }

        private void CopySelectedValuesToClipboard()
        {
            var builder = new StringBuilder();
            foreach (ListViewItem item in logListView.SelectedItems)
            {
                if (item.Tag is LogMessageItem logMsgItem)
                    builder.AppendLine(logMsgItem.Message.ToString());
            }

            Clipboard.SetDataObject(builder.ToString(), false, 5, 200);
        }

        private delegate void NotifyLogMsgCallback(LogMessage logMsg);

        private delegate void NotifyLogMsgsCallback(LogMessage[] logMsgs);
    }
}