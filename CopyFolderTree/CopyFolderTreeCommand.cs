using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;

namespace CopyFolderTree
{
    internal sealed class CopyFolderTreeCommand
    {
        public const int CopyFolderOnlyCommandId = 0x0100;
        public const int CopyFolderAndFileCommandId = 0x0101;
        public static readonly Guid CommandSet = new Guid("8a3ac71f-a104-4589-9d37-a34c2c8e401d");

        private readonly AsyncPackage package;
        private readonly DTE2 _dte;
        private readonly StringBuilder _treeBuilder;
        private readonly IVsStatusbar _statusBar;
        private readonly List<ProjectItem> _currentPath;
        private bool _includingFiles;

        private CopyFolderTreeCommand(AsyncPackage package, DTE2 dte, IMenuCommandService commandService, IVsStatusbar statusBar)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            _dte = dte ?? throw new ArgumentNullException(nameof(dte));
            _statusBar = statusBar ?? throw new ArgumentNullException(nameof(statusBar));
            _treeBuilder = new StringBuilder();
            _currentPath = new List<ProjectItem>();

            if (commandService != null)
            {
                var folderOnlyCommandID = new CommandID(CommandSet, CopyFolderOnlyCommandId);
                var folderAndFileCommandID = new CommandID(CommandSet, CopyFolderAndFileCommandId);

                commandService.AddCommand(new MenuCommand((s, e) => Execute(s, e, false), folderOnlyCommandID));
                commandService.AddCommand(new MenuCommand((s, e) => Execute(s, e, true), folderAndFileCommandID));
            }
        }

        public static CopyFolderTreeCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            var statusBar = await package.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;

            if (dte == null || commandService == null || statusBar == null) return;

            Instance = new CopyFolderTreeCommand(package, dte, commandService, statusBar);
        }

        private void ShowStatusMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _statusBar.FreezeOutput(0);
            _statusBar.SetText(message);
            _statusBar.FreezeOutput(1);
        }

        private void Execute(object sender, EventArgs e, bool includeFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _includingFiles = includeFiles;
            _currentPath.Clear();

            try
            {
                var selectedItems = _dte.SelectedItems;
                if (selectedItems.Count == 0)
                {
                    ShowStatusMessage("Please select a folder to copy its tree structure.");
                    return;
                }

                _treeBuilder.Clear();
                foreach (SelectedItem item in selectedItems)
                {
                    if (item.Project != null)
                    {
                        ProcessProject(item.Project, 0, true, true);
                    }
                    else if (item.ProjectItem != null)
                    {
                        ProcessProjectItem(item.ProjectItem, 0, true, true);
                    }
                }

                if (_treeBuilder.Length > 0)
                {
                    Clipboard.SetText(_treeBuilder.ToString());
                    ShowStatusMessage($"Tree structure has been copied to the clipboard ({(_includingFiles ? "including" : "excluding")} files).");
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error: {ex.Message}");
            }
            finally
            {
                _currentPath.Clear();
            }
        }

        private void ProcessProject(Project project, int depth, bool isLast, bool isRoot)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            AppendTreeLine(depth, isLast, "📁 " + project.Name, isRoot);

            if (project.ProjectItems != null)
            {
                var items = project.ProjectItems.Cast<ProjectItem>()
                    .Where(item => IsValidItem(item))
                    .ToList();

                for (int i = 0; i < items.Count; i++)
                {
                    ProcessProjectItem(items[i], depth + 1, i == items.Count - 1, false);
                }
            }
        }

        private void ProcessProjectItem(ProjectItem item, int depth, bool isLast, bool isRoot)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _currentPath.Add(item);

                if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
                {
                    AppendTreeLine(depth, isLast, "📁 " + item.Name, isRoot);

                    if (item.ProjectItems != null)
                    {
                        var subItems = item.ProjectItems.Cast<ProjectItem>()
                            .Where(subItem => IsValidItem(subItem))
                            .ToList();

                        for (int i = 0; i < subItems.Count; i++)
                        {
                            ProcessProjectItem(subItems[i], depth + 1, i == subItems.Count - 1, false);
                        }
                    }
                }
                else if (_includingFiles && item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                {
                    AppendTreeLine(depth, isLast, "📄 " + item.Name, isRoot);
                }
            }
            catch (COMException)
            {
                // Skip items that can't be accessed
            }
            finally
            {
                if (_currentPath.Count > 0)
                {
                    _currentPath.RemoveAt(_currentPath.Count - 1);
                }
            }
        }

        private bool IsValidItem(ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder ||
                   (_includingFiles && item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile);
        }

        private List<ProjectItem> GetSiblingItems(ProjectItem parent)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (parent == null && _currentPath.Count > 0)
            {
                return _currentPath[0].Collection.Cast<ProjectItem>()
                    .Where(item => IsValidItem(item))
                    .ToList();
            }

            return parent?.ProjectItems?
                .Cast<ProjectItem>()
                .Where(item => IsValidItem(item))
                .ToList() ?? new List<ProjectItem>();
        }

        private bool IsLastAtLevel(int level)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (level >= _currentPath.Count) return true;

                var currentItem = _currentPath[level];
                var parentItem = level > 0 ? _currentPath[level - 1] : null;

                var siblings = GetSiblingItems(parentItem);
                return siblings.IndexOf(currentItem) == siblings.Count - 1;
            }
            catch
            {
                return false;
            }
        }

        private void AppendTreeLine(int depth, bool isLast, string text, bool isRoot)
        {
            StringBuilder line = new StringBuilder();

            if (isRoot)
            {
                line.Append(text);
            }
            else
            {
                for (int i = 0; i < depth - 1; i++)
                {
                    bool showVerticalLine = !IsLastAtLevel(i);
                    line.Append(showVerticalLine ? "│  " : "   ");
                }

                line.Append(isLast ? "└─" : "├─");
                line.Append(text);
            }

            _treeBuilder.AppendLine(line.ToString());
        }
    }
}