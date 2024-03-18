import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";
import { StringDecoder } from "string_decoder";
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

// 파일이 텍스트 기반인지 확인하는 함수
function isTextBasedFile(filePath: string): boolean {
  const buffer = fs.readFileSync(filePath);
  const decoder = new StringDecoder("utf8");
  const text = decoder.write(buffer);
  const nonPrintable = text.match(/[\x00-\x08\x0E-\x1F\x80-\xFF]/g);
  const threshold = 0.1; // 10% 이상이 인쇄 불가능한 문자면 바이너리 파일로 간주
  return !nonPrintable || nonPrintable.length / text.length < threshold;
}

// 파일 확장자가 알려진 언어 목록에 있는지 확인하는 함수
function isKnownFileType(fileExtension: string): boolean {
  return Object.values(languageExtensions).some(
    (extensions) =>
      extensions.includes(fileExtension) ||
      extensions.includes(`*${fileExtension}`)
  );
}

export function activate(context: vscode.ExtensionContext) {
  let disposable = vscode.commands.registerCommand(
    "copy-text-selected-files.copyFilesContent",
    async (fileUri?: vscode.Uri, selectedFiles?: vscode.Uri[]) => {
      let aggregatedContent = "";

      // 사용자가 단축키를 사용해 명령을 실행했을 때 현재 활성 파일 또는 선택된 파일들 처리
      if (!fileUri && vscode.window.activeTextEditor) {
        fileUri = vscode.window.activeTextEditor.document.uri;
      }

      if (!fileUri) {
        vscode.window.showInformationMessage("No file or folder selected!");
        return;
      }

      const isDirectory = fs.statSync(fileUri.fsPath).isDirectory();
      if (isDirectory) {
        const files = findAllFilesInFolder(fileUri.fsPath);
        files.forEach((filePath) => {
          const fileExtension = path.extname(filePath);
          if (!isKnownFileType(fileExtension) && !isTextBasedFile(filePath)) {
            return; // 알려진 파일 형식이 아니고 텍스트 기반이 아니면 건너뜀
          }
          const content = fs.readFileSync(filePath, "utf8");
          const relativePath = path.relative(
            vscode.workspace.rootPath || "",
            filePath
          );
          const language = determineLanguage(fileExtension);
          aggregatedContent += `### ${relativePath}\n\n\`\`\`${language}\n${content}\n\`\`\`\n\n`;
        });
      } else if (selectedFiles && selectedFiles.length > 0) {
        selectedFiles.forEach((file) => {
          const fileExtension = path.extname(file.fsPath);
          if (
            !isKnownFileType(fileExtension) &&
            !isTextBasedFile(file.fsPath)
          ) {
            return; // 알려진 파일 형식이 아니고 텍스트 기반이 아니면 건너뜀
          }
          const content = fs.readFileSync(file.fsPath, "utf8");
          const relativePath = path.relative(
            vscode.workspace.rootPath || "",
            file.fsPath
          );
          const language = determineLanguage(fileExtension);
          aggregatedContent += `### ${relativePath}\n\n\`\`\`${language}\n${content}\n\`\`\`\n\n`;
        });
      } else {
        const fileExtension = path.extname(fileUri.fsPath);
        if (isKnownFileType(fileExtension) || isTextBasedFile(fileUri.fsPath)) {
          const content = fs.readFileSync(fileUri.fsPath, "utf8");
          const relativePath = path.relative(
            vscode.workspace.rootPath || "",
            fileUri.fsPath
          );
          const language = determineLanguage(fileExtension);
          aggregatedContent += `### ${relativePath}\n\n\`\`\`${language}\n${content}\n\`\`\`\n\n`;
        }
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
