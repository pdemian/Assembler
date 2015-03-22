.lang "x86"
.file "test.c"
.subsystem "GUI"
.include "kernel32"
.include "user32"

.data
	SimplePE = "A simple PE Executable"
	HelloWorld = "Hello World!"	

.text
main:
	push 0
	push SimplePE
	push HelloWorld
	push 0
	call MessageBoxA
	push 0
	call ExitProcess