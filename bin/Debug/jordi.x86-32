call label
label:
	pop eax
loop:
	sub eax, 10
	cmp dword ptr[eax], 0
je loop
	mov dword ptr[eax], 0
	jmp loop
