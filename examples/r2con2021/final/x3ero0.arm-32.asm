_start:
     add r14, pc, regs
    .hex ff3f9ee8 ; ldm r14, {r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, sl, fp, ip, sp}

x3ero0:
    .hex 0de01de0 ; ands lr, sp, sp
    .hex 0cd0e001 ; mvneq    sp, ip
    push {r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, r13, r14, r15}
    .hex 0cd0e0e1 ; mvn sp, ip
regs:
    mov pc, 0x3c0


    .hex 00000000
    .hex 0de01de0
    .hex 0cd0e001
    .hex ffff2de9
    .hex ffff2de9
    .hex ffff2de9
    .hex ffff2de9
    .hex ffff2de9
    .hex ffff2de9
    .hex ffff2de9
    .hex ffff2de9
    .hex 1dff2fe1
    .hex 10000000
    .hex fffbffff ; ip
    .hex 00040000 ; sp
