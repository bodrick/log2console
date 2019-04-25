using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Log2Console.Log;
using Log2Console.Properties;
using Log2Console.Receiver;
using Log2Console.Settings;
using Log2Console.UI;
using Log2Console.UI.ControlExtenders;
using log4net.Config;
using Microsoft.WindowsAPICodePack.Taskbar;
using Timer = System.Threading.Timer;

// Configure log4net using the .config file
[assembly: XmlConfigurator(Watch = true)]

namespace Log2Console
{
    public partial class MainForm : Form, ILogMessageNotifiable
    {
        private const int TaskbarProgressTimerPeriod = 2000;
        private const int WM_SIZE = 0x0005;
        private const int SIZE_MINIMIZED = 1;

        private static readonly Regex Stacktrace = new Regex(@"([\S]+\.[\w\d]{1,3}):(line|строка) (\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly UserSettings _configuration;
        private readonly DockExtender _dockExtender;
        private readonly Queue<LogMessage> _eventQueue;
        private readonly bool _isWin7OrLater;
        private readonly ThumbnailToolBarButton _pauseWinbarBtn;
        private readonly WindowRestorer _windowRestorer;
        private bool _addedLogMessage;
        private bool _ignoreEvents;
        private LoggerItem _lastHighlightedLogger;
        private LoggerItem _lastHighlightedLogMsgs;
        private Timer _logMsgTimer;
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
            var logDetailsPanelFloaty = _dockExtender.Attach(logDetailPanel, logDetailToolStrip, logDetailSplitter);
            logDetailsPanelFloaty.HideHandle = false;
            logDetailsPanelFloaty.Docking += OnFloatyDocking;

            // Dockable Logger Tree
            var loggersPanelFloaty = _dockExtender.Attach(loggerPanel, loggersToolStrip, loggerSplitter);
            loggersPanelFloaty.HideHandle = false;
            loggersPanelFloaty.Docking += OnFloatyDocking;

            // Settings
            _configuration = UserSettings.Instance;
            if (_configuration.FirstStartup)
            {
                // Initialize default layout
                _configuration.Layout.Set(DesktopBounds, WindowState, logDetailPanel, loggerPanel);

                // Force panel to visible
                _configuration.Layout.ShowLogDetailView = true;
                _configuration.Layout.ShowLoggerTree = true;
                _configuration.DefaultFont = Environment.OSVersion.Version.Major >= 6
                    ? new Font("Segoe UI", 9F)
                    : new Font("Tahoma", 8.25F);
            }

            Font = _configuration.DefaultFont ?? Font;

            _windowRestorer = new WindowRestorer(this, _configuration.Layout.WindowPosition,
                _configuration.Layout.WindowState);

            // Windows 7 CodePack (Taskbar icons and progress)
            _isWin7OrLater = TaskbarManager.IsPlatformSupported;

            if (_isWin7OrLater)
            {
                try
                {
                    // Taskbar Progress
                    TaskbarManager.Instance.ApplicationId = Text;
                    _taskbarProgressTimer = new Timer(OnTaskbarProgressTimer, null, TaskbarProgressTimerPeriod,
                        TaskbarProgressTimerPeriod);

                    // Pause Btn
                    _pauseWinbarBtn = new ThumbnailToolBarButton(Icon.FromHandle(((Bitmap)pauseBtn.Image).GetHicon()),
                        pauseBtn.ToolTipText);
                    _pauseWinbarBtn.Click += PauseBtn_Click;

                    // Auto Scroll Btn
                    var autoScrollWinbarBtn = new ThumbnailToolBarButton(
                        Icon.FromHandle(((Bitmap)autoLogToggleBtn.Image).GetHicon()),
                        autoLogToggleBtn.ToolTipText);
                    autoScrollWinbarBtn.Click += AutoLogToggleBtn_Click;

                    // Clear All Btn
                    var clearAllWinbarBtn = new ThumbnailToolBarButton(
                        Icon.FromHandle(((Bitmap)clearLoggersBtn.Image).GetHicon()),
                        clearLoggersBtn.ToolTipText);
                    clearAllWinbarBtn.Click += ClearAll_Click;

                    // Add Btns
                    TaskbarManager.Instance.ThumbnailToolBars.AddButtons(Handle, _pauseWinbarBtn, autoScrollWinbarBtn,
                        clearAllWinbarBtn);
                }
                catch
                {
                    // Not running on Win 7?
                    _isWin7OrLater = false;
                }
            }

            ApplySettings(true);

            _eventQueue = new Queue<LogMessage>();

            // Initialize Receivers
            foreach (var receiver in _configuration.Receivers)
            {
                InitializeReceiver(receiver);
            }

            // Start the timer to process event logs in batch mode
            _logMsgTimer = new Timer(OnLogMessageTimer, null, 1000, 100);
        }

