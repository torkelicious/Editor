# tangentröra

trashy text editor made in c#

runs in the cli

accepts file paths as cli arguments

pseudo-modal text editor

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
X: Delete char on cursor
D: Delete Line
O: Insert into NewLine
Q: Quit
Y: Copy Line to internal editor clipboard
P: Paste from internal editor clipboard
ESCAPE: Exit Insert mode
Undo/Redo with U / R
You can navigate quickly with TAB and SHIFT+TAB (or W and B)
G: Goes to start of buffer, SHIFT+G: goes to end of buffer
```

if you want to try it, you should build it from source. its a small project, but im too busy/lazy/forgetfull to keep any
binaries updated.

Pre-built binaries can be found at https://github.com/torkelicious/Editor/releases/latest
