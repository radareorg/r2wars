; "her isimizde hayirlisi olsun"

; a replicator bot. This one unpacks it's main loop payload
; around 0xc and jumps to there. In the loop, it first fills
; upper half of the arena then bottom half of the arena using
; pushad's.
;
; v1: initial; pushad from 0x400 through 0x0                       result: 5 of 9
; v2: I don't remember                                             not competed
; v3: a longer pushad sequence in init phase for quickscoping      result: 5 of 13 && 8 of 14
; v4: the longest packed loop using all registers for just tryin'. result: 10 of 15
;
;
; pack contents are below 
;;;;;;;;;;;;;;;;;;;;;;;;;;
;                mov esp, edi
;        loop:
;                pushad
;                pushad
;                pushad
;                pushad
;                pushad
;                pushad
;                pushad
;                pushad
;                pushad
;                pushad
;                pushad
;                pushad
;                pushad
;                pushad
;                pushad
;                mov esp, 0x400
;                jmp loop

pack:       
        mov edi, 0x00000280
        mov esi, 0x6060fc89
        mov ebp, 0x60606060
        ; esp popped here as 0x20 (a bad instruction). must be corrected in unpack loop
        mov ebx, 0x60606060
        mov edx, 0x60606060
        mov ecx, 0x0400bc60
        mov eax, 0xeaeb0000
unpack:
        mov esp, 0x020
        pushad
        mov dword [esp + 4 + 8], 0x60606060 ; fix popped esp value
        add esp, 0x4 ; tune this according to pack length
        jmp esp
