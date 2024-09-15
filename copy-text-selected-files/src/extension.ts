import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";
import { StringDecoder } from "string_decoder";
import { languageExtensions } from "./languageExtensions";
import * as mime from "mime-types";

// 최대 파일 크기 (3MB)
const MAX_FILE_SIZE = 3 * 1024 * 1024; // 3MB

function determineLanguage(fileExtension: string): string {
  for (const language in languageExtensions) {
    const extensions = languageExtensions[language];
    if (extensions.includes(fileExtension)) {
      return language;
    }
  }
  // 언어를 찾지 못한 경우 확장자를 그대로 반환 (첫 글자의 점 제거)
  return fileExtension.startsWith('.') ? fileExtension.slice(1) : fileExtension;
}

function isBinaryFile(filePath: string): boolean {
  const mimeType = mime.lookup(filePath);
  if (!mimeType) {
    return false;
  }
  // MIME 타입이 아래 목록에 있는 경우 바이너리 파일로 간주
  const binaryMimeTypes = [
    // 이미지 파일
    "image/png",
    "image/jpeg",
    "image/gif",
    "image/bmp",
    "image/webp",
    "image/tiff",
    "image/svg+xml",
    "image/x-icon",
    // 비디오 파일
    "video/mp4",
    "video/x-msvideo",
    "video/mpeg",
    "video/ogg",
    "video/webm",
    "video/3gpp",
    "video/3gpp2",
    // 오디오 파일
    "audio/midi",
    "audio/mpeg",
    "audio/webm",
    "audio/ogg",
    "audio/wav",
    "audio/x-wav",
    "audio/x-pn-wav",
    "audio/aac",
    "audio/flac",
    "audio/aiff",
    "audio/basic",
    "audio/x-aiff",
    // 압축 파일
    "application/zip",
    "application/x-tar",
    "application/x-bzip",
    "application/x-bzip2",
    "application/gzip",
    "application/x-7z-compressed",
    // 애플리케이션 파일
    "application/octet-stream",
    "application/pdf",
    "application/x-msdownload",
    "application/vnd.ms-excel",
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    "application/msword",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    "application/vnd.ms-powerpoint",
    "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    "application/rtf",
    "application/vnd.oasis.opendocument.text",
    "application/vnd.oasis.opendocument.spreadsheet",
    "application/vnd.oasis.opendocument.presentation",
  ];
  return binaryMimeTypes.includes(mimeType);
}

function getAllFiles(dirPath: string, arrayOfFiles: string[] = []): string[] {
  const files = fs.readdirSync(dirPath);
  files.forEach((file) => {
    const filePath = path.join(dirPath, file);
    if (fs.statSync(filePath).isDirectory()) {
      arrayOfFiles = getAllFiles(filePath, arrayOfFiles);
    } else {
      arrayOfFiles.push(filePath);
    }
  });
  return arrayOfFiles;
}

export function activate(context: vscode.ExtensionContext) {
  let disposable = vscode.commands.registerCommand(
    "copy-text-selected-files.copyFilesContent",
    async (fileUri?: vscode.Uri, selectedFiles?: vscode.Uri[]) => {
      let aggregatedContent = "";

      if (!fileUri && !selectedFiles && vscode.window.activeTextEditor) {
        selectedFiles = vscode.workspace.textDocuments
          .filter(
            (document) =>
              document.isUntitled === false && document.uri.scheme === "file"
          )
          .map((document) => document.uri);
      }

      if (selectedFiles && selectedFiles.length > 0) {
        for (const file of selectedFiles) {
          const fileStats = fs.statSync(file.fsPath);
          if (fileStats.isDirectory()) {
            const allFiles = getAllFiles(file.fsPath);
            for (const filePath of allFiles) {
              const fileStats = fs.statSync(filePath);
              if (fileStats.size > MAX_FILE_SIZE) {
                vscode.window.showWarningMessage(
                  `Skipped large file: ${filePath}`
                );
                continue;
              }

              if (isBinaryFile(filePath)) {
                continue;
              }

              const content = fs.readFileSync(filePath, "utf8");
              const relativePath = path.relative(
                vscode.workspace.rootPath || "",
                filePath
              );
              let language = determineLanguage(
                path.extname(filePath).toLowerCase()
              );
              let codeBlock = "```";

              if (path.extname(filePath).toLowerCase() === ".md") {
                language = "markdown";
                codeBlock = "``````";
              }

              aggregatedContent += `### ${relativePath}\n\n${codeBlock}${language}\n${content}\n${codeBlock}\n\n`;
            }
          } else {
            if (fileStats.size > MAX_FILE_SIZE) {
              vscode.window.showWarningMessage(
                `Skipped large file: ${file.fsPath}`
              );
              continue;
            }

            if (isBinaryFile(file.fsPath)) {
              continue;
            }

            const content = fs.readFileSync(file.fsPath, "utf8");
            const relativePath = path.relative(
              vscode.workspace.rootPath || "",
              file.fsPath
            );
            let language = determineLanguage(
              path.extname(file.fsPath).toLowerCase()
            );
            let codeBlock = "```";

            if (path.extname(file.fsPath).toLowerCase() === ".md") {
              language = "markdown";
              codeBlock = "``````";
            }

            aggregatedContent += `### ${relativePath}\n\n${codeBlock}${language}\n${content}\n${codeBlock}\n\n`;
          }
        }
      } else if (fileUri) {
        const fileStats = fs.statSync(fileUri.fsPath);
        if (fileStats.isDirectory()) {
          const allFiles = getAllFiles(fileUri.fsPath);
          for (const filePath of allFiles) {
            const fileStats = fs.statSync(filePath);
            if (fileStats.size > MAX_FILE_SIZE) {
              vscode.window.showWarningMessage(
                `Skipped large file: ${filePath}`
              );
              continue;
            }

            if (isBinaryFile(filePath)) {
              continue;
            }

            const content = fs.readFileSync(filePath, "utf8");
            const relativePath = path.relative(
              vscode.workspace.rootPath || "",
              filePath
            );
            let language = determineLanguage(
              path.extname(filePath).toLowerCase()
            );
            let codeBlock = "```";

            if (path.extname(filePath).toLowerCase() === ".md") {
              language = "markdown";
              codeBlock = "``````";
            }

            aggregatedContent += `### ${relativePath}\n\n${codeBlock}${language}\n${content}\n${codeBlock}\n\n`;
          }
        } else {
          if (fileStats.size > MAX_FILE_SIZE) {
            vscode.window.showWarningMessage(
              `Skipped large file: ${fileUri.fsPath}`
            );
            return;
          }

          if (isBinaryFile(fileUri.fsPath)) {
            return;
          }

          const content = fs.readFileSync(fileUri.fsPath, "utf8");
          const relativePath = path.relative(
            vscode.workspace.rootPath || "",
            fileUri.fsPath
          );
          let language = determineLanguage(
            path.extname(fileUri.fsPath).toLowerCase()
          );
          let codeBlock = "```";

          if (path.extname(fileUri.fsPath).toLowerCase() === ".md") {
            language = "markdown";
            codeBlock = "``````";
          }

          aggregatedContent += `### ${relativePath}\n\n${codeBlock}${language}\n${content}\n${codeBlock}\n\n`;
        }
      }

      if (aggregatedContent) {
        vscode.env.clipboard
          .writeText(aggregatedContent)
          .then(() =>
            vscode.window.showInformationMessage("Content copied to clipboard!")
          );
      } else {
        vscode.window.showInformationMessage(
          "No text files found or selected!"
        );
      }
    }
  );

  context.subscriptions.push(disposable);
}

export function deactivate() {}
