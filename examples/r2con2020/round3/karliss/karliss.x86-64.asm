lea rsp, [0x25]
mov rbx, qword[rsp]
mov rcx, qword[rsp+8]
mov r10, 0x25f
mov r11, 0x3f0
mov r12, 0x10

add rsp, r10
and rsp, r11
add rsp, r12
push rcx
push rbx
jmp rsp
nop
nop
nop
nop
nop
nop
