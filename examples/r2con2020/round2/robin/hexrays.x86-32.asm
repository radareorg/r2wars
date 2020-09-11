; This bot sucks but I wamted to make something
mov esp, 0x3de
mov edx, 0xdead
mov ebx, 0xdead
mov ecx, 0xdead
jmp loop
hijack:
    jmp eax
loop:
    inc al
    cmp eax, 0x3de
    jle continue
    xor eax, eax
continue:
    cmp [eax], 0x00
    jne hijack
    pushal
    jmp loop
