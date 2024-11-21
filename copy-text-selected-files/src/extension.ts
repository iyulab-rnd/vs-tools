import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";
import { getFileFormat } from "./languageExtensions";

const MAX_FILE_SIZE = 3 * 1024 * 1024; // 3 MB

interface FileProcessingResult {
  content: string;
  relativePath: string;
  language: string;
}

async function getSelectedFiles(): Promise<vscode.Uri[]> {
  const visibleTextEditors = vscode.window.visibleTextEditors;
  if (visibleTextEditors.length > 0) {
    return visibleTextEditors.map(editor => editor.document.uri);
  }

  const activeTextEditor = vscode.window.activeTextEditor;
  if (activeTextEditor) {
    return [activeTextEditor.document.uri];
  }

  throw new Error("No files selected or open in editor");
}

function canProcessFile(filePath: string): boolean {
  try {
    const fileStats = fs.statSync(filePath);
    if (fileStats.size > MAX_FILE_SIZE) {
      vscode.window.showWarningMessage(`Skipped large file: ${filePath}`);
      return false;
    }

    return getFileFormat(filePath) !== null;
  } catch (error) {
    console.error(`Error processing file ${filePath}:`, error);
    return false;
  }
}

function processFile(filePath: string, basePath: string): FileProcessingResult | null {
  if (!canProcessFile(filePath)) {
    return null;
  }

  try {
    const content = fs.readFileSync(filePath, "utf8");
    const relativePath = path.relative(basePath, filePath);
    const format = getFileFormat(filePath);
    
    return { content, relativePath, language: format! };
  } catch (error) {
    console.error(`Error reading file ${filePath}:`, error);
    return null;
  }
}

function formatContent(result: FileProcessingResult): string {
  const codeBlock = result.language === 'markdown' ? '``````' : '```';
  return `### ${result.relativePath}\n\n${codeBlock}${result.language}\n${result.content}\n${codeBlock}\n\n`;
}

function getAllFilesRecursively(directoryPath: string): string[] {
  let files: string[] = [];
  
  const items = fs.readdirSync(directoryPath);
  
  for (const item of items) {
    const fullPath = path.join(directoryPath, item);
    const stats = fs.statSync(fullPath);
    
    if (stats.isDirectory()) {
      files = files.concat(getAllFilesRecursively(fullPath));
    } else if (stats.isFile()) {
      files.push(fullPath);
    }
  }
  
  return files;
}

async function processFileOrDirectory(uri: vscode.Uri): Promise<string> {
  try {
    const fileStats = fs.statSync(uri.fsPath);
    let content = '';

    if (fileStats.isDirectory()) {
      const allFiles = getAllFilesRecursively(uri.fsPath);
      const basePath = uri.fsPath;

      for (const filePath of allFiles) {
        const result = processFile(filePath, basePath);
        if (result) {
          content += formatContent(result);
        }
      }
    } else {
      const result = processFile(uri.fsPath, path.dirname(uri.fsPath));
      if (result) {
        content += formatContent(result);
      }
    }

    return content;
  } catch (error) {
    console.error(`Error processing path ${uri.fsPath}:`, error);
    return '';
  }
}

export function activate(context: vscode.ExtensionContext) {
  let disposable = vscode.commands.registerCommand(
    "copy-text-selected-files.copyFilesContent",
    async (uri?: vscode.Uri, uris?: vscode.Uri[]) => {
      try {
        const filesToProcess = uris || (uri ? [uri] : await getSelectedFiles());

        if (!filesToProcess?.length) {
          vscode.window.showInformationMessage("No files selected or open in editor");
          return;
        }

        let aggregatedContent = '';
        for (const file of filesToProcess) {
          aggregatedContent += await processFileOrDirectory(file);
        }

        if (aggregatedContent) {
          await vscode.env.clipboard.writeText(aggregatedContent);
          vscode.window.showInformationMessage("Content copied to clipboard!");
        } else {
          vscode.window.showInformationMessage("No text files found or selected!");
        }
      } catch (error: unknown) {
        const message = error instanceof Error ? error.message : 'An unknown error occurred';
        vscode.window.showErrorMessage(`Error: ${message}`);
      }
    }
  );

  context.subscriptions.push(disposable);
}

export function deactivate() {}