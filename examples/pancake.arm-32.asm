mov r0, pc
loop:
  str r0, [r0, 0]
  sub r0, r0, 8
  b loop
