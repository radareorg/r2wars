FROM mono:latest
MAINTAINER @CaptnBanana

RUN apt update && apt -y install git build-essential wget

# Get and install r2 from maser
RUN git clone https://github.com/radareorg/radare2 /opt/radare2 && /opt/radare2/sys/install.sh

# Get and compile r2wars from master
RUN git clone https://github.com/radareorg/r2wars.git /opt/r2wars
WORKDIR /opt/r2wars/csharp

RUN xbuild /p:Configuration=Release r2wars.csproj

WORKDIR /opt/r2wars/csharp/bin/Release
ENTRYPOINT ["mono", "r2wars.exe"]
