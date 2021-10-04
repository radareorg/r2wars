; bc0004000089e5b860606060bb60606060b9e4ffe4ffba10fc8300be0f44e500bf606060606060606060606060ffe483fc100f44e5ebe
start:
    mov esp, 0x400

    ; max bound
    mov eax, esp

    ; another code
    mov ecx, 0xc3c3c3c3 ; ret - i386
    mov edx, 0xd65f03c0 ; ret - aarch64  
    mov ebx, 0xe12fff1e ; bx lr - arm32

    ; main program 
    mov ebp, 0xe4ff6060
    mov esi, 0x6060e046
    mov edi, 0x0f60fc83

; 83fc20 - cmp esp, 0x20
; 0f46e1 - cmovbe esp, eax
; ffe4   - jmp esp

    pushad
    jmp esp