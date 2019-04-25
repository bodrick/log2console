using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Log2Console.Settings;

namespace Log2Console.Log
{
    /// <summary>
    ///     Describes a Logger.
    /// </summary>
    public class LoggerItem
    {
        private const char LoggerSeparator = '.';

        /// <summary>
        ///     When set the Logger and its Messages are displayed.
        /// </summary>
        private bool _enabled = true;

        private bool _hasSearchedText;

        private bool _highlight;
        private bool _highlightLogMessages;

        /// <summary>
        ///     A reference to the Log ListView associated to this Logger.
        /// </summary>
        private ListView _logListView;

        /// <summary>
        ///     Short Name of this Logger (used as the node name).
        /// </summary>
        private string _name = string.Empty;

        private string _searchedText;

        /// <summary>
        ///     Full Name (or "Path") of this Logger.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        ///     The optional associated List View Group.
        /// </summary>
        public ListViewGroup Group { get; set; }

        /// <summary>
        ///     Collection of child Logger Items, identified by their full path.
        /// </summary>
        public Dictionary<string, LoggerItem> Loggers { get; set; } = new Dictionary<string, LoggerItem>();

        /// <summary>
        ///     The associated Tree Node.
        /// </summary>
        public ILoggerView LoggerView { get; set; }

        /// <summary>
        ///     Collection of Log Messages for this Logger.
        /// </summary>
        public List<LogMessageItem> LogMessages { get; set; } = new List<LogMessageItem>();

        /// <summary>
        ///     Parent Logger Item. Null for the Root.
        /// </summary>
        public LoggerItem Parent { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;

                if (LoggerView != null)
                {
                    LoggerView.Text = _name;
                }
            }
        }

        internal bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;
                LoggerView.Enabled = value;

                // Now enable all child loggers if the settings are set to 
                // recursively enable/disable child loggers.
                if (UserSettings.Instance.RecursivelyEnableLoggers)
                {
                    foreach (var child in Loggers.Values)
                    {
                        child.Enabled = value;
                    }
                }

                if (LogMessages.Count == 0)
                {
                    return;
                }

                // Update List View
                _logListView.BeginUpdate();

                foreach (var item in LogMessages)
                {
                    EnableLogMessage(item, _enabled && IsItemToBeEnabled(item));
                }

