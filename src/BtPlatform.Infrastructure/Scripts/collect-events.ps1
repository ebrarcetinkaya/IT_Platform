# Salt okunur olay günlüğü betiği: girdi almaz, sistemde hiçbir şeyi değiştirmez, sadece JSON yazar.
# System ve Application günlüklerinden son 24 saatin Critical/Error/Warning olayları (en fazla 50).
$ErrorActionPreference = 'Stop'
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8

    $since = (Get-Date).AddHours(-24)
    $items = @()

    foreach ($log in @('System', 'Application')) {
        # Level: 1=Critical, 2=Error, 3=Warning
        $found = Get-WinEvent -FilterHashtable @{ LogName = $log; Level = 1, 2, 3; StartTime = $since } `
                    -MaxEvents 50 -ErrorAction SilentlyContinue
        foreach ($e in $found) {
            $levelName = switch ($e.Level) { 1 { 'Critical' } 2 { 'Error' } 3 { 'Warning' } default { 'Other' } }
            $msg = [string]$e.Message
            if ($msg.Length -gt 600) { $msg = $msg.Substring(0, 600) }
            $items += [pscustomobject]@{
                log      = $log
                level    = $levelName
                eventId  = [int]$e.Id
                source   = [string]$e.ProviderName
                time     = $e.TimeCreated.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
                message  = $msg
                recordId = [long]$e.RecordId
            }
        }
    }

    # En yeni 50 olay
    $top = @($items | Sort-Object time -Descending | Select-Object -First 50)
    if ($top.Count -eq 0) { Write-Output '[]' }
    else { ConvertTo-Json -InputObject $top -Compress -Depth 3 }
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}