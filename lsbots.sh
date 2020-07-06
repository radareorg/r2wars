#!/bin/sh
(
for  a in warriors/* ; do
	if [ "`echo $a | grep txt$`" ]; then
		continue;
	fi
	printf "$a   "
	if [ "`echo $a | grep x86-32`" ]; then
		rasm2 -a x86 -b 32 -f $a |wc -c
	elif [ "`echo $a | grep arm-64`" ]; then
		rasm2 -a arm -b 64 -f $a |wc -c
	elif [ "`echo $a | grep arm-16`" ]; then
		rasm2 -a arm -b 16 -f $a |wc -c
	elif [ "`echo $a | grep mips-32`" ]; then
		rasm2 -a mips -b 32 -f $a |wc -c
	elif [ "`echo $a | grep 8051`" ]; then
		rasm2 -a 8051 -f $a |wc -c
	fi
done
) |awk '{print $2"\t"$1}'
