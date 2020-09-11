call init

syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall

init:
  mov eax, 0x340
  mov ebx, 0xc3c3c3c3
  mov ecx, ebx ; trash
  mov edx, 0x400 ; end of arena

  mov edi, 0xd439c401
  mov esi, 0x60e04f0f
  mov ebp, 0xe4ff6060

  call yolo_1

syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall

yolo_1:
  mov esp, 0x210
  pushad
  mov esp, 0x66
  pushad
  mov esp, 0x3f9
  pushad
  call yolo_2

syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall

yolo_2:
  mov esp, eax ; relocate to `eax`
  pushad
  pushad
  mov eax, 0x99 ; first address after reloc
  jmp esp

syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
syscall
