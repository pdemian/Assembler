.lang "x86"
.file "test.cf"
.subsystem "CUI"
.include "msvcrt"
.include "kernel32"

.data
	String1 = "Type in your first name: "
	String2 = "Your name is %d characters in length."

.text
main:
	push ebp
	mov	ebp, esp
	sub	esp, 272
	
	;print Type in your first name
	mov	[esp], String1
	call printf
	
	;Get name
	mov	eax, _iob
	mov	[esp+8], eax
	mov	[esp+4], 256
	lea	eax, [esp+16]
	mov	[esp], eax
	call fgets
	
	;print Your name is %d characters in length
	lea	eax, [esp+16]
	mov	[esp], eax
	call strlen
	sub eax, 1
	mov	[esp+4], eax
	mov	[esp], String2
	call printf
	
	xor eax,eax
	leave
	ret
	
	
	