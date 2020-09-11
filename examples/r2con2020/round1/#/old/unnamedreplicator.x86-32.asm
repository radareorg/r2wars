        call init
init:   
        pop esp
        add esp, 0x26
        mov dword [eax], esp ;  esp saved @0x0
        sub esp, 0x20
save:
        popad
        jmp restore
        .hex 0xcccccccc
        .hex 0xcccccccc
        .hex 0xcccccccc
        .hex 0xcccccccc
restore:
        mov esp, dword [0x0]
        add esp, 0x20
        xor esi, esi
        mov dword [esi], esp
        pushad
