# tangentröra

trashy text editor made in c#

runs in the cli

accepts file paths as cli arguments

modal text editor

---
sometimes the wheel needs to be reinvented
... and sometimes even really poorly when im bored...

This is mostly a learning project

---

```
controls:
HJKL to move (Arrow keys in INSERT mode)
I: Enter Insert mode 
A: Append (enter Insert after cursor)
SHIFT + A: Append at end of line 
V: Enter Visual Mode
X: Delete char on cursor
D: Delete Line
O: Insert into NewLine
Q: Quit
Y: Copy Line to internal editor clipboard
P: Paste from internal editor clipboard
CTRL+S: Quicksave
ESCAPE: Return to Normal mode
Undo/Redo with U / R
You can navigate quickly with TAB and SHIFT+TAB (or W and B)
G: Goes to start of buffer, SHIFT+G: goes to end of buffer
```

Configuration file is located at:

**Linux / Unix-likes:**

```
~/.config/tgent/config.json
```

**Windows:**

```
%APPDATA%\tgent\config.json
```

**macOS:**

```
~/Library/Application Support/tgent/config.json
```

### detailed information can be found in help.md

https://github.com/torkelicious/Editor/blob/main/help.md

---

if you want to try it, you should build it from source. its a small project, but im too busy/lazy/forgetfull to keep any
binaries updated.

See `/scripts` for more information on building from source:
https://github.com/torkelicious/Editor/tree/main/scripts

Pre-built binaries can be found at https://github.com/torkelicious/Editor/releases/latest
