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

function isTextBasedFile(filePath: string): boolean {
  const buffer = fs.readFileSync(filePath);
  const decoder = new StringDecoder("utf8");
  const text = decoder.write(buffer);
  const nonPrintable = text.match(/[\x00-\x08\x0E-\x1F\x80-\xFF]/g);
  const threshold = 0.1;
  return !nonPrintable || nonPrintable.length / text.length < threshold;
}

function isKnownFileType(fileExtension: string): boolean {
  return Object.values(languageExtensions).some(
    (extensions) =>
      extensions.includes(fileExtension) ||
      extensions.includes(`*${fileExtension}`)
  );
}

function isBinaryFile(filePath: string): boolean {
  const mimeType = mime.lookup(filePath);
  if (!mimeType) return true;
  return !mimeType.startsWith("text/");
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

              const fileExtension = path.extname(filePath).toLowerCase();
              if (!isKnownFileType(fileExtension) || isBinaryFile(filePath)) {
                continue;
              }

              const content = fs.readFileSync(filePath, "utf8");
              const relativePath = path.relative(
                vscode.workspace.rootPath || "",
                filePath
              );
              let language = determineLanguage(fileExtension);
              let codeBlock = "```";

              if (fileExtension === ".md") {
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

            const fileExtension = path.extname(file.fsPath).toLowerCase();
            if (!isKnownFileType(fileExtension) || isBinaryFile(file.fsPath)) {
              continue;
            }

            const content = fs.readFileSync(file.fsPath, "utf8");
            const relativePath = path.relative(
              vscode.workspace.rootPath || "",
              file.fsPath
            );
            let language = determineLanguage(fileExtension);
            let codeBlock = "```";

            if (fileExtension === ".md") {
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

            const fileExtension = path.extname(filePath).toLowerCase();
            if (!isKnownFileType(fileExtension) || isBinaryFile(filePath)) {
              continue;
            }

            const content = fs.readFileSync(filePath, "utf8");
            const relativePath = path.relative(
              vscode.workspace.rootPath || "",
              filePath
            );
            let language = determineLanguage(fileExtension);
            let codeBlock = "```";

            if (fileExtension === ".md") {
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

          const fileExtension = path.extname(fileUri.fsPath).toLowerCase();
          if (!isKnownFileType(fileExtension) || isBinaryFile(fileUri.fsPath)) {
            return;
          }

          const content = fs.readFileSync(fileUri.fsPath, "utf8");
          const relativePath = path.relative(
            vscode.workspace.rootPath || "",
            fileUri.fsPath
          );
          let language = determineLanguage(fileExtension);
          let codeBlock = "```";

          if (fileExtension === ".md") {
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
