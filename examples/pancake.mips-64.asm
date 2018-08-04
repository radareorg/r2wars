bal 0 + getpc
getpc:
  move v0, ra
  lui v1, 1
loop:
  sw v0, 0(v1) 
  addiu v1, v1, v0
  b loop
  nop
