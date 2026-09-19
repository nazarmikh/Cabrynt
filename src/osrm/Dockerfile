FROM ghcr.io/project-osrm/osrm-backend@sha256:8a1b1bc938412f15f9b5b32d794c4ec6bf4a85dfbbabfa0a014b70b187edb53b

LABEL org.opencontainers.image.source="https://github.com/nazarmikh/Cabrynt"

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl \
    && rm -rf /var/lib/apt/lists/*

COPY start-osrm.sh /usr/local/bin/start-osrm
RUN chmod +x /usr/local/bin/start-osrm

ENTRYPOINT ["/usr/local/bin/start-osrm"]
