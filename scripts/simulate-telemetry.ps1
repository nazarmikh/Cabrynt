[CmdletBinding()]
param(
    [string]$ConfigPath = ".\scripts\vehicle-simulator.config.json",
    [int]$StartupRetryCount = 30,
    [int]$StartupRetryDelaySeconds = 5
)

$ErrorActionPreference = "Stop"

function Get-RandomOffset {
    param(
        [double]$Min = -0.0025,
        [double]$Max = 0.0025
    )

    return (Get-Random -Minimum ($Min * 1000000) -Maximum ($Max * 1000000)) / 1000000
}

function Clamp-Value {
    param(
        [double]$Value,
        [double]$Min,
        [double]$Max
    )

    return [Math]::Min($Max, [Math]::Max($Min, $Value))
}

function Invoke-JsonPost {
    param(
        [string]$Url,
        [hashtable]$Body,
        [hashtable]$Headers = @{}
    )

    return Invoke-RestMethod -Method Post -Uri $Url -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 5) -Headers $Headers
}

function Invoke-WithRetry {
    param(
        [scriptblock]$Action,
        [string]$Description,
        [int]$RetryCount,
        [int]$DelaySeconds
    )

    for ($attempt = 1; $attempt -le $RetryCount; $attempt++) {
        try {
            return & $Action
        }
        catch {
            if ($attempt -eq $RetryCount) {
                throw "Failed after $RetryCount attempts: $Description. Last error: $($_.Exception.Message)"
            }

            Write-Warning ("{0} failed on attempt {1}/{2}: {3}. Retrying in {4} second(s)..." -f $Description, $attempt, $RetryCount, $_.Exception.Message, $DelaySeconds)
            Start-Sleep -Seconds $DelaySeconds
        }
    }
}

if (-not (Test-Path $ConfigPath)) {
    throw "Config file not found: $ConfigPath. Copy scripts/vehicle-simulator.config.example.json to scripts/vehicle-simulator.config.json and fill the real vehicle IDs."
}

$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$baseUrl = $config.baseUrl.TrimEnd("/")
$intervalSeconds = [int]$config.intervalSeconds
$includeAnomalies = [bool]$config.includeAnomalies

$vehicleStates = @()

foreach ($vehicle in $config.vehicles) {
    Write-Host "Logging in vehicle account $($vehicle.email)..." -ForegroundColor Cyan

    $loginResponse = Invoke-WithRetry -Description "Vehicle login for $($vehicle.email)" -RetryCount $StartupRetryCount -DelaySeconds $StartupRetryDelaySeconds -Action {
        Invoke-JsonPost -Url "$baseUrl/api/public/auth/login" -Body @{
            email = $vehicle.email
            password = $vehicle.password
        }
    }

    if (-not $loginResponse.accessToken) {
        throw "Login failed for $($vehicle.email)."
    }

    $vehicleStates += [pscustomobject]@{
        Email = $vehicle.email
        VehicleId = [int]$vehicle.vehicleId
        Token = [string]$loginResponse.accessToken
        BaseLatitude = [double]$vehicle.baseLatitude
        BaseLongitude = [double]$vehicle.baseLongitude
        Latitude = [double]$vehicle.baseLatitude
        Longitude = [double]$vehicle.baseLongitude
        Battery = [double]$vehicle.batteryStart
        HardwareTemperature = [double]$vehicle.hardwareTemperatureStart
        Speed = [double]$vehicle.speedStart
        Tick = 0
    }
}

Write-Host "Telemetry simulator started. Press Ctrl+C to stop." -ForegroundColor Green

while ($true) {
    foreach ($state in $vehicleStates) {
        $state.Tick += 1
        $state.Latitude = [Math]::Round($state.BaseLatitude + (Get-RandomOffset), 6)
        $state.Longitude = [Math]::Round($state.BaseLongitude + (Get-RandomOffset), 6)

        $batteryDrain = 0.1 + (Get-Random -Minimum 0 -Maximum 20) / 100
        $state.Battery = [Math]::Round((Clamp-Value -Value ($state.Battery - $batteryDrain) -Min 2 -Max 100), 2)

        $tempShift = (Get-Random -Minimum -15 -Maximum 16) / 10
        $state.HardwareTemperature = [Math]::Round((Clamp-Value -Value ($state.HardwareTemperature + $tempShift) -Min 35 -Max 90), 2)

        $speedShift = Get-Random -Minimum -8 -Maximum 9
        $state.Speed = [Math]::Round((Clamp-Value -Value ($state.Speed + $speedShift) -Min 0 -Max 170), 2)

        if ($includeAnomalies) {
            if ($state.Tick % 15 -eq 0) {
                $state.Speed = 165
            }

            if ($state.Tick % 20 -eq 0) {
                $state.Battery = 4.5
            }

            if ($state.Tick % 25 -eq 0) {
                $state.HardwareTemperature = 86
            }
        }

        $headers = @{
            Authorization = "Bearer $($state.Token)"
            Accept = "application/json"
        }

        $payload = @{
            latitude = $state.Latitude
            longitude = $state.Longitude
            currentSpeed = $state.Speed
            remainingBatteryPercentage = $state.Battery
            hardwareTemperature = $state.HardwareTemperature
            vehicleId = $state.VehicleId
        }

        try {
            Invoke-JsonPost -Url "$baseUrl/api/private/telemetry" -Body $payload -Headers $headers | Out-Null
            Write-Host ("[{0}] Vehicle {1} -> lat {2}, lon {3}, speed {4}, battery {5}, temp {6}" -f (Get-Date -Format "HH:mm:ss"), $state.VehicleId, $state.Latitude, $state.Longitude, $state.Speed, $state.Battery, $state.HardwareTemperature)
        }
        catch {
            Write-Warning "Telemetry post failed for vehicle $($state.VehicleId) ($($state.Email)): $($_.Exception.Message)"
        }
    }

    Start-Sleep -Seconds $intervalSeconds
}
