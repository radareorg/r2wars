# initial setup
adr x0, start
add x0, x0, 0x23
movz x3, 0x1d8
movz x4, 0x200
ldp x1, x2, [x0], 16
ldp x8, x9, [x0], 16
stp x1, x2, [x4]
stp x8, x9, [x4], 16
br x4



loop:
stp x1, x2, [x3], 0x10
stp x8, x9, [x3], -0x38
tbz x3, 30, -12

# if we got below 0x0:
add x3, x3, 0x3f8
stp x1, x2, [x3, 0x10]!
sub x3, x3, 0x28
b 0x3cc
