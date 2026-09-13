[CmdletBinding()]
param(
    [string]$PbfUrl = "https://download.geofabrik.de/europe/portugal-latest.osm.pbf",
    [string]$Image = "ghcr.io/project-osrm/osrm-backend:v6.0.0",
    [switch]$ForceDownload
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$DataDirectory = Join-Path $ProjectRoot "data/osrm"
$PbfPath = Join-Path $DataDirectory "portugal-latest.osm.pbf"
$OsrmBasePath = "/data/portugal-latest.osrm"
$VolumeMount = "$($DataDirectory):/data"

New-Item -ItemType Directory -Force -Path $DataDirectory | Out-Null

& docker info | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Docker Desktop must be running before OSRM can be prepared."
}

if ($ForceDownload) {
    Remove-Item -LiteralPath $PbfPath -Force -ErrorAction SilentlyContinue
    Get-ChildItem -Path $DataDirectory -Filter "portugal-latest.osrm*" |
        Remove-Item -Force
}

if (-not (Test-Path $PbfPath)) {
    Write-Host "Downloading the Portugal OpenStreetMap extract..."
    Invoke-WebRequest -Uri $PbfUrl -OutFile $PbfPath
}

$SourceMetadata = [ordered]@{
    pbf_url = $PbfUrl
    pbf_sha256 = (Get-FileHash -Path $PbfPath -Algorithm SHA256).Hash
    prepared_at_utc = (Get-Date).ToUniversalTime().ToString("o")
}
$SourceMetadata | ConvertTo-Json | Set-Content (Join-Path $DataDirectory "source.json")

$PartitionPath = Join-Path $DataDirectory "portugal-latest.osrm.partition"
$CustomizationPath = Join-Path $DataDirectory "portugal-latest.osrm.cell_metrics"

if (-not (Test-Path $PartitionPath)) {
    Write-Host "Extracting the road network. This can take several minutes."
    & docker run --rm -v $VolumeMount $Image osrm-extract -p /opt/car.lua $OsrmBasePath.Replace(".osrm", ".osm.pbf")
    if ($LASTEXITCODE -ne 0) {
        throw "OSRM extraction failed."
    }

    & docker run --rm -v $VolumeMount $Image osrm-partition $OsrmBasePath
    if ($LASTEXITCODE -ne 0) {
        throw "OSRM partitioning failed."
    }
}

if (-not (Test-Path $CustomizationPath)) {
    Write-Host "Customizing the routing graph."
    & docker run --rm -v $VolumeMount $Image osrm-customize $OsrmBasePath
    if ($LASTEXITCODE -ne 0) {
        throw "OSRM customization failed."
    }
}

Write-Host "OSRM data is ready. Start it with:"
Write-Host "docker compose -f compose.osrm.yaml up -d"
