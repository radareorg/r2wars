call getpc
getpc:
  pop eax
loop:
  sub eax, 8
  mov dword ptr [eax], 0
  jmp loop
