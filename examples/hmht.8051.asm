	mov	dptr, #kill
loop:
	inc	dptr
	inc	dptr
	inc	dptr
	inc	a
	movx	@dptr, a
	sjmp	loop
kill:
