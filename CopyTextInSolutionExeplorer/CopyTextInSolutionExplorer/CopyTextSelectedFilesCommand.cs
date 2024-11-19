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
using System.Text;
using System.IO;
using UtfUnknown;
using CopyTextInSolutionExeplorer;

namespace CopyTextInSolutionExplorer
{
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

        public static CopyTextSelectedFilesCommand Instance { get; private set; }
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

                var combinedText = new StringBuilder();
                var processedFiles = new HashSet<string>();

                foreach (UIHierarchyItem selectedItem in items)
                {
                    if (selectedItem.Object is ProjectItem projectItem)
                    {
                        ProcessProjectItem(projectItem, combinedText, processedFiles);
                    }
                    else if (selectedItem.Object is Project project)
                    {
                        ProcessProject(project, combinedText, processedFiles);
                    }
                    else if (selectedItem.Object is UIHierarchyItem folderItem)
                    {
                        ProcessFolderItem(folderItem, combinedText, processedFiles);
                    }
                }

                if (combinedText.Length == 0)
                {
                    dte.StatusBar.Text = "No files were selected or the selected files are empty.";
                    return;
                }

                Clipboard.SetText(combinedText.ToString());
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

            return fileExtension.TrimStart('.').ToLower();
        }

        private string GetCodeBlockDelimiters(string markdownLanguage)
        {
            return markdownLanguage.ToLower() == "markdown" ? "``````" : "```";
        }

        private string ReadFileWithProperEncoding(string filePath)
        {
            try
            {
                // UTF.Unknown 라이브러리를 사용하여 인코딩 감지
                var detectionResult = CharsetDetector.DetectFromFile(filePath);

                if (detectionResult.Detected == null)
                {
                    // 감지 실패시 UTF-8로 시도
                    return File.ReadAllText(filePath, Encoding.UTF8);
                }

                // 감지된 인코딩으로 파일 읽기
                using (var reader = new StreamReader(filePath, detectionResult.Detected.Encoding))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading file {filePath}: {ex.Message}");
                // 오류 발생시 UTF-8로 fallback
                return File.ReadAllText(filePath, Encoding.UTF8);
            }
        }

        private void ProcessProjectItem(ProjectItem projectItem, StringBuilder combinedText, HashSet<string> processedFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItem == null) return;

            if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindSolutionItems)
            {
                // Solution folder 처리
                if (projectItem.ProjectItems != null)
                {
                    foreach (ProjectItem subItem in projectItem.ProjectItems)
                    {
                        ProcessProjectItem(subItem, combinedText, processedFiles);
                    }
                }
                else
                {
                    ProcessSolutionItem(projectItem, combinedText, processedFiles);
                }
            }
            else
            {
                // 현재 아이템 처리
                ProcessSingleItem(projectItem, combinedText, processedFiles);

                // 하위 아이템 처리
                if (projectItem.ProjectItems != null)
                {
                    foreach (ProjectItem subItem in projectItem.ProjectItems)
                    {
                        ProcessProjectItem(subItem, combinedText, processedFiles);
                    }
                }
            }
        }

        private void ProcessSolutionItem(ProjectItem solutionItem, StringBuilder combinedText, HashSet<string> processedFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string filePath = solutionItem.FileNames[1];
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath) && !processedFiles.Contains(filePath))
            {
                processedFiles.Add(filePath);

                string relativePath = GetRelativePathForSolutionItem(filePath, solutionItem);
                string fileExtension = Path.GetExtension(filePath);
                string markdownLanguage = MapFileExtensionToLanguage(fileExtension);
                string codeBlockDelimiters = GetCodeBlockDelimiters(markdownLanguage);

                string content = ReadFileWithProperEncoding(filePath);
                combinedText.AppendLine($"### {relativePath}");
                combinedText.AppendLine();
                combinedText.AppendLine($"{codeBlockDelimiters}{markdownLanguage}");
                combinedText.AppendLine(content);
                combinedText.AppendLine(codeBlockDelimiters);
                combinedText.AppendLine();
            }
        }

        private void ProcessSingleItem(ProjectItem item, StringBuilder combinedText, HashSet<string> processedFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            for (short i = 1; i <= item.FileCount; i++)
            {
                string filePath = item.FileNames[i];
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || processedFiles.Contains(filePath))
                    continue;

                processedFiles.Add(filePath);

                string actualFilePath = GetActualFilePath(item, filePath);
                string relativePath = GetRelativePathWithProjectName(actualFilePath, item.ContainingProject);
                string fileExtension = Path.GetExtension(actualFilePath);
                string markdownLanguage = MapFileExtensionToLanguage(fileExtension);
                string codeBlockDelimiters = GetCodeBlockDelimiters(markdownLanguage);

                string content = ReadFileWithProperEncoding(actualFilePath);
                combinedText.AppendLine($"### {relativePath}");
                combinedText.AppendLine();
                combinedText.AppendLine($"{codeBlockDelimiters}{markdownLanguage}");
                combinedText.AppendLine(content);
                combinedText.AppendLine(codeBlockDelimiters);
                combinedText.AppendLine();
            }
        }

        private string GetRelativePathForSolutionItem(string filePath, ProjectItem solutionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string solutionDir = Path.GetDirectoryName(solutionItem.DTE.Solution.FullName);
            Uri fileUri = new Uri(filePath);
            Uri solutionUri = new Uri(solutionDir + Path.DirectorySeparatorChar);
            return Uri.UnescapeDataString(solutionUri.MakeRelativeUri(fileUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private string GetActualFilePath(ProjectItem item, string originalPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                foreach (Property prop in item.Properties)
                {
                    if (prop != null && (prop.Name == "LocalPath" || prop.Name == "Link"))
                    {
                        string propValue = prop.Value as string;
                        if (!string.IsNullOrEmpty(propValue))
                        {
                            string projectPath = Path.GetDirectoryName(item.ContainingProject.FullName);
                            string fullPath = Path.GetFullPath(Path.Combine(projectPath, propValue));
                            if (File.Exists(fullPath))
                            {
                                return fullPath;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to access properties: {ex.Message}");
            }

            return originalPath;
        }

        private void ProcessFolderItem(UIHierarchyItem folderItem, StringBuilder combinedText, HashSet<string> processedFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (folderItem.Object is ProjectItem projectItem)
            {
                ProcessProjectItem(projectItem, combinedText, processedFiles);
            }
            else if (folderItem.UIHierarchyItems != null)
            {
                foreach (UIHierarchyItem subItem in folderItem.UIHierarchyItems)
                {
                    ProcessFolderItem(subItem, combinedText, processedFiles);
                }
            }
        }

        private string GetRelativePathWithProjectName(string filePath, Project project)
        {
            string projectDirectory = Path.GetDirectoryName(project.FullName);
            string relativePath = GetRelativePath(filePath, project.FullName);
            return Path.Combine(project.Name, relativePath);
        }

        private string GetRelativePath(string filePath, string projectPath)
        {
            Uri fileUri = new Uri(filePath);
            Uri projectUri = new Uri(Path.GetDirectoryName(projectPath) + "\\");
            Uri relativeUri = projectUri.MakeRelativeUri(fileUri);
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', '\\'));
        }

        private void ProcessProject(Project project, StringBuilder combinedText, HashSet<string> processedFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                ProcessProjectItem(projectItem, combinedText, processedFiles);
            }
        }
    }
}