call getpc
getpc:
  pop edi
  add edi, 128
loop:
  mov dword ptr [edi], 0xcccccccc
  add edi, 16
  jmp loop
