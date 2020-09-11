; when i say jump -- you say how high

mov eax, 0xffffffff
mov ecx, eax
mov edx, eax
mov ebx, eax
mov ebp, eax
mov esi, eax

check:
    mov edi, 0x000
    cmp [edi], 0
    jne planb
    mov esp, 0x400
    inc edi
    mov [edi], 0xe7ff6060
    jmp edi

planb:
    mov edi, 0x3fb
    mov [edi], 0xe7ff6060
    mov esp, 0x3fa
    jmp edi
