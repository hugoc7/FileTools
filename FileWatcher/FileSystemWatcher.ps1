#By BigTeddy 05 September 2011

#This script uses the .NET FileSystemWatcher class to monitor file events in folder(s).
#The advantage of this method over using WMI eventing is that this can monitor sub-folders.
#The -Action parameter can contain any valid Powershell commands.  I have just included two for example.
#The script can be set to a wildcard filter, and IncludeSubdirectories can be changed to $true.
#You need not subscribe to all three types of event.  All three are shown for example.
# Version 1.1

$folder = 'DOSSIER1' # Enter the root path you want to monitor.
$filter = '*.*'  # You can enter a wildcard filter here.

# In the following line, you can change 'IncludeSubdirectories to $true if required.                          
$fsw = New-Object IO.FileSystemWatcher $folder, $filter -Property @{IncludeSubdirectories = $true;NotifyFilter = [IO.NotifyFilters]'FileName, LastWrite, DirectoryName'}
# Here, all three events are registerd.  You need only subscribe to events that you need:

$fileNameList = [System.Collections.ArrayList]::new()
$timeStampList  = [System.Collections.ArrayList]::new()
$changeTypeList = [System.Collections.ArrayList]::new()


Register-ObjectEvent $fsw Renamed -SourceIdentifier FileRenamed -Action {
    #$Event | ConvertTo-Json | Write-Host
    $name = $Event.SourceEventArgs.Name
    $changeType = $Event.SourceEventArgs.ChangeType
    $timeStamp = $Event.TimeGenerated
    #Write-Host "RENAME: The file '$name' was $changeType at $timeStamp" -fore green
    #Out-File -FilePath outlog.txt -Append -InputObject "The file '$name' was $changeType at $timeStamp"
}

Register-ObjectEvent $fsw Created -SourceIdentifier FileCreated -Action {
    $name = $Event.SourceEventArgs.Name
    $changeType = $Event.SourceEventArgs.ChangeType
    $timeStamp = $Event.TimeGenerated
    for ($i = 0;  $i -lt $fileNameList.Length; $i++){
        if ($changeTypeList[$i] -eq [System.IO.WatcherChangeTypes]::Deleted)
        {
            Write-Host "Hey you!"
            if($fileNameList[$i] -eq $name)
            {
                Write-Host "['$timeStamp'] MOVE: '$fileNameList[$i]' => '$name'" -fore Yellow
                #$fileNameList.RemoveAt($i)
                #$changeTypeList.RemoveAt($i)
                #$timeStampList.RemoveAt($i)
            }
        }
    }
    $fileNameList.Add($name)
    $changeTypeList.Add($changeType)
    $timeStampList.Add($timeStamp)
    Write-Host "CREATE: The file '$name' was $changeType at $timeStamp" -fore green
     

    Out-File -FilePath outlog.txt -Append -InputObject "The file '$name' was $changeType at $timeStamp"}

Register-ObjectEvent $fsw Deleted -SourceIdentifier FileDeleted -Action {
    #$Event | ConvertTo-Json | Write-Host
    $name = $Event.SourceEventArgs.Name
    $changeType = $Event.SourceEventArgs.ChangeType
    $timeStamp = $Event.TimeGenerated
    for ($i = 0;  $i -lt $fileNameList.Length; $i++){
        if ($changeTypeList[$i] -eq [System.IO.WatcherChangeTypes]::Created)
        {
            Write-Host "Hey you!"

            if($fileNameList[$i] -eq $name)
            {
                Write-Host "['$timeStamp'] MOVE: '$name' => '$fileNameList[$i]'" -fore Yellow
                #$fileNameList.RemoveAt($i)
                #$changeTypeList.RemoveAt($i)
                #$timeStampList.RemoveAt($i)
            }
        }
    }
    $fileNameList.Add($name)
    $changeTypeList.Add($changeType)
    $timeStampList.Add($timeStamp)
    Write-Host "DELETE: The file '$name' was $changeType at $timeStamp" -fore red
    Out-File -FilePath outlog.txt -Append -InputObject "The file '$name' was $changeType at $timeStamp"}

Register-ObjectEvent $fsw Changed -SourceIdentifier FileChanged -Action {
    #$Event | ConvertTo-Json | Write-Host
    $name = $Event.SourceEventArgs.Name
    $changeType = $Event.SourceEventArgs.ChangeType
    $timeStamp = $Event.TimeGenerated
    #Write-Host "CHANGE: The file '$name' was $changeType at $timeStamp" -fore white
    #Out-File -FilePath outlog.txt -Append -InputObject "The file '$name' was $changeType at $timeStamp"
}


