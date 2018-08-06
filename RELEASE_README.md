
             ____ ____    __  __   _   ___ _____
            | _  |__  \ _|  ||  |/'  \| _ '  __/
            |   ( / __||_|      |  =  |   (___ \
            |_|\_|____|   \_/\_/|_| |_|_|\_\___/
          
                  -- skuater && pancake --

r2wars is a corewars-style game using radare2 as backend and
supporting other architectures with ESIL as emulation engine.

Introduction
------------

In order to start the competition you need to run r2wars.exe
which is written in C# and runs on Mono, .NET/Core or MSCLR.

The UI has been rewritten from the original MFC to be in HTML,
so the program will run a local webserver which serves a
website with few buttons to control the execution of the
tournament.

On Linux/Mac it will use r2 from $PATH, on Windows you need
to copy `rasm2.exe` and `radare2.exe` in the same directory.

Gameplay
--------

Create a directory and put .asm files inside in order to load
them into shared memory space allocated by r2wars.

Before the `.asm` the file must specify the arch-bits setup.

    - .x86-32.asm for x86 32 bits warriors
    - .arm-32.asm for arm 32 bits warriors

There must be at least 2 warriors to start the game.

In case of finding warriors of more than one architecture it
will prompt the user for confirmation. Because this version
of r2wars allows to run programs written to run on different
CPUs to be executed in the same memory address space.
