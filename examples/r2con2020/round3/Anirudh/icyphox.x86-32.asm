; when i say jump -- you say how high

mov eax, 0xffffffff
mov ecx, eax
mov edx, eax
mov ebx, eax
mov ebp, eax
mov esi, eax

mov edi, 0x000
mov esp, 0x400
mov [edi], 0x20fc8360
mov [edi+4], 0xff600374
mov [edi+8], 0x0400bce7
mov [edi+12], 0xe7ff0000
;mov [edi], 0xe7ff6060
jmp edi
