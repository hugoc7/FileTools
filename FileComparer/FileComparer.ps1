#But du script : comparer les fichiers de 2 dossiers
#PARAMETERS :
#=================

#SRC doit tjrs etre le plus petit dossier !
$SRC_DIR = Get-Item "E:"
$DST_DIR = Get-Item "F:"

$LOG_PATH = "log comparaison "+$SRC_DIR.Name+" avec "+$DST_DIR.Name+".txt"

#SCRIPT :
#=================
$SRC_ROOT_PATH = $SRC_DIR.FullName
$DST_ROOT_PATH = $DST_DIR.FullName
Write-Host ("Debut de la comparaison de " + $SRC_ROOT_PATH + " avec " + $DST_ROOT_PATH)
(Write-Output ("Debut de la comparaison de " + $SRC_ROOT_PATH + " avec " + $DST_ROOT_PATH)) >> $LOG_PATH

Write-Host  ("Creation de la liste des fichiers de : "  + $SRC_ROOT_PATH + " ...")
$SRC_FILES = Get-ChildItem $SRC_ROOT_PATH -Recurse -File

Write-Host ("Comptage du nombre de fichiers de : "  + $SRC_ROOT_PATH + " ...")
$FILES_COUNT = ($SRC_FILES | Measure-Object).Count

$next_refresh = 0
$i = 0
$diff_count = 0

Write-Host ("Comparaison fichier par fichier avec : " + $DST_ROOT_PATH + " ...")
foreach($file in $SRC_FILES)
{
    $SRC_FILE_PATH = $file.FullName
    $DST_FILE_PATH = $DST_ROOT_PATH + $SRC_FILE_PATH.Substring($SRC_ROOT_PATH.Length)
    
    #Show user progression :
    $i += 1
    if($i -ge $next_refresh)
    {
        $next_refresh = $next_refresh + [Math]::Ceiling($FILES_COUNT / 100)
        $percent = 100 * $i / $FILES_COUNT
        Write-Host ("[" + [string]$i + "/" + [string]$FILES_COUNT + "] " + $SRC_FILE_PATH)
    }

    #Compare files
    if (Test-Path -LiteralPath $DST_FILE_PATH) {
        $dst_file = Get-Item -LiteralPath $DST_FILE_PATH
        if ($dst_file.Length -ne $file.Length)
        {
            Write-Host ("Taille differente : " + $DST_FILE_PATH) -ForegroundColor Blue
            (Write-Output ("Taille differente : " + $DST_FILE_PATH)) >> $LOG_PATH
            $diff_count += 1
        }
        elseif ($file.LastWriteTime -ne $dst_file.LastWriteTime) {
            Write-Host ("Date de modif. differente : " + $DST_FILE_PATH) -ForegroundColor Blue
            (Write-Output ("Date de modif. differente : " + $DST_FILE_PATH)) >> $LOG_PATH
            $diff_count += 1
        }
    }
    else {
        Write-Host ("Fichier introuvable : " + $DST_FILE_PATH) -ForegroundColor Blue
        (Write-Output ("Fichier introuvable : " + $DST_FILE_PATH)) >> $LOG_PATH
        $diff_count += 1
    }
}
Write-Host ([string]$diff_count + " differences detectees. Fin du script de comparaison.") -ForegroundColor Blue
(Write-Output ([string]$diff_count + " differences detectees. Fin du script de comparaison.")) >> $LOG_PATH