        // Specific event handler on minimized action
        public event EventHandler Minimized;

        /// <summary>
        ///     Catch on minimize event
        ///     @author : Asbjorn Ulsberg -=|=- asbjornu@hotmail.com
        /// </summary>
        /// <param name="msg"></param>
        protected override void WndProc(ref Message msg)
        {
            if (msg.Msg == WM_SIZE
                && (int)msg.WParam == SIZE_MINIMIZED)
            {
                Minimized?.Invoke(this, EventArgs.Empty);
            }

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
            if (!_configuration.FirstStartup) return;

            MessageBox.Show(this,
                @"Welcome to Log2Console! You must configure some Receivers in order to use the tool.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

            ShowReceiversForm();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _logMsgTimer?.Dispose();
                _logMsgTimer = null;

                _taskbarProgressTimer?.Dispose();
                _taskbarProgressTimer = null;


                if (_configuration.Layout.LogListViewColumnsWidths == null ||
                    _configuration.Layout.LogListViewColumnsWidths.Length != logListView.Columns.Count)
                {
                    _configuration.Layout.LogListViewColumnsWidths = new int[logListView.Columns.Count];
                }

                for (var i = 0; i < logListView.Columns.Count; i++)
                {
                    _configuration.Layout.LogListViewColumnsWidths[i] = logListView.Columns[i].Width;
                }

                _configuration.Layout.Set(_windowRestorer.WindowPosition, _windowRestorer.WindowState, logDetailPanel,
                    loggerPanel);

                _configuration.Save();
                _configuration.Close();
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
            Opacity = (double)_configuration.Transparency / 100;
            ShowInTaskbar = !_configuration.HideTaskbarIcon;

            TopMost = _configuration.AlwaysOnTop;
            pinOnTopBtn.Checked = _configuration.AlwaysOnTop;
            autoLogToggleBtn.Checked = _configuration.AutoScrollToLastLog;

            logListView.Font = _configuration.LogListFont;
            logDetailTextBox.Font = _configuration.LogDetailFont;
            loggerTreeView.Font = _configuration.LoggerTreeFont;

            logListView.BackColor = _configuration.LogListBackColor;
            logDetailTextBox.BackColor = _configuration.LogMessageBackColor;

            LogLevels.Instance.LogLevelInfos[(int)LogLevel.Trace].Color = _configuration.TraceLevelColor;
            LogLevels.Instance.LogLevelInfos[(int)LogLevel.Debug].Color = _configuration.DebugLevelColor;
            LogLevels.Instance.LogLevelInfos[(int)LogLevel.Info].Color = _configuration.InfoLevelColor;
            LogLevels.Instance.LogLevelInfos[(int)LogLevel.Warn].Color = _configuration.WarnLevelColor;
            LogLevels.Instance.LogLevelInfos[(int)LogLevel.Error].Color = _configuration.ErrorLevelColor;
            LogLevels.Instance.LogLevelInfos[(int)LogLevel.Fatal].Color = _configuration.FatalLevelColor;

            levelComboBox.SelectedIndex = (int)_configuration.LogLevelInfo.Level;

            if (logListView.ShowGroups != _configuration.GroupLogMessages)
            {
                if (noCheck)
                {
                    logListView.ShowGroups = _configuration.GroupLogMessages;
                }
                else
                {
                    var res = MessageBox.Show(this,
                        @"You changed the Message Grouping setting, the Log Message List must be cleared, OK?",
                        Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                    if (res == DialogResult.OK)
                    {
                        ClearAll();
                        logListView.ShowGroups = _configuration.GroupLogMessages;
                    }
                    else
                    {
                        _configuration.GroupLogMessages = !_configuration.GroupLogMessages;
                    }
                }
            }

            //See if the Columns Changed
            var columnsChanged = false;

            if (logListView.Columns.Count != _configuration.ColumnConfiguration.Count)
            {
                columnsChanged = true;
            }
            else
            {
                for (var i = 0; i < _configuration.ColumnConfiguration.Count; i++)
                {
                    if (!_configuration.ColumnConfiguration[i].Name.Equals(logListView.Columns[i].Text))
                    {
                        columnsChanged = true;
                        break;
                    }
                }
            }

            if (columnsChanged)
            {
                logListView.Columns.Clear();
                foreach (var column in _configuration.ColumnConfiguration) logListView.Columns.Add(column.Name);
            }

            // Layout
            if (noCheck)
            {
                DesktopBounds = _configuration.Layout.WindowPosition;
                WindowState = _configuration.Layout.WindowState;

                ShowDetailsPanel(_configuration.Layout.ShowLogDetailView);
                logDetailPanel.Size = _configuration.Layout.LogDetailViewSize;

                ShowLoggersPanel(_configuration.Layout.ShowLoggerTree);
                loggerPanel.Size = _configuration.Layout.LoggerTreeSize;

                if (_configuration.Layout.LogListViewColumnsWidths != null)
                {
                    for (var i = 0; i < _configuration.Layout.LogListViewColumnsWidths.Length; i++)
                    {
                        if (i < logListView.Columns.Count)
                        {
                            logListView.Columns[i].Width = _configuration.Layout.LogListViewColumnsWidths[i];
                        }
                    }
                }
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

        private void Quit() => Close();

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
            {
                foreach (ListViewItem selectedItem in logListView.SelectedItems)
                {
                    if (selectedItem.Tag is LogMessageItem logMsgItem)
                    {
                        logMsgItem.Parent.Enabled = false;
                        _lastHighlightedLogger = null;
                    }
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
                    if (item.Tag is LogMessageItem logMsgItem)
                    {
                        selectedItems.Add(logMsgItem);
                    }
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

        private void ShowErrorBox(string msg) =>
            MessageBox.Show(this, msg, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

        private void ShowSettingsForm()
        {
            var form = new SettingsForm
            {
                Font = _configuration.DefaultFont
            };

            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _configuration.Save();

            ApplySettings(false);
        }

        private void ShowReceiversForm()
        {
            var form = new ReceiversForm(_configuration.Receivers)
            {
                Font = _configuration.DefaultFont
            };

            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            foreach (var receiver in form.RemovedReceivers)
            {
                TerminateReceiver(receiver);
                _configuration.Receivers.Remove(receiver);
            }

            foreach (var receiver in form.AddedReceivers)
            {
                _configuration.Receivers.Add(receiver);
                InitializeReceiver(receiver);
            }

            _configuration.Save();
        }


        private void ShowAboutForm()
        {
            var aboutBox = new AboutForm
            {
                Font = _configuration.DefaultFont, Text = $"About {AboutForm.AssemblyTitle}"
            };
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
            {
                WindowState = _windowRestorer.WindowState;
            }
        }

        /// <summary>
        ///     Adds a new log message, synchronously.
        /// </summary>
        private void AddLogMessages(IEnumerable<LogMessage> logMsgs)
        {
            if (_pauseLog)
            {
                return;
            }

            logListView.BeginUpdate();

            foreach (var msg in logMsgs)
            {
                AddLogMessage(msg);
            }

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
                {
                    return;
                }

                RemovedLogMsgsHighlight();

                _addedLogMessage = true;

                LogManager.Instance.ProcessLogMessage(logMsg);

                if (!Visible && _configuration.NotifyNewLogWhenHidden)
                {
                    ShowBalloonTip("A new message has been received...");
                }
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
                    Invoke(d, new object[]
                    {
                        messages
                    });
                }
                else
                {
                    AddLogMessages(messages);
                }
            }
        }


        private void OnTaskbarProgressTimer(object o)
        {
            if (_isWin7OrLater)
            {
                TaskbarManager.Instance.SetProgressState(_addedLogMessage
                    ? TaskbarProgressBarState.Indeterminate
                    : TaskbarProgressBarState.NoProgress);
            }

            _addedLogMessage = false;
        }

        private void QuitBtn_Click(object sender, EventArgs e)
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

        private void LogListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            RemovedLoggerHighlight();

            LogMessageItem logMsgItem = null;
            if (logListView.SelectedItems.Count > 0)
            {
                logMsgItem = logListView.SelectedItems[0].Tag as LogMessageItem;
            }

            SetLogMessageDetail(logMsgItem);

            // Highlight Logger in the Tree View
            if (logMsgItem != null && _configuration.HighlightLogger)
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

                // Removing trailing '}' char
                sb.Remove(sb.Length - 1, 1);

                if (_configuration.ShowMsgDetailsProperties)
                    // Append properties
                {
                    foreach (var kvp in logMsgItem.Message.Properties)
                    {
                        sb.AppendFormat("{0} = {1}{2}", kvp.Key, kvp.Value, "\\line\n");
                    }
                }

                // Append exception
                tbExceptions.Text = string.Empty;
                if (_configuration.ShowMsgDetailsException &&
                    !string.IsNullOrEmpty(logMsgItem.Message.ExceptionString))
                    //sb.AppendLine(logMsgItem.Message.ExceptionString);            
                {
                    if (!string.IsNullOrEmpty(logMsgItem.Message.ExceptionString))
                    {
                        PopulateExceptions(logMsgItem.Message.ExceptionString);
                    }
                }

                // Closing rtf document
                sb.Append('}');

                var rtf = sb.ToString();

                // Since rtf only support 7-bit text encoding, we need to escape non-ASCII chars
                rtf = Regex.Replace(rtf, "[^\x00-\x7F]", m => $@"\u{(short)m.Value[0]}{m.Value[0]}");

                logDetailTextBox.ForeColor = logMsgItem.Message.Level.Color;
                if (_configuration.UseMsgDetailsRtf)
                {
                    logDetailTextBox.Rtf = rtf;
                }
                else
                {
                    logDetailTextBox.Text = sb.ToString();
                }

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
                {
                    line--;
                }

                textEditorSourceCode.LoadFile(fileName);
                textEditorSourceCode.ActiveTextAreaControl.TextArea.Caret.Line = (int)line;
                textEditorSourceCode.ActiveTextAreaControl.TextArea.Caret.UpdateCaretPosition();
                lbFileName.Text = fileName + ":" + line;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Message: {ex.Message}, Stack Trace: {ex.StackTrace}", "Error opening source file");
            }
        }

        private string TryToLocateSourceFile(string file)
        {
            if (_configuration.SourceLocationMapConfiguration == null) return null;

            foreach (var sourceMap in _configuration.SourceLocationMapConfiguration)
            {
                if (file.StartsWith(sourceMap.LogSource))
                {
                    file = sourceMap.LocalSource + file.Remove(0, sourceMap.LogSource.Length);
                    return file;
                }
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

            var lines = exceptions.Split(new[]
            {
                "\r\n", "\n"
            }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (!ParseCSharpStackTraceLine(line))
                    //No supported exception stack traces is detected
                {
                    tbExceptions.SelectedText = line;
                }
                //else if (Add other Parsers Here...)

                tbExceptions.SelectedText = "\r\n";
            }
        }

        private bool ParseCSharpStackTraceLine(string line)
        {
            var stackTraceFileDetected = false;

            var match = Stacktrace.Match(line);
            if (match.Success)
            {
                var fileName = match.Groups[1].Value;
                var fileLine = match.Groups[3].Value;

                stackTraceFileDetected = true;

                tbExceptions.SelectedText = line.Substring(0, match.Groups[0].Index - 1) + " ";
                tbExceptions.InsertLink($"{fileName} line:{fileLine}");
            }

            return stackTraceFileDetected;
        }

        private void ClearBtn_Click(object sender, EventArgs e) => ClearLogMessages();

        private void CloseLoggersPanelBtn_Click(object sender, EventArgs e) => ShowLoggersPanel(false);

        private void LoggersPanelToggleBtn_Click(object sender, EventArgs e) =>
            ShowLoggersPanel(!loggersPanelToggleBtn.Checked);

        private void ShowLoggersPanel(bool show)
        {
            loggersPanelToggleBtn.Checked = show;

            if (show)
            {
                _dockExtender.Show(loggerPanel);
            }
            else
            {
                _dockExtender.Hide(loggerPanel);
            }
        }

        private void ClearLoggersBtn_Click(object sender, EventArgs e) => ClearLoggers();

        private void CollapseAllBtn_Click(object sender, EventArgs e) => CollapseLoggers();

        private void DeactivatedSourcesBtn_Click(object sender, EventArgs e) => DeactivateSelectedLoggers();

        private void DeactivatedUnselectSourcesBtn_Click(object sender, EventArgs e) => DeactivateNotSelectedLoggers();

        private void CloseLogDetailPanelBtn_Click(object sender, EventArgs e) => ShowDetailsPanel(false);

        private void LogDetailsPanelToggleBtn_Click(object sender, EventArgs e) =>
            ShowDetailsPanel(!logDetailsPanelToggleBtn.Checked);

        private void ShowDetailsPanel(bool show)
        {
            logDetailsPanelToggleBtn.Checked = show;

            if (show)
            {
                _dockExtender.Show(logDetailPanel);
            }
            else
            {
                _dockExtender.Hide(logDetailPanel);
            }
        }

        private void CopyLogDetailBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(logDetailTextBox.Text))
            {
                return;
            }

            Clipboard.SetDataObject(logDetailTextBox.Text, false, 5, 200);
        }

        private void AboutBtn_Click(object sender, EventArgs e) => ShowAboutForm();

        private void SettingsBtn_Click(object sender, EventArgs e) => ShowSettingsForm();

        private void ReceiversBtn_Click(object sender, EventArgs e) => ShowReceiversForm();

        private void AppNotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e) => RestoreWindow();

        private void OnMinimized(object sender, EventArgs e)
        {
            if (!ShowInTaskbar)
            {
                Visible = false;
            }
        }

        private void RestoreTrayMenuItem_Click(object sender, EventArgs e) => RestoreWindow();

        private void SettingsTrayMenuItem_Click(object sender, EventArgs e) => ShowSettingsForm();

        private void AboutTrayMenuItem_Click(object sender, EventArgs e) => ShowAboutForm();

        private void ExitTrayMenuItem_Click(object sender, EventArgs e)
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

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Return || e.Alt || e.Control)
            {
                return;
            }

