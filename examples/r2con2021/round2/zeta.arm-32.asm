_start:
    ldr r0, [pc, #48]
    ldr r1, [pc, #48]
    ldr r2, [pc, #48]
    ldr r3, [pc, #44]
    ldr r4, [pc, #40]
    ldr r5, [pc, #36]
    ldr r6, [pc, #36]

    movt r7, #0xffff
    movt r8, #0xffff
    movt r9, #0xffff
    movw r10, #256

    movw sp, #0x0400
    push {r0, r1, r2, r3, r4, r5, r6, r7, r8, sb, sl, fp, ip, sp, lr, pc}
    bx sp

_data:
    cmp sp, r10
    movlt sp, 0x400
    push {r0, r1, r2, r3, r4, r5, r6, r7, r8, sb, sl, fp, ip, sp, lr, pc}
    bx sp
