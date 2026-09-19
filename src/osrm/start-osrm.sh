#!/bin/sh
set -eu

data_dir="${OSRM_DATA_DIRECTORY:-/data}"
extract_url="${OSRM_EXTRACT_URL:-https://download.geofabrik.de/europe/portugal-latest.osm.pbf}"
pbf_path="$data_dir/portugal-latest.osm.pbf"
graph_path="$data_dir/portugal-latest.osrm"

mkdir -p "$data_dir"

# Azure Files keeps the prepared graph between Container App revisions.
if [ ! -f "$graph_path.mldgr" ]; then
    if [ ! -s "$pbf_path" ]; then
        curl --fail --location --retry 3 --retry-delay 5 \
            --output "$pbf_path.tmp" \
            "$extract_url"
        mv "$pbf_path.tmp" "$pbf_path"
    fi

    rm -f "$graph_path".*
    osrm-extract -p /opt/car.lua "$pbf_path"
    osrm-partition "$graph_path"
    osrm-customize "$graph_path"
fi

exec osrm-routed --algorithm mld "$graph_path"
