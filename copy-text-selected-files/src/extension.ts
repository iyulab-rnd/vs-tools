import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";
import { languageExtensions } from "./languageExtensions";

// 주어진 폴더 내의 모든 파일 경로를 재귀적으로 찾는 함수
function findAllFilesInFolder(
  dirPath: string,
  arrayOfFiles: string[] = []
): string[] {
  const files = fs.readdirSync(dirPath);

  files.forEach((file) => {
    const fullPath = path.join(dirPath, file);
    if (fs.statSync(fullPath).isDirectory()) {
      arrayOfFiles = findAllFilesInFolder(fullPath, arrayOfFiles);
    } else {
      arrayOfFiles.push(fullPath);
    }
  });

  return arrayOfFiles;
}

function determineLanguage(fileExtension: string): string {
  for (const language in languageExtensions) {
    const extensions = languageExtensions[language];
    if (
      extensions.some(
        (ext: string) => ext === fileExtension || ext === `*${fileExtension}`
      )
    ) {
      return language;
    }
  }
  return "plaintext";
}

export function activate(context: vscode.ExtensionContext) {
  let disposable = vscode.commands.registerCommand(
    "copy-text-selected-files.copyFilesContent",
    async (fileUri: vscode.Uri, selectedFiles: vscode.Uri[]) => {
      let aggregatedContent = "";

      if (!fileUri) {
        vscode.window.showInformationMessage("No file or folder selected!");
        return;
      }

      const isDirectory = fs.statSync(fileUri.fsPath).isDirectory();

      if (isDirectory) {
        const files = findAllFilesInFolder(fileUri.fsPath);
        files.forEach((filePath) => {
          const content = fs.readFileSync(filePath, "utf8");
          const relativePath = path.relative(
            vscode.workspace.rootPath || "",
            filePath
          );
          const fileExtension = path.extname(filePath);
          const language = determineLanguage(fileExtension);
          aggregatedContent += `### ${relativePath}\n\n\`\`\`${language}\n${content}\n\`\`\`\n\n`;
        });
      } else if (selectedFiles && selectedFiles.length > 0) {
        selectedFiles.forEach((file) => {
          const content = fs.readFileSync(file.fsPath, "utf8");
          const relativePath = path.relative(
            vscode.workspace.rootPath || "",
            file.fsPath
          );
          const fileExtension = path.extname(file.fsPath);
          const language = determineLanguage(fileExtension);
          aggregatedContent += `### ${relativePath}\n\n\`\`\`${language}\n${content}\n\`\`\`\n\n`;
        });
      } else {
        const content = fs.readFileSync(fileUri.fsPath, "utf8");
        const relativePath = path.relative(
          vscode.workspace.rootPath || "",
          fileUri.fsPath
        );
        const fileExtension = path.extname(fileUri.fsPath);
        const language = determineLanguage(fileExtension);
        aggregatedContent += `### ${relativePath}\n\n\`\`\`${language}\n${content}\n\`\`\`\n\n`;
      }

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
