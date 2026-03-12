# 006 - Broader Preview Support

## Status: COMPLETE

## Summary

Enhance the Preview Panel to provide richer previews for Markdown and source code files. Instead of plain text, Markdown should be rendered with basic formatting (bold, headers, lists) and source code should feature basic syntax highlighting for common languages.

## Problem

The current Preview Panel treats all text-based files as plain text in a standard `TextBox`. This makes it difficult to:
- read Markdown documentation with intended formatting
- quickly scan source code without syntax coloring
- distinguish between different types of structured data (JSON, XML)

## Proposed Changes

### 1. UI: RichTextPreview
- Replace or supplement the `TextBox` in `MainWindow.xaml` with a `RichTextBox` (or a `FlowDocumentViewer`) to support formatted text.
- Add a new `PreviewFlowDocument` property to `MainViewModel` to hold the formatted content.
- Add `HasPreviewRichText` to control visibility.

### 2. Markdown Rendering
- Implement a simple internal Markdown-to-FlowDocument converter.
- Support:
  - Headers (#, ##, ###)
  - Bold (**text**) and Italic (*text*)
  - Bulleted lists (-)
  - Code blocks (```)
- Scope: Keep it minimal and dependency-free as per project constraints.

### 3. Syntax Highlighting
- Implement a basic syntax highlighter using Regex for common languages:
  - C# (.cs)
  - XML/XAML (.xml, .xaml, .csproj)
  - JSON (.json)
  - Python (.py)
- Support:
  - Keywords
  - Comments
  - Strings
  - Numbers

### 4. Integration
- Update `UpdatePreview` in `MainViewModel` to detect Markdown or Code extensions and route to the rich preview logic.
- Ensure the 1 MB limit still applies for performance.

## Acceptance Criteria

- [x] Files with `.md` extension are rendered with bold headers and formatted lists in the preview panel.
- [x] Files with `.cs`, `.json`, `.xml`, `.py` extensions show basic color coding for keywords and comments.
- [x] Large files (> 1 MB) still show a "too large" message instead of attempting expensive rendering.
- [x] Switching between files remains fast and does not leak memory or block the UI thread.
- [x] Smoke tests verify that rich preview properties are populated for relevant extensions.

<!-- NR_OF_TRIES: 1 -->
