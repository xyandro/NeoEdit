$inputfile = $args[0]
$outputfile = $args[1]

$encoding = [Text.Encoding]::Unicode
$input = [System.IO.File]::ReadAllText($inputfile)

$year = (Get-Date).year
$count = & git rev-list HEAD --count

$input = $input -replace '%YEAR%', $year
$input = $input -replace '%NUMREVS%', $count

[System.IO.File]::WriteAllText($outputfile, $input, $encoding)
