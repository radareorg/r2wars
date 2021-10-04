;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; 	bot for r2wars   :: r2con2020 ::   ;;
; 	_goyo_                             ;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

.arch arm
.bits 32

.hex 00D04FE2		; SUB 		SP,R15,0x00
.hex FF5F9DE8           ; LDMFD   	SP,{R0-R12,R14} 

sp_top:							
.hex 050D5DE3       	; r0	CMP 	SP,0x140

top_from_subs:						
.hex FFFF2DA9		; r1 	PUSHGE 	{R0-R15}
.hex FFFF2DA9       	; r2	PUSHGE  {R0-R15}
.hex FFFF2DA9       	; r3	PUSHGE 	{R0-R15}
.hex FFFF2DA9       	; r4	PUSHGE 	{R0-R15}
.hex FFFF2DA9       	; r5	PUSHGE 	{R0-R15}
.hex 0DF0A0C1		; r7   	MOVGT 	R15,SP 		; if SP>0x140
.hex 0ED0E0D1      	; r6	MVNLE   SP,R14 		; time to go, sp low value
.hex FFFF2DE9		; r8   	PUSH    {R0-R15}	; hole starting from 0x400
.hex 28F05FE2		; r9   	SUBS 	R15,R15,0x28 	; clear flag for pushge
.hex FFFFFFFF		; r10   
.hex cccccccc		; r11  	
.hex ccc3ccc3		; r12  	 
.hex fffbffff		; r14     
