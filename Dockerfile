# syntax=docker/dockerfile:labs
ARG UBUNTU_RELEASE="latest"
ARG TIMEZONE="UTC"
ARG R2_SOURCE="https://github.com/radareorg/radare2.git"
ARG R2_INSTALL_DIR="/opt/r2"
ARG CCACHE_DIR="/root/.cache/ccache"
ARG CC="ccache gcc"
ARG CXX="ccache g++"


################################################################################
# Base image for builder/runner                                                #
################################################################################
FROM ubuntu:${UBUNTU_RELEASE} as base
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
FROM base as r2wars-builder

# Install base packages
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    apt-get update && \
    apt-get install -y --no-install-recommends \
        mono-complete

# Add r2wars source and build it
COPY --link csharp /r2wars

WORKDIR /r2wars
RUN xbuild /p:Configuration=Release r2wars.csproj


################################################################################
# r2 builder                                                                   #
################################################################################
FROM base as r2-builder
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
        meson \
        ninja-build \
        pkg-config \
        git \
        ca-certificates

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
FROM base as runner
ARG R2_INSTALL_DIR

# Install base packages
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    apt-get update && \
    apt-get install -y --no-install-recommends \
        mono-runtime \
        nuget

# Copy r2 and r2wars in from the build stages
COPY --from=r2wars-builder --link /r2wars/bin/Release /r2wars
COPY --from=r2-builder --link ${R2_INSTALL_DIR} ${R2_INSTALL_DIR}
ENV PATH=${PATH}:${R2_INSTALL_DIR}/bin

EXPOSE 9664 9966

WORKDIR /r2wars
ENTRYPOINT ["mono", "r2wars.exe"]
