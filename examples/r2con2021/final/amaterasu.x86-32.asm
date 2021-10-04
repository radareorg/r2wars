start:
    mov ebp, 0x3e0
    mov esp, 0x3e0
    ; lea eax, [end + 0x20]
    ; lea ebx, [start - 0x20]
    mov eax, 0xffffffff
    mov ebx, 0xffffffff
    mov ecx, 0xffffffff
    mov edx, 0xffffffff
    mov edi, 0xffffffff
    mov esi, 0xffffffff

bot_loop:
    ; cmp esp, eax
    ; cmovbe esp, ebx

    ; push 128 bytes at once
    pushad
    pushad
    pushad
    pushad

    ; push again
    pushad
    pushad
    pushad
    pushad

    ; jmp to beginning once again
    cmp esp, 0x10
    cmovz esp, ebp
    jmp bot_loop

end:
    nop