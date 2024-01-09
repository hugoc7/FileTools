#But du script : copier un dossier vers un autre en ignorant les fichiers identiques et en renommant les fichiers différents en conlits de nom
#L'objectif est donc de ne pas perdre de données et d'éviter les doublons

#PARAMETERS :
#=================
$SRC_ROOT_PATH = (Get-Item "E:\Famille").FullName
$DST_ROOT_PATH = (Get-Item "I:\Famille").FullName
$SUFFIX = "(bckup 2)"
$LOG_PATH = "log.txt"

#SCRIPT :
#=================

function PrintText() {
    param (
        [Parameter(Mandatory=$true)][string]$Object,
        [Parameter(Mandatory=$false)][string]$ForegroundColor,
        [Parameter(Mandatory=$false)][string]$BackGroundColor
    )
    Write-Host @PSBoundParameters
    
    Write-Output $Object >> $LOG_PATH
}

PrintText ("Starting backup script from " + $SRC_ROOT_PATH + " to " + $DST_ROOT_PATH)
PrintText "Listing and comparing files ..."
$SRC_FILES = Get-ChildItem $SRC_ROOT_PATH -Recurse -File
$DST_FILES = Get-ChildItem $DST_ROOT_PATH -Recurse -File

$COMP = Compare-Object -ReferenceObject $SRC_FILES -DifferenceObject $DST_FILES -ExcludeDifferent -IncludeEqual
$FILES_COUNT = ($COMP | Measure-Object).Count


#$COMP | Measure-Object | Get-Member
$next_refresh = 0
$i = 0
$copied_files_count = 0


PrintText ([string]$FILES_COUNT + " name-conflicting files found.") -ForegroundColor Blue
PrintText "Checking all name-conflicting files. If content is different, copy them with a different name ..."

foreach($file in $COMP)
{
    $SRC_FILE_PATH = $file.InputObject.FullName
    $DST_FILE_PATH = $DST_ROOT_PATH + $SRC_FILE_PATH.Substring($SRC_ROOT_PATH.Length)
    
    #Show user progression :
    $i += 1
    if($i -ge $next_refresh)
    {
        $next_refresh = $next_refresh + [Math]::Ceiling($FILES_COUNT / 100)
        $percent = 100 * $i / $FILES_COUNT
        Write-Host ("[" + [string]$i + "/" + [string]$FILES_COUNT + "] " + $SRC_FILE_PATH)
    }

    #If file content is different, keep both file : 
    if ((Get-FileHash $SRC_FILE_PATH).Hash -ne (Get-FileHash $DST_FILE_PATH).Hash) {

        $RENAMED_DST_FILE_PATH = $DST_ROOT_PATH + $file.InputObject.Directory.FullName.Substring($SRC_ROOT_PATH.Length) + '\' + $file.InputObject.BaseName
        $RENAMED_DST_FILE_PATH += $SUFFIX + $file.InputObject.Extension
        if (Test-Path $RENAMED_DST_FILE_PATH)
        {
            PrintText ("Error ! File already exists : " + $RENAMED_DST_FILE_PATH) -ForegroundColor Red
        }
        else {
            PrintText ("Copy " + $SRC_FILE_PATH + " => " + $RENAMED_DST_FILE_PATH) -ForegroundColor Blue
            Copy-Item $SRC_FILE_PATH -Destination ($RENAMED_DST_FILE_PATH)
            $copied_files_count += 1
        }
    }
}
PrintText ([string]$copied_files_count + " files copied.") -ForegroundColor Blue
PrintText "End of backup script."
