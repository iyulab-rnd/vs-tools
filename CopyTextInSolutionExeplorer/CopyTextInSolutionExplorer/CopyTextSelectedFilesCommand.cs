using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using System.Windows.Forms;

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
                DTE2 dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                if (dte == null) return;

                // 선택된 항목들을 가져옴
                var items = dte.ToolWindows.SolutionExplorer.SelectedItems as Array;
                if (items == null) return;

                string combinedText = "";
                foreach (UIHierarchyItem selectedItem in items)
                {
                    // 파일 내용을 읽고 combinedText에 추가
                    if (selectedItem.Object is ProjectItem projectItem && projectItem.Properties != null)
                    {
                        string filePath = projectItem.FileNames[1];
                        if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                        {
                            // 파일 이름과 확장자를 추출
                            string fileName = System.IO.Path.GetFileName(filePath);
                            // 파일 내용을 읽어서 combinedText에 추가, 파일 이름과 확장자를 주석으로 포함
                            combinedText += $"// {fileName}\n{System.IO.File.ReadAllText(filePath)}\n\n";
                        }
                    }
                }

                // 클립보드에 combinedText 복사
                Clipboard.SetText(combinedText);

                // 상태 표시줄에 메시지 표시
                dte.StatusBar.Text = "The contents of the file have been copied to the clipboard.";
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
    }
}
