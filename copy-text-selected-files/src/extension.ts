import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";
import { StringDecoder } from "string_decoder";
import { languageExtensions } from "./languageExtensions";

// export const languageExtensions: { [key: string]: string[] } = {
//   json: [".json", ".jsonc"],
//   javascript: [".js", ".jsx", ".mjs", ".cjs"],
//   typescript: [".ts", ".tsx"],
//   ...

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

      // 단축키 사용 시 현재 열린 모든 문서를 대상으로 함
      if (!fileUri && !selectedFiles && vscode.window.activeTextEditor) {
        selectedFiles = vscode.workspace.textDocuments
          .filter(
            (document) =>
              document.isUntitled === false && document.uri.scheme === "file"
          )
          .map((document) => document.uri);
      }

      if (selectedFiles && selectedFiles.length > 0) {
        // 선택된 파일들을 처리하는 로직
        selectedFiles.forEach((file) => {
          const fileExtension = path.extname(file.fsPath).toLowerCase();
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
          let language = determineLanguage(fileExtension);
          let codeBlock = "```";

          // 마크다운 파일일 경우, 코드 블록의 시작과 끝을 여섯 개의 백틱으로 설정
          if (fileExtension === ".md") {
            language = "markdown";
            codeBlock = "``````";
          }

          aggregatedContent += `### ${relativePath}\n\n${codeBlock}${language}\n${content}\n${codeBlock}\n\n`;
        });
      } else if (fileUri) {
        // 단일 파일 처리 로직 (기존과 동일)
        const fileExtension = path.extname(fileUri.fsPath).toLowerCase();
        if (isKnownFileType(fileExtension) || isTextBasedFile(fileUri.fsPath)) {
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
