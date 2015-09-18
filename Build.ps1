param
(
	[string]$platform = ""
)

#$debug = 1
$platform = if ($platform -ne "") { $platform } else { if ([System.IntPtr]::Size -eq 4) { "x86" } else { "x64" } }

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
	Invoke-Expression '& "$devenv" "NeoEdit.sln" /build "Release|$platform" /project NeoEdit.Loader /out Build.log'
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
			Copy-Item "bin\NeoEdit.exe" "$location"
		}
	}
}

GitClean
GitUpdate
Build
Start-Process "bin\Release.$platform\NeoEdit.Loader.exe" -Wait
$bytes = [System.IO.File]::ReadAllBytes("bin\Release.$platform\NeoEdit.exe")
GitClean
New-Item -ItemType Directory -Force -Path bin
[System.IO.File]::WriteAllBytes("bin\NeoEdit.exe", $bytes)
CopyLocations

Write-Host("Success!")
