# File Collector

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
<!-- TODO: Add build status badge once GitHub Actions is set up -->
<!-- [![Build Status](https://github.com/your-username/FileCollector/actions/workflows/release.yml/badge.svg)](https://github.com/your-username/FileCollector/actions/workflows/release.yml) -->
<!-- TODO: Add latest release badge -->
<!-- [![Latest Release](https://img.shields.io/github/v/release/your-username/FileCollector)](https://github.com/your-username/FileCollector/releases/latest) -->

**File Collector** is a streamlined, cross-platform desktop application designed to simplify the process of gathering and consolidating content from multiple files and directories. Built with the power of .NET Blazor and the lightweight Photino framework, it provides an intuitive interface for selecting local file system entries, applying sophisticated ignore patterns, and merging the contents into a single, ready-to-use text block.

This tool is invaluable for developers, writers, researchers, or anyone who needs to aggregate textual information for Large Language Models (LLMs), create comprehensive code snippets for documentation, or compile various text sources into one cohesive output.

## Table of Contents

*   [Why File Collector?](#why-file-collector)
*   [Core Features](#core-features)
*   [Tech Stack](#tech-stack)
*   [Screenshots](#screenshots)
*   [Getting Started](#getting-started)
    *   [Prerequisites](#prerequisites)
    *   [Building and Running](#building-and-running)
*   [How to Use](#how-to-use)
*   [Configuration Details](#configuration-details)
    *   [Application Settings](#application-settings)
    *   [Ignore Patterns](#ignore-patterns)
*   [Contributing](#contributing)
*   [License](#license)

## Why File Collector?

In an era of information abundance and the rise of AI-powered tools, efficiently gathering and preparing textual data is more important than ever. File Collector addresses this need by:

*   **Saving Time:** Quickly select and merge dozens of files instead of manually copying and pasting.
*   **Reducing Errors:** Ensure all necessary content is included and unwanted content (like build artifacts or logs) is excluded through precise ignore patterns.
*   **Improving LLM Prompts:** Construct comprehensive and well-formatted context for Large Language Models, leading to better and more accurate responses.
*   **Streamlining Documentation:** Easily gather code snippets and textual explanations from various parts of a project.
*   **Cross-Platform Accessibility:** Use the same tool seamlessly across Windows, macOS, and Linux environments.

## Core Features

File Collector is packed with features designed to make your file aggregation process smooth and efficient:

*   **Cross-Platform Native Experience:**
    *   Leverages Photino to deliver a true native desktop application feel on Windows, macOS, and Linux, without the overhead of a full browser.

*   **Intuitive Folder Selection & Navigation:**
    *   **Root Folder Selection:** Start by picking any folder on your system as the root for your collection.
    *   **Interactive File Tree:** A dynamic tree view displays the selected folder's structure, allowing for easy browsing of directories and files.

*   **Advanced Selection Control:**
    *   **Checkbox Selection:** Select individual files or entire directories with simple checkboxes.
    *   **Recursive Selection:** Checking a directory automatically selects all its non-ignored children (files and sub-directories).
    *   **Parent-Child Sync:** The selection state of parent directories updates automatically based on the selection of their children, and vice-versa.
    *   **Selected Files Overview:** A dedicated list shows only the *actual files* currently selected for merging, providing a clear count and quick reference.

*   **Powerful Ignore Pattern System:**
    *   **`.gitignore` Syntax:** Define patterns to exclude unwanted files and folders using the familiar `.gitignore` syntax (e.g., `bin/`, `obj/`, `*.log`, `temp/`).
    *   **Negation Support:** Use `!` to explicitly include files that would otherwise be ignored by a broader pattern (e.g., `!important.log` within an ignored `logs/` directory).
    *   **Centralized Settings:** Manage these patterns easily from the "Settings" page.
    *   **Import from `.gitignore`:** Directly paste or upload the content of an existing `.gitignore` file to quickly populate your ignore list.

*   **Customizable Content Prompts:**
    *   **Pre-Prompt:** Define text that will be automatically inserted *before* the content of the first selected file. Ideal for global instructions or context setting.
    *   **Post-Prompt:** Define text that will be automatically inserted *after* the content of the last selected file but *before* the User Prompt. Useful for closing remarks or standard footers.
    *   **User Prompt (Session-Specific):** Add a temporary prompt directly on the main page. This text is inserted *after* all file content and the Post-Prompt. It's perfect for specific questions or instructions related to the current merging session and is not saved permanently.

*   **Intelligent Content Merging & Display:**
    *   **Ordered Merging:** Selected files are merged in lexicographical order of their full paths, ensuring consistent output.
    *   **File Delimiters:** Each merged file's content is clearly demarcated with `// File: [path]` and `// End of file: [path]` comments, making it easy to identify individual file origins in the merged output.
    *   **Syntax Highlighting:** The merged content viewer utilizes Prism.Blazor for syntax highlighting across a variety of common file types (e.g., C#, Razor, CSS, JS, SQL, etc.), significantly improving readability.
    *   **Error Handling:** If a file cannot be read, an error message is displayed in its place within the merged content, ensuring you're aware of any issues without halting the entire process.

*   **Easy Output Management:**
    *   **One-Click Copy:** A "Copy" button allows you to instantly copy the entire plain text version of the merged content (including prompts and file delimiters) to your clipboard.
    *   **Refresh Content:** Easily regenerate the merged content with a "Refresh" button if selections or prompts change.
    *   **Clear Selections:** Quickly deselect all files and clear the merged content.

*   **Persistent Configuration & State:**
    *   **Settings Storage:** Ignore patterns, Pre-prompt, and Post-prompt are saved in an `appsettings.json` file located in the application's base directory.
    *   **Session Restoration:** The application remembers the last selected root path and the specific files you had checked, allowing you to quickly resume your work when you reopen it.

## Tech Stack

*   **Framework:** C# / .NET 9
*   **UI:** Blazor Hybrid
*   **Desktop Shell:** Photino (for creating lightweight, cross-platform native desktop applications)
*   **Icons:** BlazorTablerIcons
*   **Syntax Highlighting:** S97SP.Prism.Blazor

## Screenshots

![image](https://github.com/user-attachments/assets/08be055b-5ccc-48c8-8be3-cc8a24be971f)

![image](https://github.com/user-attachments/assets/7e6cb136-f618-44ab-94a8-8eca31050031)


## Getting Started

Follow these steps to get File Collector up and running on your system.

### Prerequisites

*   **.NET 9 SDK (or later):** Ensure you have the .NET 9 Software Development Kit installed. You can download it from the [official .NET website](https://dotnet.microsoft.com/download/dotnet/9.0).

### Building and Running

1.  **Clone the Repository:**
    Open your terminal or command prompt and clone the project:
    ```bash
    git clone https://github.com/lorenzodimauro97/FileCollector.git
    cd FileCollector
    ```
    
2.  **Restore Dependencies:**
    Navigate to the project directory (if you haven't already) and restore the necessary .NET packages:
    ```bash
    dotnet restore FileCollector/FileCollector.csproj
    ```

3.  **Run the Application (Development):**
    To run the application directly for development or testing:
    ```bash
    dotnet run --project FileCollector/FileCollector.csproj
    ```

4.  **Publish for Distribution:**
    To create distributable versions for different platforms:
    ```bash
    # For Windows x64
    dotnet publish FileCollector/FileCollector.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

    # For Linux x64
    dotnet publish FileCollector/FileCollector.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

    # For macOS x64 (Intel)
    dotnet publish FileCollector/FileCollector.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

    # For macOS ARM64 (Apple Silicon - M1/M2/M3)
    # dotnet publish FileCollector/FileCollector.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
    ```
    The published application will be located in the `bin/Release/net9.0/<runtime-identifier>/publish/` directory. The `-p:PublishSingleFile=true` flag attempts to create a single executable.

## How to Use

1.  **Launch File Collector:** Run the application executable.
2.  **Select Root Folder:** Click the "Select Root Folder" button. A dialog will appear, allowing you to choose the main directory you want to work with.
3.  **Navigate and Select:**
    *   The file tree of the selected folder will appear in the left-hand panel.
    *   Click the expander icons (▶ or ▼) next to directories to show or hide their contents.
    *   Use the checkboxes to select files or entire directories. Files within selected directories (that are not ignored) will be included.
    *   The "Selected Files" list below the tree shows a count and names of only the *individual files* selected for merging.
4.  **Configure Settings (Optional but Recommended):**
    *   Click the "Settings" icon in the sidebar to navigate to the settings page.
    *   **Ignore Patterns:** Add or remove patterns (e.g., `obj/`, `*.tmp`, `!src/important.cs`). You can also paste content from a `.gitignore` file or upload one.
    *   **Prompts:** Set a "Pre-Prompt" (text before all files) and/or a "Post-Prompt" (text after all files, before the User Prompt).
    *   Click **"Save All Settings"** to persist your changes.
5.  **Add a User Prompt (Optional):**
    *   Back on the main page, in the "Merged Content" panel, you can type a "User Prompt". This prompt is temporary and applies only to the current session. It's inserted after all file content and the Post-Prompt.
    *   The merged content will automatically refresh shortly after you stop typing in this field.
6.  **Generate Merged Content:**
    *   If you've made changes to selections or settings and auto-refresh hasn't triggered, click the "Refresh" button in the "Merged Content" panel.
7.  **Review and Copy:**
    *   The right-hand panel will display the merged content, with syntax highlighting for supported file types. Each file's content is clearly marked with its path.
    *   If any files couldn't be read, an error message will be shown in their place.
    *   Click the "Copy" button to copy the entire plain text content to your clipboard. A confirmation message ("Copied!") will briefly appear.
8.  **Clear Selections:**
    *   Use the "Clear Selection" button to deselect all items in the file tree.

## Configuration Details

### Application Settings

File Collector stores its persistent settings in an `appsettings.json` file, typically located in the same directory as the application executable (or `AppContext.BaseDirectory`). This includes:

*   `IgnorePatterns`: A list of strings for filtering files.
*   `PrePrompt`: Text to prepend to the merged output.
*   `PostPrompt`: Text to append to the merged output (before the User Prompt).

These settings are manageable through the "Settings" UI within the application.

### Ignore Patterns

The ignore patterns feature is crucial for tailoring the file collection process. It uses a syntax similar to `.gitignore`. Here's a quick rundown:

*   **Blank lines or lines starting with `#`** are ignored (comments).
*   **Standard Patterns:**
    *   `*.log` ignores all files ending with `.log`.
    *   `temp/` ignores any directory named `temp` and all its contents, at any level.
    *   `/temp/` ignores a `temp` directory only at the root of your selected folder.
    *   `docs/*.md` ignores all Markdown files directly within a `docs` directory.
*   **Directory Separators:** Always use forward slashes (`/`) in patterns, even on Windows. The application normalizes paths internally.
*   **Negation:** A pattern starting with `!` negates the pattern. Any file matching a negated pattern will be included even if it was excluded by a previous pattern.
    *   Example:
        ```
        *.tmp       # Ignore all .tmp files
        !important.tmp # But include important.tmp
        ```
*   **Matching Order:** The order of patterns matters. Later patterns can override earlier ones (especially with negation).
*   **Pattern Specificity:**
    *   `foo`: Ignores files or directories named `foo` anywhere.
    *   `/foo`: Ignores a file or directory named `foo` only at the root.
    *   `foo/`: Ignores directories named `foo` anywhere (and their contents).
    *   `foo/*`: Ignores all files and directories directly inside any directory named `foo`.

The filtering logic aims to replicate common `.gitignore` behavior.

## Contributing

We welcome contributions to File Collector! Whether it's a bug fix, a new feature, or documentation improvement, your help is appreciated.

1.  **Fork the Repository:** Click the 'Fork' button at the top right of this page.
2.  **Clone Your Fork:** `git clone https://github.com/YOUR_USERNAME/FileCollector.git`
3.  **Create a Branch:** `git checkout -b feature/my-awesome-feature` or `bugfix/address-that-bug`
4.  **Make Your Changes:** Implement your feature or bug fix.
5.  **Commit Your Changes:** `git commit -am 'Add: My awesome feature'`
6.  **Push to Your Branch:** `git push origin feature/my-awesome-feature`
7.  **Open a Pull Request:** Go to the original repository on GitHub and open a new pull request from your forked branch.

Please ensure your code follows the existing project structure and coding style. If you're adding a new feature, consider adding tests if applicable.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for full details.
