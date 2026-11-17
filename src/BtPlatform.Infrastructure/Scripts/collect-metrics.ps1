# Salt okunur canlı ölçüm betiği: girdi almaz, sistemde hiçbir şeyi değiştirmez, sadece JSON yazar.
$ErrorActionPreference = 'Stop'
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8

    $os = Get-CimInstance Win32_OperatingSystem
    $cpu = Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average
    $disk = Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" |
    Measure-Object -Property Size, FreeSpace -Sum

    $ramTotal = $os.TotalVisibleMemorySize / 1MB          # KB -> GB
    $ramFree = $os.FreePhysicalMemory / 1MB
    $diskTotal = ($disk | Where-Object Property -eq 'Size').Sum / 1GB
    $diskFree = ($disk | Where-Object Property -eq 'FreeSpace').Sum / 1GB
    $uptime = ((Get-Date) - $os.LastBootUpTime).TotalHours

    $result = [ordered]@{
        cpuPercent  = [math]::Round([double]$cpu.Average, 1)
        ramTotalGb  = [math]::Round($ramTotal, 1)
        ramUsedGb   = [math]::Round($ramTotal - $ramFree, 1)
        diskTotalGb = [math]::Round($diskTotal, 1)
        diskFreeGb  = [math]::Round($diskFree, 1)
        uptimeHours = [math]::Round($uptime, 1)
    }

    $result | ConvertTo-Json -Compress
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}