                _logListView.EndUpdate();
            }
        }


        internal bool Highlight
        {
            get => _highlight;
            set
            {
                // Don't allow highlight for the root item
                if (_highlight == value || Parent == null)
                {
                    return;
                }

                _highlight = value;
                LoggerView.Highlight = value;
                Parent.Highlight = value;
            }
        }


        internal bool HighlightLogMessages
        {
            get => _highlightLogMessages;
            set
            {
                // Don't allow highlight for the root item
                if (Parent == null)
                {
                    return;
                }

                _highlightLogMessages = value;


                _logListView.BeginUpdate();

                foreach (var item in LogMessages)
                {
                    item.Highlight(value);
                }

                _logListView.EndUpdate();
            }
        }

        public static LoggerItem CreateRootLoggerItem(string name, ILoggerView loggerView, ListView logListView)
        {
            var logger = new LoggerItem
            {
                Name = name, _logListView = logListView
            };

            // Tree Node
            logger.LoggerView = loggerView.AddNew(name, logger);

            return logger;
        }

        private static LoggerItem CreateLoggerItem(string name, string fullName, LoggerItem parent)
        {
            // Creating the logger item.
            var logger = new LoggerItem
            {
                Name = name, FullName = fullName, Parent = parent ?? throw new ArgumentNullException()
            };

            logger._logListView = logger.Parent._logListView;

            // Adding the logger as a child of the parent logger.
            logger.Parent.Loggers.Add(name, logger);

            // Creating a child logger view and saving it in the new logger.
            logger.LoggerView = parent.LoggerView.AddNew(name, logger);

            if (UserSettings.Instance.RecursivelyEnableLoggers)
            {
                logger._enabled = parent.Enabled;
                logger.LoggerView.Enabled = parent.LoggerView.Enabled;
            }

            // Group
            if (UserSettings.Instance.GroupLogMessages)
            {
                logger.Group = new ListViewGroup(fullName);
                logger._logListView.Groups.Add(logger.Group);
            }

            return logger;
        }

        public void Remove()
        {
            if (Parent == null)
            {
                // If root, clear all
                ClearAll();
                return;
            }

            ClearLogMessages();

            Parent.Loggers.Remove(Name);
            Parent.LoggerView.Remove(Name);
        }

        public void ClearAll()
        {
            ClearAllLogMessages();

            foreach (var kvp in Loggers)
            {
                kvp.Value.ClearAll();
            }

            LoggerView.Clear();
            Loggers.Clear();
        }

        public void ClearAllLogMessages()
        {
            _logListView.BeginUpdate();

            Group?.Items.Clear();
            _logListView.Items.Clear();
            LogMessages.Clear();

            foreach (var kvp in Loggers)
            {
                kvp.Value.ClearLogMessages();
            }

            _logListView.EndUpdate();
        }

        public void ClearLogMessages()
        {
            _logListView.BeginUpdate();

            Group?.Items.Clear();

            foreach (var item in LogMessages)
            {
                _logListView.Items.Remove(item.Item);
            }

            LogMessages.Clear();

            foreach (var kvp in Loggers)
            {
                kvp.Value.ClearLogMessages();
            }

            _logListView.EndUpdate();
        }


        internal void UpdateLogLevel()
        {
            _logListView.BeginUpdate();

            foreach (var item in LogMessages)
            {
                EnableLogMessage(item, IsItemToBeEnabled(item));
            }

            foreach (var kvp in Loggers)
            {
                kvp.Value.UpdateLogLevel();
            }

            _logListView.EndUpdate();
        }

        private void EnableLogMessage(LogMessageItem item, bool enable)
        {
            if (item.Enabled && enable || !item.Enabled && !enable)
            {
                return;
            }

            if (enable)
            {
                EnableLogMessage(item);
            }
            else
            {
                DisableLogMessage(item);
            }
        }

        private void EnableLogMessage(LogMessageItem item)
        {
            if (item.Enabled)
            {
                return;
            }

            InsertLogMessageByDate(_logListView.Items, item);

            if (Group != null)
            {
                InsertLogMessageByDate(Group.Items, item);
            }
        }

        private void DisableLogMessage(LogMessageItem item)
        {
            if (!item.Enabled)
            {
                return;
            }

            Group?.Items.Remove(item.Item);

            _logListView.Items.Remove(item.Item);

            // Mark the item as disabled
            item.Enabled = false;
        }

        private void InsertLogMessageByDate(ListView.ListViewItemCollection items, LogMessageItem refItem)
        {
            // Get previous item
            var prevItem = refItem;
            while (prevItem != null && !prevItem.Enabled)
            {
                prevItem = prevItem.Previous;
            }

            var pos = 0;
            if (prevItem?.Item != null)
            {
                pos = items.IndexOf(prevItem.Item) + 1;
            }

            items.Insert(pos, refItem.Item);

            // Mark the item as enabled
            refItem.Enabled = true;
        }

        internal LoggerItem GetOrCreateLogger(string loggerPath)
        {
            if (string.IsNullOrEmpty(loggerPath))
            {
                return null;
            }

            // Extract Logger Name
            var currentLoggerName = loggerPath;
            var remainingLoggerPath = string.Empty;
            var pos = loggerPath.IndexOf(LoggerSeparator);
            if (pos > 0)
            {
                currentLoggerName = loggerPath.Substring(0, pos);
                remainingLoggerPath = loggerPath.Substring(pos + 1);
            }

            // Check if the Logger is in the Child Collection
            if (!Loggers.TryGetValue(currentLoggerName, out var logger))
            {
                // Not found here, needs to be created
                var childLoggerPath =
                    (string.IsNullOrEmpty(FullName) ? "" : FullName + LoggerSeparator) + currentLoggerName;
                logger = CreateLoggerItem(currentLoggerName, childLoggerPath, this);
            }

            // Continue?
            if (!string.IsNullOrEmpty(remainingLoggerPath))
            {
                logger = logger.GetOrCreateLogger(remainingLoggerPath);
            }

            return logger;
        }

        internal LogMessageItem AddLogMessage(LogMessage logMsg)
        {
            var item = new LogMessageItem(this, logMsg)
            {
                Enabled = Enabled
            };
            LogMessages.Add(item);


            // We may remove and add many items, disable the drawing one moment
            _logListView.BeginUpdate();

            // Limit the number of displayed messages if necessary
            if (UserSettings.Instance.MessageCycleCount > 0)
            {
                RemoveExtraLogMessages(UserSettings.Instance.MessageCycleCount);
            }

            var index = 0;
            if (_logListView.Items.Count > 0)
            {
                for (index = _logListView.Items.Count; index > 0; --index)
                {
                    item.Previous = _logListView.Items[index - 1].Tag as LogMessageItem;
                    if (item.Previous != null && item.Previous.Message.TimeStamp.Ticks <= item.Message.TimeStamp.Ticks)
                    {
                        break;
                    }
                }
            }

            // Message
            if (Enabled)
            {
                // Add it to the main list
                _logListView.Items.Insert(index, item.Item);

                // Add to the corresponding if necessary
                if (UserSettings.Instance.GroupLogMessages)
                {
                    Group?.Items.Add(item.Item);
                }

                // Force the item to be visible if necessary
                if (UserSettings.Instance.AutoScrollToLastLog)
                {
                    item.Item.EnsureVisible();
                }
            }

            // Hide the item is is not in range or doesn't match the current text search if any
            if (!IsItemToBeEnabled(item))
            {
                DisableLogMessage(item);
            }

            // Done!
            _logListView.EndUpdate();

            return item;
        }

        private void RemoveExtraLogMessages(int count)
        {
            // TODO: Buggy with Grouping....!!!

            var idx = 0;
            while (LogMessages.Count > count)
            {
                var item = LogMessages[idx];

                if (!item.Enabled)
                {
                    count++;
                    idx++;
                }

                RemoveLogMessage(item);
            }
        }

        private void RemoveLogMessage(LogMessageItem item)
        {
            LogMessages.Remove(item);

            Group?.Items.Remove(item.Item);

            _logListView.Items.Remove(item.Item);
        }

        internal void SearchText(string str)
        {
            _logListView.BeginUpdate();

            DoSearch(str);

            _logListView.EndUpdate();
        }

        private void DoSearch(string str)
        {
            _hasSearchedText = !string.IsNullOrEmpty(str);
            _searchedText = str;

            // Skip all log messages if logger is disabled
            if (Enabled)
            {
                foreach (var item in LogMessages)
                {
                    EnableLogMessage(item, IsItemToBeEnabled(item));
                }
            }

            // Iterate call
            foreach (var kvp in Loggers)
            {
                kvp.Value.DoSearch(_searchedText);
            }
        }

        internal bool IsItemToBeEnabled(LogMessageItem item) =>
            item.IsLevelInRange() && (!_hasSearchedText || item.HasSearchedText(_searchedText));

        public override string ToString() => Name;
    }
}