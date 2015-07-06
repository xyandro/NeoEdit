#$debug = 1

Function Fail ($error)
{
	Write-Host($error)
	Write-Host("Press any key to exit.")
	$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
	exit
}

Function SvnCleanAndUpdate ()
{
	$fail = 0;
	$status = @(svn status --no-ignore)
	if ($status)
	{
		ForEach ($line in $status)
		{
			if ($line -NotMatch '^i')
			{
				#Write-Host("$line")
				$fail = 1;
			}
		}

		if (($fail) -and (!$debug)) { Fail("Invalid files present") }

		$status -Match '^i' -Replace '^i\s+' -NotMatch '^Locations.txt$' | rm -recurse -force
		if (!$?) { Fail("Failed to delete files.") }
	}
	svn up --non-interactive
	svn cleanup --non-interactive
}

Function Build ()
{
	$devenv = "C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\devenv.com"
	$solution = "NeoEdit.sln"
	$configuration = "Release"
	$platform = "x64"
	$builddir = "bin\$configuration.$platform"

	Invoke-Expression '& "$devenv" "$solution" /build "$configuration|$platform" /out Build.log'
	if ($LASTEXITCODE -ne 0) { Fail("Failed to build.") }

	rm $builddir\*.pdb
	rm $builddir\*.metagen
	rm $builddir\*.ilk
	rm $builddir\*.lib
	rm $builddir\*.xml

	Start-Process "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\ngen.exe" -Verb runAs -ArgumentList 'install "bin\Release.x64\NeoEdit.exe"' -Wait

	if (Test-Path "Locations.txt")
	{
		$locations = Get-Content "Locations.txt"
		ForEach ($location in $locations)
		{
			if ([string]::IsNullOrEmpty($location)) { continue; }
			robocopy.exe /e /ndl /r:0 "$builddir" "$location"
		}
	}
}

SvnCleanAndUpdate
Build

Write-Host("Success!")
