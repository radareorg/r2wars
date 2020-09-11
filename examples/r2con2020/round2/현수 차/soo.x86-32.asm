call get_eip
get_eip:
pop esp
sub ecx, 0x20
dec eax

qq:


pushad
pushad
pushad

add esp, ecx
dec ecx
cmp esp, 0x80
ja qq
mov esp, 0x3a0
jmp qq
