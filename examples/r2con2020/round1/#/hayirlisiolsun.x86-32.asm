pack:       
;        mov edi, 0xcccccccc
;        mov esi, 0xcccccccc
;        mov ebp, 0xcccccccc
        mov ebx, 0x00000400
        mov edx, 0x6060dc89
        mov ecx, 0x60606060
        mov eax, 0xf9eb6060
unpack:
        mov esp, 0x20
        pushad
        add esp, 0x14 ; if shorter
        jmp esp
loop: 
        inc eax
        jmp loop

        
