;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; 	bot for r2wars   :: r2con2021 ::   ;;
; 	_goyo_                             ;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

.arch arm
.bits 32


.hex 08D08FE2           ; 	ADD     SP,R15,0x08		  
								 
.hex FF5F9DE8           ; 	LDMFD 	SP,{R0-R12,R14}    	; loading. 

.hex 2DB35CE0         	;       SUBS    R11,R12,SP, LSR #6	; return a memory map zone value [-10 -> +6]
.hex 0BF18DA0 		; 	ADDGE  	R15,SP,R11, LSL #2  	; pushes 1,2,3,4,5,or 6 or nothing, worst case when pc < 0x40
start:
.hex FFFF2DE9		; r0	PUSH    {R0-R15} 		;  
.hex FFFF2DE9		; r1 	PUSH 	{R0-R15}		;  
.hex FFFF2DE9       	; r2	PUSH 	{R0-R15}		; 1 to 6 pushes, R11 controlled 
.hex FFFF2DE9       	; r3	PUSH 	{R0-R15}		;
.hex FFFF2DE9       	; r4	PUSH 	{R0-R15}		;
.hex FFFF2DE9       	; r5	PUSH 	{R0-R15}		;

.hex 2DB35CE0           ; r6    SUBS    R11,R12,SP, LSR #6      ; condition check
.hex 0DF0A0D1           ; r7    MOVLE   R15,SP                  ; SUBLE  R15,SP,R11, LSL #6 another posibility.
.hex 0BF18D20           ; r8    ADDCS   R15,SP,R11, LSL #2      ; time to go, sp low value
.hex 0ED0E0E1           ; r9    MVN     SP,R14                  ; sp = 0x400 again
.hex 8CF15FC0           ; r10   SUBSGT  R15,R12, LSL #3         ; back to pushes
.hex 00000000           ; r11   data
.hex 06000000           ; r12   data
.hex fffbffff           ; r14   invalid                         ; bad instrucction, neg(0xfffffbff) == 0x400 == stack top 







