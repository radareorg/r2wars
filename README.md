# r2wars

![Alt text](csharp/resources/r2wars_logo_transparent.png?raw=true "r2wars Logo")

The C# implementation of the r2wars tournament competition

Tournament api from: https://github.com/otac0n/tournaments

## Description

r2wars is a game similar to corewars, where 2 programs run on a
shared memory space trying to catch each other in order to trash
their code and make them crash.

This game was initially developed by pancake as a PoC in here

* https://github.com/radare/radare2-extras/tree/master/r2wars

You can refer to the r2wars For N00bs talk from r2con2020 for more information:

* https://www.youtube.com/watch?v=PB0AFBqFwGQ

Furthermore, you can find an explanation of the game in the first competition
that happened during the 2nd r2con in 2017.

* https://www.youtube.com/watch?v=sB-i5yUatx4

This repository contains an evolved implementation of the engine
written in C# by SkUaTeR dropping the MFC requirement that was
making it impossible to run outside Windows.

The solution was to use an embedded webserver that provides a
web interface using websockets to stream the process changes
from the M

## Dependencies

* .NET 10 SDK (to build) or ASP.NET Core Runtime 10 (to run a published build)
* radare2

The Docker image pins radare2 6.2.0 and installs the checksummed `amd64` or
`arm64` package from the official GitHub release.

Run the application from the repository root with:

    dotnet run --project csharp/r2wars.csproj -- warriors

Then open `http://127.0.0.1:9664/`.

On Windows you need to have radare2.exe and rasm2.exe in the published
application directory or in `PATH`.

On Mac/Linux/BSD, r2wars will try to find them in the PATH.

--pancake
