_start:
	.hex e50325aa ; mvn x5, x5
	.hex e60326aa ; mvn x6, x6
	adr x3, loop
	adr x4, bot_end
	mov x9, 0x3d0
	mov x0, x3
	mov x1, x4
loop:
	; cmp x0, x4
	; .hex 0020839a ; csel x0, x0, x3, cs
	; cmp x1, x3
	; .hex 2120849a ; csel x1, x1, x4, cs
	and x0, x0, x9
	.hex 0518bea9 ; stp x5, x6, [x0, -32]!
	.hex 0518bfa9 ; stp x5, x6, [x0, -16]!
	; sub x0, x0, 32
	
	and x1, x1, x9
	.hex 251882a9 ;	stp x5, x6, [x1, 32]!
	.hex 251881a9 ;	stp x5, x6, [x1, 16]!
	; sub x0, x0, 0x10
	; add x1, x1, 32
	; cmp x0, 0xf
	; .hex 00808b9a ; csel x0, x0, x11, hi
	; .hex 3fc40ff1 ; cmp x1, 0x3f1
	; .hex 21308b9a ; csel x1, x1, x11, cc
	b loop
	 


bot_end:
