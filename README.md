# File Collector

[![Build Status](https://github.com/lorenzodimauro97/FileCollector/actions/workflows/dotnet.yml/badge.svg)](https://github.com/lorenzodimauro97/FileCollector/actions)
[![Latest Release](https://img.shields.io/github/v/release/lorenzodimauro97/FileCollector?display_name=tag&sort=semver&label=Release&color=blueviolet)](https://github.com/lorenzodimauro97/FileCollector/releases/latest)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-blueviolet.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**File Collector** is a streamlined, cross-platform desktop application designed to simplify the aggregation and consolidation of content from multiple files and directories. Built with .NET 9, Blazor Hybrid, and the lightweight Photino framework, it provides an intuitive interface for selecting local file system entries, applying sophisticated `.gitignore`-style ignore patterns, and merging content into a single, structured text block ready for use.

This tool is invaluable for developers, writers, and researchers needing to compile comprehensive context for Large Language Models (LLMs), create extensive code snippets for documentation, or aggregate various text-based sources into one cohesive output.

![image](https://github.com/user-attachments/assets/5cc6ad3f-dcf9-467e-8f71-6dcb52f4a354)

## Table of Contents

*   [Core Features](#core-features)
*   [Tech Stack](#tech-stack)
*   [Getting Started](#getting-started)
*   [How to Use](#how-to-use)
*   [Configuration Details](#configuration-details)
*   [Future Enhancements](#future-enhancements)
*   [Contributing](#contributing)
*   [License](#license)

## Core Features

*   **Native Cross-Platform Experience:** True native desktop feel on Windows, macOS, and Linux via Photino, without the overhead of a full embedded browser.
*   **Intuitive File System Interaction:**
    *   **Root Folder Selection:** Start by choosing any folder on your local system.
    *   **Interactive File Tree:** A dynamic tree view presents the folder's structure for easy browsing.
    *   **Granular Selection:** Select individual files or entire directories with checkboxes. Parent directories update their state (checked/indeterminate) based on child selections.
*   **Powerful Search & Filtering:**
    *   **Live File Search:** Quickly find files and folders within the loaded tree as you type.
    *   **Advanced Ignore Engine:** Uses `.gitignore` syntax to exclude unwanted files and folders. Supports negation (`!`), wildcards (`*`, `**`), and path anchoring.
    *   **Pattern Management:** Manage ignore patterns on the Settings page, including direct import from `.gitignore` files.
*   **Intelligent & Secure Content Merging:**
    *   **Ordered Aggregation:** Files are merged in a consistent, predictable order.
    *   **Data Privatization:** Automatically redact values in common configuration files (e.g., `appsettings.json`, `.env`) to prevent leaking secrets.
    *   **Syntax Highlighting:** Merged content is displayed with rich syntax highlighting for dozens of languages.
    *   **Token Count Estimation:** Get a real-time approximate token count of your merged content.
*   **Customizable & Persistent Context:**
    *   **Custom Prompts:** Add a persistent Pre-Prompt and Post-Prompt, plus a temporary User Prompt for session-specific instructions.
    *   **Context Management:** Save and load "contexts" (a root path + selected files) to quickly restore frequent selections.
    *   **Session Resumption:** Automatically reloads the last used root path and file selections on startup.
*   **Modern & User-Friendly UI:**
    *   **Resizable Layout:** Easily drag to resize the side panel to customize your workspace.
    *   **One-Click Copy:** Copy the entire merged plain text output to the clipboard.
    *   **Automatic Refresh:** Merged content automatically updates as you type in the User Prompt.
*   **Automatic Updates:**
    *   The application can check for new releases on startup or manually.
    *   If an update is found, it can be downloaded and applied automatically for a seamless upgrade experience.

## Tech Stack

*   **Core Framework:** .NET 9 (C# 13)
*   **UI Framework:** Blazor Hybrid (rendering web UI natively)
*   **Desktop Shell:** Photino (for lightweight, cross-platform native desktop applications)
*   **Iconography:** BlazorTablerIcons
*   **Syntax Highlighting:** S97SP.Prism.Blazor (client-side)
*   **Logging:** Serilog
*   **Build & Packaging:** .NET SDK tools

## Getting Started

### Prerequisites

*   **.NET 9 SDK (or later):** Download from the [official .NET website](https://dotnet.microsoft.com/download/dotnet/9.0).

### Building and Running

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/lorenzodimauro97/FileCollector.git
    cd FileCollector
    ```

2.  **Restore Dependencies:**
    Navigate to the solution's root directory (`FileCollector/`) and restore .NET packages:
    ```bash
    dotnet restore
    ```

3.  **Run the Application (Development):**
    From the `FileCollector/` directory, run the main project:
    ```bash
    dotnet run --project FileCollector/FileCollector.csproj
    ```

4.  **Publish for Distribution:**
    To create a self-contained, single-file executable:
    ```bash
    # Replace <runtime-identifier> with win-x64, linux-x64, osx-x64, or osx-arm64
    # Example for Windows x64:
    dotnet publish FileCollector/FileCollector.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
    ```
    The published application will be in `FileCollector/bin/Release/net9.0/<runtime-identifier>/publish/`.

## How to Use

1.  **Launch File Collector.**
2.  **Select Root Folder:** Click "Select Root Folder" to choose your project directory.
3.  **Select Files:** Use the checkboxes in the "File System" panel to select files and directories.
4.  **Search (Optional):** Use the "Search Files" panel to find specific items in the tree.
5.  **Review Selections:** The "Selected Files" panel lists all individual files queued for merging.
6.  **Configure Settings (Optional):**
    *   Go to the **Settings** page from the sidebar.
    *   Add **Ignore Patterns** (e.g., `obj/`, `*.tmp`). You can also paste or upload a `.gitignore` file.
    *   Set a global **Pre-Prompt** or **Post-Prompt**.
    *   Set the default behavior for **Data Privatization**.
    *   Configure **Update** settings.
    *   Click **"Save Changes"**.
7.  **Add User Prompt (Optional):** On the main page, type instructions into the "User Prompt" text area. The content will refresh automatically.
8.  **Toggle Output Options:** Use the checkboxes below the User Prompt to include the file tree in the output or to enable/disable data privatization for the current session.
9.  **Copy Output:** Click the "Copy" button to place the entire plain text content onto your clipboard.

## Configuration Details

The application uses an `appsettings.json` file (located in the same directory as the executable) to store persistent settings.

*   `appSettings`:
    *   `ignorePatterns`: An array of strings for filtering files (e.g., `["bin/", "obj/", "*.log"]`).
    *   `prePrompt`: A string for text to prepend to the merged output.
    *   `postPrompt`: A string for text to append (before User Prompt).
    *   `privatizeDataInOutput`: A boolean (`true`/`false`) to set the default state of the data privatization feature.
    *   `savedContexts`: An array of objects, each representing a saved selection context (root path and selected file paths).
    *   `update`: An object containing settings for the automatic updater.
        *   `gitHubRepoOwner`: The owner of the GitHub repository (e.g., "lorenzodimauro97").
        *   `gitHubRepoName`: The name of the repository (e.g., "FileCollector").
        *   `checkForUpdatesOnStartup`: A boolean (`true`/`false`) to enable/disable automatic update checks.
        *   `updaterExecutableName`: The filename of the updater executable (e.g., "FileCollector.Updater.exe").

## Future Enhancements

*   **Drag & Drop Root Folder:** Allow setting the root folder by dragging it onto the application window.
*   **Multiple Root Folders:** Support adding and managing selections from multiple, disparate root folders simultaneously.
*   **Customizable File Delimiters:** Provide options to change the format of `// File:` and `// End of file:` comments.
*   **Export/Import of Settings:** Allow users to back up and restore their `appsettings.json`.
*   **Accessibility Improvements:** Ongoing review and enhancements for WCAG compliance.

## Contributing

Contributions are highly welcome! Whether it's bug fixes, feature implementations, or documentation improvements, your input is valuable.

1.  **Fork the Repository.**
2.  **Create a Feature Branch:** `git checkout -b feature/your-great-idea`
3.  **Commit Your Changes:** `git commit -m "feat: Describe your amazing feature"`
4.  **Push to the Branch:** `git push origin feature/your-great-idea`
5.  **Open a Pull Request** against the `main` branch of the original repository.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for full details.