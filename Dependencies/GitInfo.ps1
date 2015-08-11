$inputfile = $args[0]
$outputfile = $args[1]

$encoding = [Text.Encoding]::UTF8
$input = [System.IO.File]::ReadAllText($inputfile)

$year = (Get-Date).year
$count = & git rev-list HEAD --count

$input = $input -replace '%YEAR%', $year
$input = $input -replace '%NUMREVS%', $count

$output = ""
if (Test-Path $outputfile)
{
	$output = [System.IO.File]::ReadAllText($outputfile)
}

if (!$input.Equals($output))
{
	[System.IO.File]::WriteAllText($outputfile, $input, $encoding)
	Write-Host("GitInfo: $outputfile updated.")
}