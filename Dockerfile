# syntax=docker/dockerfile:1
ARG UBUNTU_RELEASE="24.04"
ARG TIMEZONE="UTC"
ARG DOTNET_IMAGE="10.0-noble"
ARG R2_VERSION="6.2.0"


################################################################################
# Download the pinned radare2 release package                                  #
################################################################################
FROM ubuntu:${UBUNTU_RELEASE} AS r2-package
ARG R2_VERSION
ARG TARGETARCH

RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    apt-get update && \
    apt-get install -y --no-install-recommends \
        ca-certificates \
        curl

RUN case "${TARGETARCH}" in \
        amd64) R2_SHA256="eb82324e83315887fbee6f5d8632c982c593e056a87180f1bec5ccb06c463aeb" ;; \
        arm64) R2_SHA256="e866525e9874588d478d536cca38cf9a7562896725efb4119b886101fd93f1ec" ;; \
        *) echo "Unsupported Docker architecture: ${TARGETARCH}" >&2; exit 1 ;; \
    esac && \
    curl --fail --location --show-error \
        "https://github.com/radareorg/radare2/releases/download/${R2_VERSION}/radare2_${R2_VERSION}_${TARGETARCH}.deb" \
        --output /radare2.deb && \
    echo "${R2_SHA256}  /radare2.deb" | sha256sum --check --strict


################################################################################
# r2wars builder                                                               #
################################################################################
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_IMAGE} AS r2wars-builder

COPY --link csharp /r2wars

WORKDIR /r2wars
RUN dotnet publish r2wars.csproj \
        --configuration Release \
        --output /r2wars-publish \
        --no-self-contained


################################################################################
# r2wars runner                                                                #
################################################################################
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_IMAGE} AS runner
ARG TIMEZONE

ENV TZ=${TIMEZONE}

RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    --mount=type=bind,from=r2-package,source=/radare2.deb,target=/tmp/radare2.deb \
    apt-get update && \
    apt-get install -y --no-install-recommends /tmp/radare2.deb

COPY --from=r2wars-builder --link /r2wars-publish /r2wars

EXPOSE 9664 9966

WORKDIR /r2wars
ENTRYPOINT ["dotnet", "r2wars.dll"]
