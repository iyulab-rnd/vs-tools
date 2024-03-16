import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";

// 주어진 폴더 내의 모든 파일 경로를 재귀적으로 찾습니다.
function findAllFilesInFolder(
  dirPath: string,
  arrayOfFiles: string[] = []
): string[] {
  const files = fs.readdirSync(dirPath);

  files.forEach(function (file) {
    if (fs.statSync(dirPath + "/" + file).isDirectory()) {
      arrayOfFiles = findAllFilesInFolder(dirPath + "/" + file, arrayOfFiles);
    } else {
      arrayOfFiles.push(path.join(dirPath, "/", file));
    }
  });

  return arrayOfFiles;
}

export function activate(context: vscode.ExtensionContext) {
  let disposable = vscode.commands.registerCommand(
    "copy-text-selected-files.copyFilesContent",
    async (fileUri: vscode.Uri, selectedFiles: vscode.Uri[]) => {
      let aggregatedContent = "";

      // 사용자가 파일이나 폴더를 선택하지 않았다면, 함수를 종료합니다.
      if (!fileUri) {
        vscode.window.showInformationMessage("No file or folder selected!");
        return;
      }

      const isDirectory = fs.statSync(fileUri.fsPath).isDirectory();

      if (isDirectory) {
        // 선택된 폴더 내의 모든 파일을 찾습니다.
        const files = findAllFilesInFolder(fileUri.fsPath);
        for (const filePath of files) {
          const content = fs.readFileSync(filePath, "utf8");
          const relativePath = path.relative(
            vscode.workspace.rootPath || "",
            filePath
          );
          aggregatedContent += `// ${relativePath}\n${content}\n\n`;
        }
      } else if (selectedFiles && selectedFiles.length > 0) {
        // 선택된 파일들의 내용을 복사합니다.
        for (const file of selectedFiles) {
          const content = fs.readFileSync(file.fsPath, "utf8");
          const relativePath = path.relative(
            vscode.workspace.rootPath || "",
            file.fsPath
          );
          aggregatedContent += `// ${relativePath}\n${content}\n\n`;
        }
      } else {
        // 단일 파일 선택 시 해당 파일의 내용을 복사합니다.
        const content = fs.readFileSync(fileUri.fsPath, "utf8");
        const relativePath = path.relative(
          vscode.workspace.rootPath || "",
          fileUri.fsPath
        );
        aggregatedContent += `// ${relativePath}\n${content}\n\n`;
      }

      // 클립보드에 전체 내용을 복사합니다.
      vscode.env.clipboard
        .writeText(aggregatedContent)
        .then(() =>
          vscode.window.showInformationMessage("Content copied to clipboard!")
        );
    }
  );

  context.subscriptions.push(disposable);
}

export function deactivate() {}
