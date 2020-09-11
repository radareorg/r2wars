adr x0, start
add x0, x0, 0x23
movz x3, 0x1d8
movz x4, 0x200
.hex 0108C1A8  // ldp x1, x2, [x0], 16
.hex 0824C1A8  // ldp x8, x9, [x0], 16
.hex 810800A9  // stp x1, x2, [x4]
.hex 882401A9  // stp x8, x9, [x4], 16
br x4



loop:
.hex 610881A8   //stp x1, x2, [x3, 0x10]!
.hex 68A4BCA8   //stp x8, x9, [x3, 0x10]
.hex 83FEF736   //tbz x3, 30, -12



# ---- if loop:
add x3, x3, 0x3f8
.hex 610800A9   //stp x1, x2, [x3, 0x10]!
sub x3, x3, 0x28
b 0x3cc
