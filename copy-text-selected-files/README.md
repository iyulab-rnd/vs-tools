# Copy Text of Selected Files

<img src="https://raw.githubusercontent.com/iyulab-rnd/public/main/images/screenshot.png" width="430" />

This Visual Studio Code extension enables users to directly copy the contents of selected files to the clipboard.

## Features

- **Copy Single File**: Right-click a file in the Explorer panel and choose "Copy Content of Selected Files" to copy its contents to the clipboard.
- **Copy Multiple Files**: Select multiple files, right-click on one, and select "Copy Content of Selected Files" to copy their contents. Each content is preceded by its file name and path.
- **Copy Directory Contents**: Right-click on a directory and choose "Copy Content of Selected Files" to copy the contents of all files within, including subdirectories.

## Usage

In Visual Studio Code, go to the Explorer view, select one or more files or a directory, right-click, and select "Copy Content of Selected Files".

## Example

Consider you have the following files:

`index.html`

```html
<!DOCTYPE html>
<html lang="en">
  ...
</html>
```

`src\global.scss`

```scss
body {
  font-family: Arial, sans-serif;
  margin: 0;
  padding: 0;
  background-color: #f4f4f4;
}
```

Selecting both files and using the "Copy Content of Selected Files" command, the clipboard will contain:

````
### index.html

```html
<!DOCTYPE html>
<html lang="en">
<body>
<h1>hello world</h1>
</body>
</html>
```

### src\global.scss

```scss
body {
  font-family: Arial, sans-serif;
  margin: 0;
  padding: 0;
  background-color: #f4f4f4;
}
```
````

## Keyboard Shortcuts

This extension supports the following keyboard shortcuts for faster operation:

- **Copy Content of Selected Files**: `Ctrl+Alt+C`
  - You can use this shortcut after selecting one or more files or directories in the Explorer view. This will copy the contents directly to the clipboard.

You can customize these shortcuts in Visual Studio Code by going to File > Preferences > Keyboard Shortcuts and searching for the 'Copy Content of Selected Files' command.

## Installation

1. Open Visual Studio Code.
2. Press `Ctrl+P` to open the Quick Open dialog.
3. Type `ext install copy-text-selected-files` and search for the extension.
4. Click on Install and then on Enable.
