$data = (git log --pretty=oneline)
For ($i=0; $i -lt $data.Count; $i++)
{
	$data[$i] = ($data.Count - $i).ToString() + "	" + $data[$i].SubString(41)
}
[System.IO.File]::WriteAllLines($args[0], $data, [Text.Encoding]::UTF8)