            using (new AutoWaitCursor())
            {
                LogManager.Instance.SearchText(searchTextBox.Text);
            }
        }

        private void ZoomOutLogListBtn_Click(object sender, EventArgs e) => ZoomControlFont(logListView, false);

        private void ZoomInLogListBtn_Click(object sender, EventArgs e) => ZoomControlFont(logListView, true);

        private void ZoomOutLogDetailsBtn_Click(object sender, EventArgs e) => ZoomControlFont(logDetailTextBox, false);

        private void ZoomInLogDetailsBtn_Click(object sender, EventArgs e) => ZoomControlFont(logDetailTextBox, true);

        private void PinOnTopBtn_Click(object sender, EventArgs e)
        {
            // Toggle check state
            pinOnTopBtn.Checked = !pinOnTopBtn.Checked;

            // Save and apply setting
            _configuration.AlwaysOnTop = pinOnTopBtn.Checked;
            TopMost = pinOnTopBtn.Checked;
        }

        private static void ZoomControlFont(Control ctrl, bool zoomIn)
        {
            // Limit to a minimum size
            var newSize = Math.Max(0.5f, ctrl.Font.SizeInPoints + (zoomIn ? +1 : -1));
            ctrl.Font = new Font(ctrl.Font.FontFamily, newSize);
        }

