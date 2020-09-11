; r2wars warrior by zutle
; >> So you want to play the pusha game? :D
;
; The logic below generates the following code:
;
;
;payload:
;	test sp, sp
;	jnz replicate
;	mov sp, 0x400
;replicate:
;	pushal
;	pushal
;	pushal
;	pushal
;	pushal
;	pushal
;	pushal
;	pushal
;	mov [esp + 0xc], ecx
;	mov eax, esp
;	jmp eax

	call start
start:
	pop esp
	and esp, 0xffffff00
	add esp, 0x20
	mov edx, 0xe0ffe089
	mov ebx, 0x0c244c89
	mov ecx, 0x60606060
	mov ebp, 0x60606060
	mov esi, 0x0400bc66
	mov edi, 0x0475e485
	pushal
	mov [esp + 0xc], ecx
	mov eax, esp
	jmp eax
