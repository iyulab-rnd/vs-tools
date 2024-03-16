# Copy Text of Selected Files

This extension for Visual Studio Code allows users to copy the contents of selected files directly to the clipboard, making it easier to manage and share code snippets or entire files quickly.

## Features

- **Copy Single File**: Right-click on a file in the Explorer panel and select "Copy Content of Selected Files" to copy the entire content of the file to the clipboard.
- **Copy Multiple Files**: Select multiple files in the Explorer panel, right-click on one of them, and select "Copy Content of Selected Files" to copy the contents of all selected files, each preceded by the file name and path.
- **Copy Directory Contents**: Right-click on a directory in the Explorer panel and select "Copy Content of Selected Files" to copy the contents of all files within the directory, including those in subdirectories.

## Usage

To use this extension, navigate to the Explorer view in Visual Studio Code, select one or more files or a directory, right-click, and choose "Copy Content of Selected Files". The content of the selected files or all files within the selected directory will be copied to the clipboard, ready to be pasted wherever you need.

## Example

Consider you have the following files:

`hello.txt`

```
Hello, world!
```

`greet.js`

```javascript
function greet() {
  console.log("Hello, world!");
}
```

Selecting both files and using the "Copy Content of Selected Files" command, the clipboard will contain:

```javascript
// hello.txt
Hello, world!

// greet.js
function greet() {
  console.log("Hello, world!");
}
```

## Installation

1. Open Visual Studio Code
2. Press `Ctrl+P` to open the Quick Open dialog
3. Type `ext install copy-text-selected-files` to find the extension
4. Click the Install button, then the Enable button

## Requirements

Visual Studio Code version 1.81.0 or higher.
