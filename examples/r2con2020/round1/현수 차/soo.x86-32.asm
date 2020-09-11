call get_eip

get_eip:

pop esp


qq:

mov [esp+0x30], esp

pushad

pushad

pushad

popad

popad

popad
add esp, ecx

dec ecx
cmp esp, 0x80
ja qq
mov esp, 0x3a0
jmp qq
