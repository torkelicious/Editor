# TangentRöra/help

*This software is Licensed under:
SPDX-License-Identifier: GPL-3.0-or-later*

by torkelicious

updated on the *4th of August 2025*

---

## NORMAL MODE

Default mode for Navigation and Editing

### Movement

- `H`, `J`, `K`, `L` or Arrow Keys - Move cursor left, down, up, right
- `G` - Go to start of document
- `Shift+G` - Go to end of document
- `Tab` / `W` - Jump right (4 spaces or next tab stop)
- `Shift+Tab` / `B` - Jump left (4 spaces)
- `Page Up` / `Page Down` - Scroll half screen up/down

### Mode Changes

- `I` - Enter Insert mode at cursor
- `A` - Enter Insert mode after cursor (Append)
- `V` - Enter Visual mode for text selection
- `O` - Insert new line below and enter Insert mode

### Editing

- `X` - Delete character at cursor
- `D` - Delete entire current line
- `Y` - Yank (copy) current line to editor clipboard
- `P` - Paste from clipboard

### Undo/Redo

- `U` - Undo last action
- `R` - Redo last undone action

### File Operations

- `Ctrl+S` - Quick save file
- `Q` - Quit editor

## INSERT MODE

Text input mode

### Text Input

- Any printable character - Insert character at cursor position
- `Enter` - Insert new line
- `Tab` - Insert 4 spaces (indentation)

### Navigation

- Arrow Keys - Move cursor in any direction

### Editing

- `Backspace` - Delete character before cursor
- `Delete` - Delete character at cursor

### Mode Change

- `ESC` - Return to Normal mode

## VISUAL MODE

Text selection mode

### Selection

- `H`, `J`, `K`, `L` or Arrow Keys - Extend selection in direction
- `G` - Extend selection to start of document
- `Shift+G` - Extend selection to end of document
- `Tab` / `W` - Extend selection right (4 spaces)
- `Shift+Tab` / `B` - Extend selection left (4 spaces)

### Operations on Selection

- `Y` - Yank (copy) selected text to editor clipboard
- `D` / `X` - Delete selected text

### File Operations

- `Ctrl+S` - Quick save file

### Mode Change

- `ESC` - Return to Normal mode (clear selection)

---