;.arch arm
.bits 32

.hex 00D04FE2		; SUB 		SP,R15,0x00
.hex FF5FBDE8		; POP		{R0-R12,R14}
.hex 050D5DE3       	; r13	CMP 	SP,0x140
.hex FFFF2DA9		; r14 	PUSHGE 	{R0-15}
.hex FFFF2DA9       	; r12	PUSHGE
.hex FFFF2DA9       	; r12	PUSHGE
.hex FFFF2DA9       	; r11	PUSHGE
.hex FFFF2DA9       	; r10	PUSHGE
.hex 01DBA0D3      	; r9	orrle 	sp,r14,sp
.hex 0DF0A0C1		; r8   	MOVGT 	R15,SP #jump if SP>=0  
.hex 24F05FE2		; r6   	SUBS 	R15,R15,0x24
.hex 11111111		; r5   	
.hex 22222222		; r4   	
.hex cccccccc		; r3   	
.hex 44444444		; r2   	
.hex 00040000		; r1


