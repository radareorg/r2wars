; her isimizde hayirlisi olsun
; pushad loopunu 0x14 civarına unpack ettikten sonra
; oraya ziplayip 0x400'den geriye pushad yapmaya baslar
; v3: a longer pushad init
pack:       
        mov edi, 0x00000280
        mov esi, 0x6060fc89
        mov ebp, 0x60606060
        ; esp
        mov ebx, 0x60606060
        mov edx, 0x60606060
        mov ecx, 0x0400bc60
        mov eax, 0xeaeb0000
unpack:
        mov esp, 0x020
        pushad
        mov dword [esp + 4 + 8], 0x60606060
        add esp, 0x4 ; if shorter, tune this
        jmp esp
