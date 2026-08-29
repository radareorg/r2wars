# syntax=docker/dockerfile:labs
ARG UBUNTU_RELEASE="24.04"
ARG TIMEZONE="UTC"
ARG DOTNET_IMAGE="10.0-noble"
ARG R2_SOURCE="https://github.com/radareorg/radare2.git"
ARG R2_INSTALL_DIR="/opt/r2"
ARG CCACHE_DIR="/root/.cache/ccache"
ARG CC="ccache gcc"
ARG CXX="ccache g++"


################################################################################
# Base image for builder/runner                                                #
################################################################################
FROM ubuntu:${UBUNTU_RELEASE} AS base
ARG TIMEZONE

# Prepare timezone settings
ENV TZ=${TIMEZONE}
RUN ln -snf /usr/share/zoneinfo/${TIMEZONE} /etc/localtime && \
    echo ${TIMEZONE} > /etc/timezone

# Enable APT package caching
RUN rm -f /etc/apt/apt.conf.d/docker-clean && \
    echo 'Binary::apt::APT::Keep-Downloaded-Packages "true";' > /etc/apt/apt.conf.d/keep-cache


################################################################################
# r2wars builder                                                               #
################################################################################
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_IMAGE} AS r2wars-builder

# Add r2wars source and build it
COPY --link csharp /r2wars

WORKDIR /r2wars
RUN dotnet publish r2wars.csproj \
        --configuration Release \
        --output /r2wars-publish \
        --no-self-contained


################################################################################
# r2 builder                                                                   #
################################################################################
FROM base AS r2-builder
ARG CCACHE_DIR
ARG CC
ARG CXX
ARG R2_SOURCE
ARG R2_INSTALL_DIR

# Install base packages
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    apt-get update && \
    apt-get install -y --no-install-recommends \
        build-essential \
        ccache \
        cmake \
        ninja-build \
        pkg-config \
        git \
        ca-certificates \
        python3-pip && \
    python3 -m pip install --no-cache-dir 'meson>=0.63'

# By default, add r2 source from GitHub and build it -- replace the git repo
# with your local path if you want to build your custom radare2 source tree
ADD ${R2_SOURCE} /r2src

WORKDIR /r2src

RUN --mount=type=cache,id=ccache,target=${CCACHE_DIR},sharing=shared \
    meson setup build \
        -Dbuildtype=release \
        -Dprefix=${R2_INSTALL_DIR} \
        -Dlocal=true && \
    ninja -C build install

################################################################################
# r2wars runner                                                                #
################################################################################
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_IMAGE} AS runner
ARG R2_INSTALL_DIR
ARG TIMEZONE

ENV TZ=${TIMEZONE}

# Copy r2 and r2wars in from the build stages
COPY --from=r2wars-builder --link /r2wars-publish /r2wars
COPY --from=r2-builder --link ${R2_INSTALL_DIR} ${R2_INSTALL_DIR}
ENV PATH=${PATH}:${R2_INSTALL_DIR}/bin

EXPOSE 9664 9966

WORKDIR /r2wars
ENTRYPOINT ["dotnet", "r2wars.dll"]
