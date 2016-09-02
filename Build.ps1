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

		$status -Match '^!!' -Replace '^!!\s+' -NotMatch '^NeoEdit\.exe$' | rm -recurse -force
		if (!$?) { Fail("Failed to delete files.") }
	}
}

Function GitUpdate ()
{
	git checkout master
	git remote update
	git reset --hard origin/master
}

Function Build ()
{
	$start = [System.DateTime]::Now

	$devenv = "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.com"
	$args = ""
	foreach ($bitdepth in $bitdepths)
	{
		$args += "-Start=..\Release.$bitdepth\NeoEdit.exe "
		Invoke-Expression '& "$devenv" "NeoEdit.sln" /build "Release|$bitdepth" /project Loader /out Build$bitdepth.log'
		if ($LASTEXITCODE -ne 0) { Fail("Failed to build.") }
	}
	$args += "-output=NeoEdit.exe -ngen=1 -extractaction=gui -go"
	$proc = Start-Process "bin\Release.AnyCPU\Loader.exe" $args -PassThru -Wait
	if ($proc.ExitCode -ne 0) { Fail("Failed to build.") }

	$end = [System.DateTime]::Now
	$elapsed = ($end - $start).TotalSeconds

	Write-Host("Build time: $elapsed seconds.")
}

GitClean
GitUpdate
Build
$bytes = [System.IO.File]::ReadAllBytes("bin\Release.AnyCPU\NeoEdit.exe")
GitClean
New-Item -ItemType Directory -Force -Path bin
[System.IO.File]::WriteAllBytes("bin\NeoEdit.exe", $bytes)

Write-Host("Success!")
