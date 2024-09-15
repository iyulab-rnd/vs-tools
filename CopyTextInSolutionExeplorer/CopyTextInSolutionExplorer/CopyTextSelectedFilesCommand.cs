using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using CopyTextInSolutionExeplorer;

namespace CopyTextInSolutionExplorer
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CopyTextSelectedFilesCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("ecda92e4-f110-40f3-9dbf-753a995322ec");

        private readonly AsyncPackage package;

        private CopyTextSelectedFilesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static CopyTextSelectedFilesCommand Instance
        {
            get;
            private set;
        }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CopyTextSelectedFilesCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                if (!(Package.GetGlobalService(typeof(DTE)) is DTE2 dte)) return;

                if (!(dte.ToolWindows.SolutionExplorer.SelectedItems is Array items)) return;

                string combinedText = "";
                HashSet<string> processedFiles = new HashSet<string>();

                foreach (UIHierarchyItem selectedItem in items)
                {
                    if (selectedItem.Object is ProjectItem projectItem)
                    {
                        ProcessProjectItem(projectItem, ref combinedText, processedFiles);
                    }
                    else if (selectedItem.Object is Project project)
                    {
                        ProcessProject(project, ref combinedText, processedFiles);
                    }
                    else if (selectedItem.Object is UIHierarchyItem folderItem)
                    {
                        ProcessFolderItem(folderItem, ref combinedText, processedFiles);
                    }
                }

                if (string.IsNullOrEmpty(combinedText))
                {
                    dte.StatusBar.Text = "No files were selected or the selected files are empty.";
                    return;
                }
                Clipboard.SetText(combinedText);

                dte.StatusBar.Text = "The contents of the file(s) have been copied to the clipboard.";
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "An error occurred during copying: " + ex.Message,
                    "Error",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private string MapFileExtensionToLanguage(string fileExtension)
        {
            var map = LanguageExtensions.GetMap();

            if (map.TryGetValue(fileExtension, out string language))
                return language;
            else
                return "";
        }

        private void ProcessProjectItem(ProjectItem projectItem, ref string combinedText, HashSet<string> processedFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItem == null)
            {
                return;
            }

            if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindSolutionItems)
            {
                // This is a solution folder
                if (projectItem.ProjectItems != null)
                {
                    foreach (ProjectItem subItem in projectItem.ProjectItems)
                    {
                        ProcessProjectItem(subItem, ref combinedText, processedFiles);
                    }
                }
                else
                {
                    // Handle the case when ProjectItems is null for a Solution Items folder
                    ProcessSolutionItem(projectItem, ref combinedText, processedFiles);
                }
            }
            else
            {
                // Process the current item
                ProcessSingleItem(projectItem, ref combinedText, processedFiles);

                // Process subitems if any
                if (projectItem.ProjectItems != null)
                {
                    foreach (ProjectItem subItem in projectItem.ProjectItems)
                    {
                        ProcessProjectItem(subItem, ref combinedText, processedFiles);
                    }
                }
            }
        }

        private void ProcessSolutionItem(ProjectItem solutionItem, ref string combinedText, HashSet<string> processedFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Try to get the physical file associated with the solution item
            string filePath = solutionItem.FileNames[1];
            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath) && !processedFiles.Contains(filePath))
            {
                processedFiles.Add(filePath);

                string relativePath = GetRelativePathForSolutionItem(filePath, solutionItem);

                string fileName = System.IO.Path.GetFileName(filePath);
                string fileExtension = System.IO.Path.GetExtension(fileName);
                string markdownLanguage = MapFileExtensionToLanguage(fileExtension);

                combinedText += $"### {relativePath}\n\n```{markdownLanguage}\n";
                combinedText += System.IO.File.ReadAllText(filePath) + "\n```\n\n";
            }
        }

        private string GetRelativePathForSolutionItem(string filePath, ProjectItem solutionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string solutionDir = System.IO.Path.GetDirectoryName(solutionItem.DTE.Solution.FullName);
            Uri fileUri = new Uri(filePath);
            Uri solutionUri = new Uri(solutionDir + System.IO.Path.DirectorySeparatorChar);
            return Uri.UnescapeDataString(solutionUri.MakeRelativeUri(fileUri).ToString().Replace('/', System.IO.Path.DirectorySeparatorChar));
        }

        private void ProcessSingleItem(ProjectItem item, ref string combinedText, HashSet<string> processedFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            for (short i = 1; i <= item.FileCount; i++)
            {
                string filePath = item.FileNames[i];
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath) || processedFiles.Contains(filePath))
                {
                    continue;
                }

                processedFiles.Add(filePath);

                string actualFilePath = GetActualFilePath(item, filePath);
                string relativePath = GetRelativePathWithProjectName(actualFilePath, item.ContainingProject);

                string fileName = System.IO.Path.GetFileName(actualFilePath);
                string fileExtension = System.IO.Path.GetExtension(fileName);
                string markdownLanguage = MapFileExtensionToLanguage(fileExtension);

                combinedText += $"### {relativePath}\n\n```{markdownLanguage}\n";
                combinedText += System.IO.File.ReadAllText(actualFilePath) + "\n```\n\n";
            }
        }

        private string GetActualFilePath(ProjectItem item, string originalPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var linkProperty = item.Properties.Item("Link");
                if (linkProperty != null && linkProperty.Value != null)
                {
                    string linkPath = linkProperty.Value.ToString();
                    if (!string.IsNullOrEmpty(linkPath))
                    {
                        string projectPath = System.IO.Path.GetDirectoryName(item.ContainingProject.FullName);
                        return System.IO.Path.Combine(projectPath, linkPath);
                    }
                }
            }
            catch (ArgumentException)
            {
                // The "Link" property doesn't exist, so this is not a linked item
            }

            return originalPath;
        }

        private void ProcessFolderItem(UIHierarchyItem folderItem, ref string combinedText, HashSet<string> processedFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (folderItem.Object is ProjectItem projectItem)
            {
                ProcessProjectItem(projectItem, ref combinedText, processedFiles);
            }
            else if (folderItem.UIHierarchyItems != null)
            {
                foreach (UIHierarchyItem subItem in folderItem.UIHierarchyItems)
                {
                    ProcessFolderItem(subItem, ref combinedText, processedFiles);
                }
            }
        }

        private string GetRelativePathWithProjectName(string filePath, Project project)
        {
            string projectDirectory = System.IO.Path.GetDirectoryName(project.FullName);
            string relativePath = GetRelativePath(filePath, project.FullName);
            return System.IO.Path.Combine(project.Name, relativePath);
        }

        private string GetRelativePath(string filePath, string projectPath)
        {
            Uri fileUri = new Uri(filePath);
            Uri projectUri = new Uri(System.IO.Path.GetDirectoryName(projectPath) + "\\");
            Uri relativeUri = projectUri.MakeRelativeUri(fileUri);
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', '\\'));
        }

        private void ProcessProject(Project project, ref string combinedText, HashSet<string> processedFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                ProcessProjectItem(projectItem, ref combinedText, processedFiles);
            }
        }
    }
}