        private void DeleteLoggerTreeMenuItem_Click(object sender, EventArgs e)
        {
            var logger = (LoggerItem)loggerTreeView.SelectedNode.Tag;

            logger?.Remove();
        }

        private void DeleteAllLoggerTreeMenuItem_Click(object sender, EventArgs e) => ClearAll();

        private void LoggerTreeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            // Select the clicked node
            loggerTreeView.SelectedNode = loggerTreeView.GetNodeAt(e.X, e.Y);
            deleteLoggerTreeMenuItem.Enabled = loggerTreeView.SelectedNode != null;
            loggerTreeContextMenuStrip.Show(loggerTreeView, e.Location);
        }

        private void LoggerTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // If we are suppose to ignore events right now, then just return.
            if (_ignoreEvents)
            {
                return;
            }

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
                    ((LoggerItem)e.Node.Tag).Enabled = e.Node.Checked;
                }
                finally
                {
                    _ignoreEvents = false;
                }
            }
        }

        private void LevelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsHandleCreated)
            {
                return;
            }

            using (new AutoWaitCursor())
            {
                _configuration.LogLevelInfo =
                    LogUtils.GetLogLevelInfo((LogLevel)levelComboBox.SelectedIndex);
                LogManager.Instance.UpdateLogLevel();
            }
        }

        private void LoggerTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (!_configuration.HighlightLogMessages) return;

            _lastHighlightedLogMsgs = e.Node.Tag as LoggerItem;
            if (_lastHighlightedLogMsgs != null) _lastHighlightedLogMsgs.HighlightLogMessages = true;
        }

        private void LoggerTreeView_AfterSelect(object sender, TreeViewEventArgs e) => RemovedLogMsgsHighlight();

        private void RemovedLogMsgsHighlight()
        {
            if (_lastHighlightedLogMsgs == null) return;

            _lastHighlightedLogMsgs.HighlightLogMessages = false;
            _lastHighlightedLogMsgs = null;
        }

        private void RemovedLoggerHighlight()
        {
            if (_lastHighlightedLogger == null) return;

            _lastHighlightedLogger.Highlight = false;
            _lastHighlightedLogger = null;
        }

        private void PauseBtn_Click(object sender, EventArgs e)
        {
            _pauseLog = !_pauseLog;

            pauseBtn.Image = _pauseLog ? Resources.Go16 : Resources.Pause16;
            pauseBtn.Checked = _pauseLog;

            if (!_isWin7OrLater) return;

            _pauseWinbarBtn.Icon = Icon.FromHandle(((Bitmap)pauseBtn.Image).GetHicon());

            TaskbarManager.Instance.SetOverlayIcon(_pauseLog ? Icon.FromHandle(Resources.Pause16.GetHicon()) : null,
                string.Empty);
        }

        private void GoToFirstLogBtn_Click(object sender, EventArgs e)
        {
            if (logListView.Items.Count == 0)
            {
                return;
            }

            logListView.Items[0].EnsureVisible();
        }

        private void GoToLastLogBtn_Click(object sender, EventArgs e)
        {
            if (logListView.Items.Count == 0)
            {
                return;
            }

            logListView.Items[logListView.Items.Count - 1].EnsureVisible();
        }

        private void AutoLogToggleBtn_Click(object sender, EventArgs e)
        {
            _configuration.AutoScrollToLastLog = !_configuration.AutoScrollToLastLog;

            autoLogToggleBtn.Checked = _configuration.AutoScrollToLastLog;
        }

        private void ClearAll_Click(object sender, EventArgs e) => ClearAll();

        /// <summary>
        ///     Quick and dirty implementation of an export function...
        /// </summary>
        private void SaveBtn_Click(object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "csv files (*.csv)|*.csv", FileName = "logs", Title = "Export to Excel"
            };
            if (dlg.ShowDialog(this) == DialogResult.Cancel)
            {
                return;
            }

            Utils.Export2Excel(logListView, dlg.FileName);
        }

        private void BtnOpenFileInVS_Click(object sender, EventArgs e)
        {
            try
            {
                var processInfo = new ProcessStartInfo("devenv",
                    $"/edit \"{textEditorSourceCode.FileName}\" /command \"Edit.Goto {0}\"");
                Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error opening file in Visual Studio");
            }
        }

        private void TbExceptionsLinkClicked(object sender, LinkClickedEventArgs e)
        {
            var exception = e.LinkText;
            if (exception == null) return;

            var exceptionPair = exception.Split(new[]
            {
                " line:"
            }, StringSplitOptions.None);
            if (exceptionPair.Length == 2)
            {
                int.TryParse(exceptionPair[1], out var lineNr);

                OpenSourceFile(exceptionPair[0], (uint)lineNr);
                tabControlDetail.SelectedTab = tabSource;
            }
        }

        private void QuickLoadBtn_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            if (!File.Exists(openFileDialog1.FileName))
            {
                MessageBox.Show($"File: {openFileDialog1.FileName} does not exists", "Error Opening Log File");
                return;
            }

            var fileReceivers = new List<IReceiver>();
            foreach (var receiver in _configuration.Receivers)
            {
                if (receiver is CsvFileReceiver)
                {
                    fileReceivers.Add(receiver);
                }
            }

            var form = new ReceiversForm(fileReceivers, true)
            {
                Font = _configuration.DefaultFont
            };

            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            foreach (var receiver in form.AddedReceivers)
            {
                _configuration.Receivers.Add(receiver);
                InitializeReceiver(receiver);
            }

            _configuration.Save();

            if (!(form.SelectedReceiver is CsvFileReceiver fileReceiver))
            {
                return;
            }

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

        private void LogListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete) DeactivateSelectedLoggers();

            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedValuesToClipboard();
            }
        }

        private void CopySelectedValuesToClipboard()
        {
            var builder = new StringBuilder();
            foreach (ListViewItem item in logListView.SelectedItems)
            {
                if (item.Tag is LogMessageItem logMsgItem)
                {
                    builder.AppendLine(logMsgItem.Message.ToString());
                }
            }

            Clipboard.SetDataObject(builder.ToString(), false, 5, 200);
        }

        private delegate void NotifyLogMsgCallback(LogMessage logMsg);

        private delegate void NotifyLogMsgsCallback(LogMessage[] logMsgs);

        #region ILogMessageNotifiable Members

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

        #endregion
    }
}