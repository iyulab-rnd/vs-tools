using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using System.Windows.Forms;
using CopyTextInSolutionExplorer;
using System.Linq;
using CopyTextInSolutionExeplorer;
using System.Collections.Generic;
using Microsoft.Internal.VisualStudio.PlatformUI.VideoLauncher;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Package;

namespace CopyTextInSolutionExplorer
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CopyTextSelectedFilesCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("ecda92e4-f110-40f3-9dbf-753a995322ec");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyTextSelectedFilesCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private CopyTextSelectedFilesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CopyTextSelectedFilesCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in CopyTextSelectedFilesCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CopyTextSelectedFilesCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                // DTE (Development Tools Environment) object를 통해 현재 환경에 접근
                if (!(Package.GetGlobalService(typeof(DTE)) is DTE2 dte)) return;

                // 선택된 항목들을 가져옴
                if (!(dte.ToolWindows.SolutionExplorer.SelectedItems is Array items)) return;

                string combinedText = "";
                HashSet<string> processedFiles = new HashSet<string>(); // 중복 방지를 위한 HashSet 초기화

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
                }

                // 클립보드에 combinedText 복사
                Clipboard.SetText(combinedText);

                // 상태 표시줄에 메시지 표시
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
            // 파일 확장자를 기준으로 언어를 찾음

            if (map.TryGetValue(fileExtension, out string language))
                return language;
            else
                return "";
        }

        private void ProcessProjectItem(ProjectItem projectItem, ref string combinedText, HashSet<string> processedFiles)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
            {
                // 파일 경로를 가져옴
                string filePath = projectItem.FileNames[1];

                // 이미 처리된 파일인지 확인
                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath) && !processedFiles.Contains(filePath))
                {
                    // 파일 경로를 HashSet에 추가
                    processedFiles.Add(filePath);

                    // 프로젝트 이름을 포함한 솔루션 탐색기의 루트부터 상대 경로 계산
                    string relativePath = GetRelativePathWithProjectName(filePath, projectItem.ContainingProject);

                    string fileName = System.IO.Path.GetFileName(filePath);
                    string fileExtension = System.IO.Path.GetExtension(fileName);
                    // 확장자를 기준으로 언어를 매핑
                    string markdownLanguage = MapFileExtensionToLanguage(fileExtension);

                    // 마크다운 형식으로 파일 경로와 언어를 추가
                    combinedText += $"### {relativePath}\n\n```{markdownLanguage}\n";
                    // 파일 내용을 읽고 combinedText에 추가
                    combinedText += System.IO.File.ReadAllText(filePath) + "\n```\n\n";
                }
            }

            // 하위 항목들을 처리
            if (projectItem.ProjectItems != null && projectItem.ProjectItems.Count > 0)
            {
                foreach (ProjectItem subItem in projectItem.ProjectItems)
                {
                    ProcessProjectItem(subItem, ref combinedText, processedFiles);
                }
            }

            // 연결된 파일들 처리 (.xaml.cs 등)
            if (projectItem.FileCount > 1)
            {
                for (short i = 2; i <= projectItem.FileCount; i++)
                {
                    string filePath = projectItem.FileNames[i];
                    if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath) && !processedFiles.Contains(filePath))
                    {
                        // 파일 경로를 HashSet에 추가
                        processedFiles.Add(filePath);

                        // 프로젝트 이름을 포함한 솔루션 탐색기의 루트부터 상대 경로 계산
                        string relativePath = GetRelativePathWithProjectName(filePath, projectItem.ContainingProject);

                        string fileName = System.IO.Path.GetFileName(filePath);
                        string fileExtension = System.IO.Path.GetExtension(fileName);
                        // 확장자를 기준으로 언어를 매핑
                        string markdownLanguage = MapFileExtensionToLanguage(fileExtension);

                        // 마크다운 형식으로 파일 경로와 언어를 추가
                        combinedText += $"### {relativePath}\n\n```{markdownLanguage}\n";
                        // 파일 내용을 읽고 combinedText에 추가
                        combinedText += System.IO.File.ReadAllText(filePath) + "\n```\n\n";
                    }
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