param ( [int]$bitdepth = 0 )

$bitdepths = @()
if ($bitdepth -eq 0)
{ $bitdepths += (32, 64) }
else
{ $bitdepths += $bitdepth }

#$debug = 1

Function Fail ($error)
{
	Write-Host($error)
	Write-Host("Press any key to exit.")
	$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
	exit
}

Function GitClean ()
{
	$fail = 0;
	$status = @(git status --porcelain --ignored)
	if ($status)
	{
		ForEach ($line in $status)
		{
			if ($line -NotMatch '^!!')
			{
				Write-Host("Invalid: $line")
				$fail = 1;
			}
		}

		if (($fail) -and (!$debug)) { Fail("Invalid files present") }

		$status -Match '^!!' -Replace '^!!\s+' -NotMatch '^(Locations.txt|NeoEdit.exe)$' | rm -recurse -force
		if (!$?) { Fail("Failed to delete files.") }
	}
}

Function GitUpdate ()
{
	git checkout master
	git pull
}

Function Build ()
{
	$devenv = "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.com"
	$args = ""
	if ($bitdepths -contains 32)
	{
		$args += "Start=..\Release.x86\NeoEdit.exe "
		Invoke-Expression '& "$devenv" "NeoEdit.sln" /build "Release|x86" /project Loader /out Build32.log'
		if ($LASTEXITCODE -ne 0) { Fail("Failed to build.") }
	}
	if ($bitdepths -contains 64)
	{
		$args += "Start=..\Release.x64\NeoEdit.exe "
		Invoke-Expression '& "$devenv" "NeoEdit.sln" /build "Release|x64" /project Loader /out Build64.log'
		if ($LASTEXITCODE -ne 0) { Fail("Failed to build.") }
	}
	$args += "output=NeoEdit.exe ngen=1 extractaction=gui go"
	$proc = Start-Process "bin\Release.AnyCPU\Loader.exe" $args -PassThru -Wait
	if ($proc.ExitCode -ne 0) { Fail("Failed to build.") }
}

Function CopyLocations ()
{
	if (Test-Path "Locations.txt")
	{
		$locations = Get-Content "Locations.txt"
		ForEach ($location in $locations)
		{
			if ([string]::IsNullOrEmpty($location)) { continue; }
			Copy-Item "bin\NeoEdit.exe" "$location"
		}
	}
}

GitClean
GitUpdate
Build
$bytes = [System.IO.File]::ReadAllBytes("bin\Release.AnyCPU\NeoEdit.exe")
GitClean
New-Item -ItemType Directory -Force -Path bin
[System.IO.File]::WriteAllBytes("bin\NeoEdit.exe", $bytes)
CopyLocations

Write-Host("Success!")
