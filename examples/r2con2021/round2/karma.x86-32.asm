mov ebp, 0x400
mov esp, ebp
mov esi, 0x0f0b0f0b
mov edi, 0x0f0b0f0b
mov ecx, 0x0f0b0f0b
mov edx, 0x0f0b0f0b
mov ebx, 0x0f0b0f0b
mov eax, 0x0f0b0f0b
start:
    pushad
    sub esp, 0x20
    and esp, 0x3ff
    jmp start