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
    mov ebp, esp
    sub esp, 272
    
    ;print Type in your first name
    push String1
    call printf
    add esp,4
    
    ;Get name
    ;fgets(c, 256, stdin);
    call __iob_func
    mov ecx, 0x20
    mov edx, 0
    imul edx, ecx
    add eax, edx
    ;stdin
    push eax
    ;256
    push 0x100
    ;c
    lea eax, [ebp-264]
    push eax
    call fgets
    add esp, 12
    
    ;print Your name is %d characters in length
    lea eax, [ebp-264]
    push eax
    call strlen
    add esp, 4
    sub eax, 1
    ;%d
    push eax
    ;string
    push String2
    call printf
    add esp, 8
    
    xor eax,eax
    leave
    ret
    