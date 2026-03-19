# 1. Explicitly load the HttpClient assembly for Windows PowerShell 5.1
Add-Type -AssemblyName System.Net.Http

$apiUrl = "http://localhost:5000/api/events/telemetry"
$totalEvents = 500

Write-Host "Initializing high-throughput stress test..." -ForegroundColor Cyan
Write-Host "Target: $apiUrl" -ForegroundColor Gray
Write-Host "Payload: $totalEvents concurrent events" -ForegroundColor Gray

# 2. Instantiate the .NET HttpClient
$httpClient = [System.Net.Http.HttpClient]::new()
$tasks = @()

Write-Host "Queuing $totalEvents concurrent requests..." -ForegroundColor Yellow

# 3. Generate and queue the requests
for ($i = 1; $i -le $totalEvents; $i++) {
    
    $zone = if ($i % 3 -eq 0) { "Server Room" } else { "Main Lobby" }
    $occupancyDelta = Get-Random -Minimum -2 -Maximum 5
    $eventId = [guid]::NewGuid().ToString()
    $eventType = if ($i % 100 -eq 0) { 1 } else { 0 } 

    $jsonPayload = @"
    {
        "EventId": "$eventId",
        "ZoneId": "$zone",
        "DeviceId": "Simulated-Sensor-$($i % 10)",
        "Type": $eventType,
        "Timestamp": "$( (Get-Date).ToUniversalTime().ToString("o") )",
        "OccupancyDelta": $occupancyDelta
    }
"@

    $content = [System.Net.Http.StringContent]::new($jsonPayload, [System.Text.Encoding]::UTF8, "application/json")
    
    # Fire the request asynchronously
    $tasks += $httpClient.PostAsync($apiUrl, $content)
}

# 4. Await all asynchronous tasks
[System.Threading.Tasks.Task]::WaitAll($tasks)

Write-Host "Stress test complete! $totalEvents events blasted to the Ingress API." -ForegroundColor Green

# 5. Cleanup
$httpClient.Dispose()