; her isimizde hayirlisi olsun
; pushad loopunu 0x14 civarına unpack ettikten sonra
; oraya ziplayip 0x400'den geriye pushad yapmaya baslar
; v3: a longer pushad init
pack:       
;        mov edi, 0xcccccccc
;        mov esi, 0xcccccccc
;        mov ebp, 0xcccccccc
        mov ebx, 0x00000400
        mov edx, 0x6060dc89
        mov ecx, 0x60606060
        mov eax, 0xf6eb6060
unpack:
        mov esp, 0x220
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        pushad
        add esp, 0x14 ; if shorter, tune this
        jmp esp
