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
	Invoke-Expression '& "$devenv" "NeoEdit.sln" /build "Release|x86" /project Loader /out Build-x86.log'
	if ($LASTEXITCODE -ne 0) { Fail("Failed to build.") }
	Invoke-Expression '& "$devenv" "NeoEdit.sln" /build "Release|x64" /project Loader /out Build-x64.log'
	if ($LASTEXITCODE -ne 0) { Fail("Failed to build.") }
}

Function CopyLocations ()
{
	if (Test-Path "Locations.txt")
	{
		$locations = Get-Content "Locations.txt"
		ForEach ($location in $locations)
		{
			if ([string]::IsNullOrEmpty($location)) { continue; }
			Copy-Item "bin\NeoEdit32.exe" "$location"
			Copy-Item "bin\NeoEdit64.exe" "$location"
		}
	}
}

GitClean
GitUpdate
Build
Start-Process "bin\Release.x86\NeoEdit.Loader.exe" -Wait
Start-Process "bin\Release.x64\NeoEdit.Loader.exe" -Wait
$bytes32 = [System.IO.File]::ReadAllBytes("bin\Release.x86\NeoEdit.exe")
$bytes64 = [System.IO.File]::ReadAllBytes("bin\Release.x64\NeoEdit.exe")
GitClean
New-Item -ItemType Directory -Force -Path bin
[System.IO.File]::WriteAllBytes("bin\NeoEdit32.exe", $bytes32)
[System.IO.File]::WriteAllBytes("bin\NeoEdit64.exe", $bytes64)
CopyLocations

Write-Host("Success!")
