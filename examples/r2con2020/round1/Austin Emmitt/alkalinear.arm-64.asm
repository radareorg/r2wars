sub x20, x20, 16;
movk x21, 0x1ff;

bl start;
start:
movk x22, 0x8a;
strb w22, [x30, 23];    // patch ORR -> AND
ldp x0, x1, [x30, 16];  // copy
ldp x2, x3, [x30, 32];  // copy

add x20, x20, 32;
orr x20, x20, x21;      // becomes AND
stp x0, x1, [x20, 120]; // stomp
stp x0, x1, [x20, 480]; // stomp
stp x2, x3, [x20, 496]; // stomp
stp x0, x1, [x20, 0];   // paste
stp x2, x3, [x20, 16];  // paste
br x20;