.lang "x86"
.file "test.cf"
.subsystem "CUI"
.include "msvcrt"
.include "kernel32"

.data
	HelloWorld = "Hello World!\n"

.text
PrintValue:
	push ebp
	mov ebp, esp
	mov eax, [ebp+8]
	push eax
	call puts
	add esp, 4
	mov esp, ebp
	pop ebp
	ret

main:
	push ebp
	mov	ebp, esp
	
	push HelloWorld
	call PrintValue
	add esp, 4
	
	xor eax, eax
	push eax
	call ExitProcess