#But du script : copier un dossier vers un autre en ignorant les fichiers identiques et en renommant les fichiers différents en conlits de nom
#L'objectif est donc de ne pas perdre de données et d'éviter les doublons
#Ce script ce base sur un fichier csv des fichiers à copier

#PARAMETERS :
#=================
$DST_ROOT_PATH = ".\Famille test"
$SRC_ROOT_PATH = "E:\Hugo"
$SUFFIX = "(bckup 2)"
$CSV_PATH = ".\comp HUGO4.csv"
$LOG_PATH = "log test.txt"

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

PrintText ("Starting backup script with CSV from " + $SRC_ROOT_PATH + " to " + $DST_ROOT_PATH + " ...")

$CSV = (Import-CSV -Path $CSV_PATH -Delimiter ",")

$copied_files_count = 0

foreach($line in $CSV)
{
    #$line | Get-Member | Write-Host
    #Write-Host ($line."Nom du fichier")
    #break

    if($line."Resultat de la comparaison" -eq "Les fichiers binaires sont differents")
    {
        $SRC_FILE_PATH = $SRC_ROOT_PATH + "\" + $line."Dossier" + "\" + $line."Nom du fichier"
        $DST_FILE_PATH = $DST_ROOT_PATH + "\" + $line."Dossier" + "\" + $line."Nom du fichier"
        $DST_FILE_PATH = $DST_FILE_PATH -replace "\.[^.]+$", "" 
        $DST_FILE_PATH += $SUFFIX + "." + $line.Extension
        if (Test-Path $DST_FILE_PATH)
        {
            PrintText ("Error ! File already exists : " + $DST_FILE_PATH) -ForegroundColor Red
        }
        elseif (-not (Test-Path $SRC_FILE_PATH)) {
            PrintText ("Error ! Source file does not exist : " + $SRC_FILE_PATH) -ForegroundColor Red
        }
        else{
            PrintText ("Copy " + $SRC_FILE_PATH + " => " + $DST_FILE_PATH) -ForegroundColor Blue
            New-Item -ItemType File -Path $DST_FILE_PATH -force
            Copy-Item $SRC_FILE_PATH -Destination ($DST_FILE_PATH)
            $copied_files_count += 1
            
        }
    }
}
PrintText ([string]$copied_files_count + " files copied.") -ForegroundColor Blue
PrintText "End of backup script with CSV."
