; when i say jump -- you say how high

mov eax, 0xfeebfeeb; feebfeeb
mov ebx, eax
mov ecx, eax
mov edx, eax
mov ebp, eax
mov edi, eax
mov esp, 0x3fc
mov esi, 0x3fd
mov [esi], 0xe6ff60
jmp esi
