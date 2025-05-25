# File Collector

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build Status](https://img.shields.io/badge/Build-Pending-lightgrey)](https://github.com/lorenzodimauro97/FileCollector/actions)
[![Latest Release](https://img.shields.io/github/v/release/lorenzodimauro97/FileCollector?display_name=tag&sort=semver&label=Release&color=blueviolet)](https://github.com/lorenzodimauro97/FileCollector/releases/latest)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-blueviolet.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)

**File Collector** is a streamlined, cross-platform desktop application engineered to simplify the aggregation and consolidation of content from multiple files and directories. Built with the power of .NET 9, Blazor Hybrid, and the lightweight Photino framework, it provides an intuitive interface for selecting local file system entries, applying sophisticated ignore patterns (compatible with `.gitignore` syntax), and merging content into a single, structured text block.

This tool is invaluable for developers, writers, researchers, or anyone needing to:
*   Compile comprehensive context for Large Language Models (LLMs).
*   Create extensive code snippets for documentation or knowledge bases.
*   Aggregate various text-based sources into one cohesive output.

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
    *   [Application Settings (`appsettings.json`)](#application-settings-appsettingsjson)
    *   [Ignore Patterns Explained](#ignore-patterns-explained)
*   [Known Issues & Limitations](#known-issues--limitations)
*   [Future Enhancements](#future-enhancements)
*   [Contributing](#contributing)
*   [License](#license)

## Why File Collector?

In an era of information abundance and the increasing sophistication of AI-powered tools, efficiently gathering and preparing textual data is paramount. File Collector addresses this need by:

*   **Maximizing Efficiency:** Rapidly select and merge numerous files, eliminating tedious manual copy-pasting.
*   **Ensuring Accuracy:** Precisely include necessary content while excluding artifacts (e.g., build outputs, logs) through robust ignore patterns.
*   **Optimizing LLM Interactions:** Construct comprehensive and well-formatted context for LLMs, leading to significantly improved and more relevant responses.
*   **Streamlining Documentation:** Effortlessly gather code snippets and textual explanations from diverse project components.
*   **True Cross-Platform Functionality:** Utilize a consistent, native-feeling tool across Windows, macOS, and Linux environments.

## Core Features

File Collector is engineered with a rich feature set for a seamless file aggregation experience:

*   **Native Cross-Platform Experience:**
    *   Leverages Photino for a true native desktop application feel on Windows, macOS, and Linux, without the overhead of embedded browser engines.

*   **Intuitive File System Interaction:**
    *   **Root Folder Selection:** Initiate your collection by choosing any folder on your local system.
    *   **Interactive File Tree:** A dynamic tree view presents the selected folder's structure, facilitating easy browsing and discovery.

*   **Advanced Selection Management:**
    *   **Granular Selection:** Select individual files or entire directories using intuitive checkboxes.
    *   **Recursive Selection:** Checking a directory automatically selects all its non-ignored children (files and sub-directories).
    *   **Parent-Child State Synchronization:** The selection state of parent directories dynamically updates based on child selections and vice-versa, ensuring visual consistency.
    *   **Selected Files Overview:** A dedicated list displays only the *actual files* currently selected for merging, offering a clear count and quick reference.

*   **Powerful Ignore Pattern Engine:**
    *   **`.gitignore` Syntax Compatibility:** Define patterns to exclude unwanted files and folders using the widely adopted `.gitignore` syntax (e.g., `bin/`, `obj/`, `*.log`, `temp/**`).
    *   **Negation Support:** Employ `!` to explicitly include files that would otherwise be ignored by a broader pattern (e.g., `!important.log` within an ignored `logs/` directory).
    *   **Centralized Pattern Management:** Easily manage ignore patterns via the dedicated "Settings" page.
    *   **Import from `.gitignore`:** Directly paste content or upload an existing `.gitignore` file to rapidly populate your exclusion list.

*   **Customizable Content Prompts:**
    *   **Pre-Prompt:** Define text automatically inserted *before* the content of the first selected file. Ideal for global instructions or setting foundational context.
    *   **Post-Prompt:** Define text automatically inserted *after* the content of the last selected file but *before* the User Prompt. Useful for closing remarks or standard footers.
    *   **User Prompt (Session-Specific):** Add a temporary prompt directly on the main page. This text is inserted *after* all file content and the Post-Prompt. Perfect for ad-hoc questions or instructions for the current merging session; this prompt is not saved persistently.

*   **Intelligent Content Merging & Display:**
    *   **Ordered Aggregation:** Selected files are merged in lexicographical order of their full paths, ensuring consistent and predictable output.
    *   **Clear File Demarcation:** Each merged file's content is distinctly separated by `// File: [relative_path]` and `// End of file: [relative_path]` comments, simplifying identification of individual file origins.
    *   **Syntax Highlighting:** The merged content viewer utilizes Prism.Blazor for rich syntax highlighting across numerous common file types (C#, Razor, CSS, JS, TS, SQL, etc.), significantly enhancing readability.
    *   **Robust Error Handling:** If a file cannot be read, an informative error message is displayed in its place within the merged content, ensuring awareness of issues without halting the entire process.

*   **Effortless Output Management:**
    *   **One-Click Copy:** A "Copy" button allows instant copying of the entire plain text version of the merged content (including all prompts and file delimiters) to the system clipboard.
    *   **Dynamic Refresh:** Easily regenerate the merged content with a "Refresh" button if selections or prompts change, or automatically upon User Prompt modification.
    *   **Clear Selections:** Quickly deselect all files and clear the merged content view.

*   **Persistent Configuration & State:**
    *   **Settings Persistence:** Ignore patterns, Pre-Prompt, and Post-Prompt are saved in an `appsettings.json` file located in the application's base directory.
    *   **Context Saving & Loading:** Save and load named "contexts" (a specific root path and its selected files) for quick recall of frequent selections.
    *   **Session Resumption:** The application remembers the last used root path and its associated selected files, enabling a quick return to previous work upon reopening.

## Tech Stack

*   **Core Framework:** .NET 9 (C# 13)
*   **UI Framework:** Blazor Hybrid (rendering web UI natively)
*   **Desktop Shell:** Photino (for lightweight, cross-platform native desktop applications)
*   **Iconography:** BlazorTablerIcons
*   **Syntax Highlighting:** S97SP.Prism.Blazor (client-side)
*   **Build & Packaging:** .NET SDK tools

## Screenshots

![image](https://github.com/user-attachments/assets/5cc6ad3f-dcf9-467e-8f71-6dcb52f4a354)

![image](https://github.com/user-attachments/assets/0e2b7176-8beb-4b59-9544-f81b54fcc304)

![image](https://github.com/user-attachments/assets/8f68bbfc-0b12-40ca-b1e5-ba6208b0da7d)

## Getting Started

Follow these steps to get File Collector built and running on your system.

### Prerequisites

*   **.NET 9 SDK (or later):** Ensure the .NET 9 Software Development Kit is installed. Download from the [official .NET website](https://dotnet.microsoft.com/download/dotnet/9.0).

### Building and Running

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/lorenzodimauro97/FileCollector.git
    cd FileCollector
    ```
    
2.  **Restore Dependencies:**
    Navigate to the project's root directory (`FileCollector/`) and restore .NET packages:
    ```bash
    dotnet restore FileCollector/FileCollector.csproj
    ```

3.  **Run the Application (Development):**
    Execute the following command from the `FileCollector/` directory:
    ```bash
    dotnet run --project FileCollector/FileCollector.csproj
    ```

4.  **Publish for Distribution:**
    To create distributable, self-contained applications:
    ```bash
    # Example for Windows x64:
    dotnet publish FileCollector/FileCollector.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true

    # Example for Linux x64:
    dotnet publish FileCollector/FileCollector.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true

    # Example for macOS x64 (Intel):
    dotnet publish FileCollector/FileCollector.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true

    # Example for macOS ARM64 (Apple Silicon - M1/M2/M3):
    # dotnet publish FileCollector/FileCollector.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true
    ```
    The published application will be in `FileCollector/bin/Release/net9.0/<runtime-identifier>/publish/`.
    *   `--self-contained true`: Bundles the .NET runtime with your app.
    *   `-p:PublishSingleFile=true`: Creates a single executable (may require `IncludeAllContentForSelfExtract` on some platforms/versions for assets).
    *   `-p:IncludeAllContentForSelfExtract=true`: Ensures assets like `wwwroot` are bundled into the single executable.

## How to Use

1.  **Launch File Collector:** Run the application executable.
2.  **Select Root Folder:** Click "Select Root Folder". A system dialog will prompt you to choose the main directory for your work.
3.  **Navigate and Select Files/Folders:**
    *   The file tree of the selected folder populates the left panel.
    *   Use expander icons (▶ or ▼) next to directories to toggle visibility of their contents.
    *   Utilize checkboxes to select files or entire directories. Files within selected directories (if not ignored) are automatically included.
    *   The "Selected Files" list (below the tree) shows a count and names of only the *individual files* marked for merging.
4.  **Configure Global Settings (Optional but Recommended):**
    *   Access the "Settings" page via the sidebar.
    *   **Ignore Patterns:** Add or remove patterns (e.g., `obj/`, `*.tmp`, `!src/important.cs`). You can also paste content from a `.gitignore` file or upload one.
    *   **Prompts:** Define a "Pre-Prompt" (text prepended to all files) and/or a "Post-Prompt" (text appended after all files, before the User Prompt).
    *   Click **"Save All Settings"** to persist these configurations.
5.  **Manage Contexts (Optional):**
    *   On the main page, use the "Saved Contexts" section to save the current root folder and selected files under a specific name.
    *   Later, you can quickly load a saved context to restore that selection state.
6.  **Add a User Prompt (Optional, Session-Specific):**
    *   On the main page, within the "Merged Content" panel, type a "User Prompt". This prompt is temporary and applies only to the current session. It is inserted after all file content and the Post-Prompt.
    *   The merged content view refreshes automatically shortly after you stop typing in this field.
7.  **Generate/Refresh Merged Content:**
    *   If auto-refresh hasn't triggered after selection changes or settings modifications, click the "Refresh" button in the "Merged Content" panel.
8.  **Review and Copy Output:**
    *   The right-hand panel displays the syntax-highlighted merged content. Each file's content is clearly demarcated by its relative path.
    *   If any files were unreadable, an error message will appear in their place.
    *   Click "Copy" to transfer the entire plain text content to your clipboard. A "Copied!" confirmation will briefly appear.
9.  **Clear Selections:**
    *   Use the "Clear Selection" button to deselect all items in the file tree and reset the merged content view.

## Configuration Details

### Application Settings (`appsettings.json`)

File Collector stores persistent settings in an `appsettings.json` file, typically located in `AppContext.BaseDirectory` (usually the application's executable directory). This JSON file includes:

*   `AppSettings`:
    *   `IgnorePatterns`: An array of strings for filtering files (e.g., `["bin/", "obj/", "*.log"]`).
    *   `PrePrompt`: A string for text to prepend to the merged output.
    *   `PostPrompt`: A string for text to append (before User Prompt).
    *   `SavedContexts`: An array of objects, each representing a saved selection context (root path and selected file paths).

These settings are primarily managed through the "Settings" UI and "Saved Contexts" UI within the application.

### Ignore Patterns Explained

The ignore pattern system is essential for tailoring file collection. It closely follows `.gitignore` syntax:

*   **Comments:** Blank lines or lines starting with `#` are ignored.
*   **Basic Patterns:**
    *   `*.log`: Ignores all files ending with `.log`.
    *   `temp/`: Ignores any directory named `temp` and all its contents, at any level.
    *   `/temp/`: Ignores a `temp` directory *only* at the root of your selected folder.
    *   `docs/*.md`: Ignores all Markdown files directly within any `docs` directory.
*   **Directory Separators:** Always use forward slashes (`/`) in patterns. The application normalizes paths internally.
*   **Negation (`!`):** A pattern starting with `!` re-includes a file if it was excluded by a previous pattern. Order matters.
    *   Example:
        ```
        logs/
        !logs/important.log
        ```
*   **Double Asterisk (`**`):**
    *   `**/foo`: Matches `foo` in any subdirectory.
    *   `abc/**`: Matches everything inside `abc/`, recursively.
    *   `a/**/b`: Matches files or directories `b` anywhere inside `a`, with any number of intermediate directories.
*   **Pattern Specificity & Anchoring:**
    *   `foo`: Ignores files or directories named `foo` anywhere.
    *   `/foo`: Ignores `foo` only at the root of the selected folder.
    *   `foo/`: Specifically targets directories named `foo` (and their contents).
    *   `foo/*`: Ignores all files and directories *directly* inside any directory named `foo`, but not `foo` itself.

The filtering logic diligently attempts to replicate common `.gitignore` behavior to provide a familiar and powerful exclusion mechanism.

## Known Issues & Limitations

*   **Large Number of Files:** Performance might degrade when selecting a root folder containing an exceptionally large number of files and directories (e.g., hundreds of thousands) during initial tree loading and filtering.
*   **Extremely Large Individual Files:** Merging very large individual files (e.g., >50MB) might impact UI responsiveness during content loading and highlighting. The primary use case is for text-based source code and documents.
*   **Binary File Content:** While binary files can be selected, their content will be displayed as-is (or with read errors if unreadable as text), which may not be useful in the merged output. Syntax highlighting will not apply.
*   **Photino Dialogs:** System dialogs (folder picker) are basic and provided by Photino. Advanced dialog features are not available.
*   **Symlink Behavior:** Behavior with complex symbolic link structures (especially loops or links outside the primary selected path) might vary and is not extensively tested. Standard symlinks to files/directories within the selected scope should generally work.

## Future Enhancements

We are considering several enhancements for future releases:

*   **Advanced Search/Filter in File Tree:** Allow users to quickly find specific files or filter the tree view by name or extension.
*   **Drag & Drop Root Folder Selection:** Implement drag-and-drop functionality to set the root folder.
*   **Multiple Root Folder Support:** Allow users to add and manage selections from multiple, disparate root folders simultaneously.
*   **Customizable File Delimiters:** Provide options to change the format of `// File:` and `// End of file:` comments.
*   **Token Count Estimation:** Display an estimated token count for the merged content, useful for LLM context limits.
*   **Export/Import of `appsettings.json`:** Allow users to back up and restore their complete application configuration.
*   **More Robust `.gitignore` Parsing:** Continuously refine the ignore pattern engine for even closer parity with complex `git` behaviors.
*   **Accessibility Improvements:** Ongoing review and enhancements for WCAG compliance.

## Contributing

Contributions are highly welcome! Whether it's bug fixes, feature implementations, or documentation improvements, your input is valuable.

1.  **Fork the Repository:** Click 'Fork' on the GitHub page.
2.  **Clone Your Fork:** `git clone https://github.com/YOUR_USERNAME/FileCollector.git`
3.  **Create a Feature/Bugfix Branch:** `git checkout -b feature/your-great-idea` or `bugfix/fix-that-issue`
4.  **Implement Changes:** Write your code and ensure it adheres to existing project style.
5.  **Commit Changes:** `git commit -m "Add: Brief description of your change"`
6.  **Push to Your Branch:** `git push origin feature/your-great-idea`
7.  **Open a Pull Request:** Submit a PR against the `main` branch of the original `lorenzodimauro97/FileCollector` repository.

Please provide a clear description of your changes in the pull request. For new features, consider if unit tests are applicable.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for full details.