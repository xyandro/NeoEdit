@ECHO OFF
REM The command is Base64/UTF-16 encoded.  Decode to edit.

powershell -EncodedCommand IwAgAFQAbwAgAHIAdQBuACAAbwBuACAAYwBvAG0AbQBhAG4AZAAgAGwAaQBuAGUAOgAgAA0ACgAjACAAcABvAHcAZQByAHMAaABlAGwAbAAgAC0AQwBvAG0AbQBhAG4AZAAgAC0AIAA8ACAAQgB1AGkAbABkAC4AYgBhAHQADQAKAA0ACgAjACQAZABlAGIAdQBnACAAPQAgADEADQAKAA0ACgBGAHUAbgBjAHQAaQBvAG4AIABGAGEAaQBsACAAKAAkAGUAcgByAG8AcgApAA0ACgB7AA0ACgAJAFcAcgBpAHQAZQAtAEgAbwBzAHQAKAAkAGUAcgByAG8AcgApAA0ACgAJAFcAcgBpAHQAZQAtAEgAbwBzAHQAKAAiAFAAcgBlAHMAcwAgAGEAbgB5ACAAawBlAHkAIAB0AG8AIABlAHgAaQB0AC4AIgApAA0ACgAJACQAeAAgAD0AIAAkAGgAbwBzAHQALgBVAEkALgBSAGEAdwBVAEkALgBSAGUAYQBkAEsAZQB5ACgAIgBOAG8ARQBjAGgAbwAsAEkAbgBjAGwAdQBkAGUASwBlAHkARABvAHcAbgAiACkADQAKAAkAZQB4AGkAdAANAAoAfQANAAoADQAKAEYAdQBuAGMAdABpAG8AbgAgAFMAdgBuAEMAbABlAGEAbgBBAG4AZABVAHAAZABhAHQAZQAgACgAKQANAAoAewANAAoACQAkAGYAYQBpAGwAIAA9ACAAMAA7AA0ACgAJACQAcwB0AGEAdAB1AHMAIAA9ACAAQAAoAHMAdgBuACAAcwB0AGEAdAB1AHMAIAAtAC0AbgBvAC0AaQBnAG4AbwByAGUAKQANAAoACQBpAGYAIAAoACQAcwB0AGEAdAB1AHMAKQANAAoACQB7AA0ACgAJAAkARgBvAHIARQBhAGMAaAAgACgAJABsAGkAbgBlACAAaQBuACAAJABzAHQAYQB0AHUAcwApAA0ACgAJAAkAewANAAoACQAJAAkAaQBmACAAKAAkAGwAaQBuAGUAIAAtAE4AbwB0AE0AYQB0AGMAaAAgACcAXgBpACcAKQANAAoACQAJAAkAewANAAoACQAJAAkACQAjAFcAcgBpAHQAZQAtAEgAbwBzAHQAKAAiACQAbABpAG4AZQAiACkADQAKAAkACQAJAAkAJABmAGEAaQBsACAAPQAgADEAOwANAAoACQAJAAkAfQANAAoACQAJAH0ADQAKAA0ACgAJAAkAaQBmACAAKAAoACQAZgBhAGkAbAApACAALQBhAG4AZAAgACgAIQAkAGQAZQBiAHUAZwApACkAIAB7ACAARgBhAGkAbAAoACIASQBuAHYAYQBsAGkAZAAgAGYAaQBsAGUAcwAgAHAAcgBlAHMAZQBuAHQAIgApACAAfQANAAoADQAKAAkACQAkAHMAdABhAHQAdQBzACAALQBNAGEAdABjAGgAIAAnAF4AaQAnACAALQBSAGUAcABsAGEAYwBlACAAJwBeAGkAXABzACsAJwAgAC0ATgBvAHQATQBhAHQAYwBoACAAJwBeAEwAbwBjAGEAdABpAG8AbgBzAC4AdAB4AHQAJAAnACAAfAAgAHIAbQAgAC0AcgBlAGMAdQByAHMAZQAgAC0AZgBvAHIAYwBlAA0ACgAJAAkAaQBmACAAKAAhACQAPwApACAAewAgAEYAYQBpAGwAKAAiAEYAYQBpAGwAZQBkACAAdABvACAAZABlAGwAZQB0AGUAIABmAGkAbABlAHMALgAiACkAIAB9AA0ACgAJAH0ADQAKAAkAcwB2AG4AIAB1AHAAIAAtAC0AbgBvAG4ALQBpAG4AdABlAHIAYQBjAHQAaQB2AGUADQAKAAkAcwB2AG4AIABjAGwAZQBhAG4AdQBwACAALQAtAG4AbwBuAC0AaQBuAHQAZQByAGEAYwB0AGkAdgBlAA0ACgB9AA0ACgANAAoARgB1AG4AYwB0AGkAbwBuACAAQgB1AGkAbABkACAAKAApAA0ACgB7AA0ACgAJACQAZABlAHYAZQBuAHYAIAA9ACAAIgBDADoAXABQAHIAbwBnAHIAYQBtACAARgBpAGwAZQBzACAAKAB4ADgANgApAFwATQBpAGMAcgBvAHMAbwBmAHQAIABWAGkAcwB1AGEAbAAgAFMAdAB1AGQAaQBvACAAMQAyAC4AMABcAEMAbwBtAG0AbwBuADcAXABJAEQARQBcAGQAZQB2AGUAbgB2AC4AYwBvAG0AIgANAAoACQAkAHMAbwBsAHUAdABpAG8AbgAgAD0AIAAiAE4AZQBvAEUAZABpAHQALgBzAGwAbgAiAA0ACgAJACQAYwBvAG4AZgBpAGcAdQByAGEAdABpAG8AbgAgAD0AIAAiAFIAZQBsAGUAYQBzAGUAIgANAAoACQAkAHAAbABhAHQAZgBvAHIAbQAgAD0AIAAiAHgANgA0ACIADQAKAAkAJABiAHUAaQBsAGQAZABpAHIAIAA9ACAAIgBiAGkAbgBcACQAYwBvAG4AZgBpAGcAdQByAGEAdABpAG8AbgAuACQAcABsAGEAdABmAG8AcgBtACIADQAKAA0ACgAJAEkAbgB2AG8AawBlAC0ARQB4AHAAcgBlAHMAcwBpAG8AbgAgACcAJgAgACIAJABkAGUAdgBlAG4AdgAiACAAIgAkAHMAbwBsAHUAdABpAG8AbgAiACAALwBiAHUAaQBsAGQAIAAiACQAYwBvAG4AZgBpAGcAdQByAGEAdABpAG8AbgB8ACQAcABsAGEAdABmAG8AcgBtACIAIAAvAG8AdQB0ACAAQgB1AGkAbABkAC4AbABvAGcAJwANAAoACQBpAGYAIAAoACQATABBAFMAVABFAFgASQBUAEMATwBEAEUAIAAtAG4AZQAgADAAKQAgAHsAIABGAGEAaQBsACgAIgBGAGEAaQBsAGUAZAAgAHQAbwAgAGIAdQBpAGwAZAAuACIAKQAgAH0ADQAKAA0ACgAJAHIAbQAgACQAYgB1AGkAbABkAGQAaQByAFwAKgAuAHAAZABiAA0ACgAJAHIAbQAgACQAYgB1AGkAbABkAGQAaQByAFwAKgAuAG0AZQB0AGEAZwBlAG4ADQAKAAkAcgBtACAAJABiAHUAaQBsAGQAZABpAHIAXAAqAC4AaQBsAGsADQAKAAkAcgBtACAAJABiAHUAaQBsAGQAZABpAHIAXAAqAC4AbABpAGIADQAKAAkAcgBtACAAJABiAHUAaQBsAGQAZABpAHIAXAAqAC4AeABtAGwADQAKAA0ACgAJACYAIAAiAEMAOgBcAFcAaQBuAGQAbwB3AHMAXABNAGkAYwByAG8AcwBvAGYAdAAuAE4ARQBUAFwARgByAGEAbQBlAHcAbwByAGsANgA0AFwAdgA0AC4AMAAuADMAMAAzADEAOQBcAG4AZwBlAG4ALgBlAHgAZQAiACAAaQBuAHMAdABhAGwAbAAgACIAYgBpAG4AXABSAGUAbABlAGEAcwBlAC4AeAA2ADQAXABOAGUAbwBFAGQAaQB0AC4AZQB4AGUAIgANAAoADQAKAAkAaQBmACAAKABUAGUAcwB0AC0AUABhAHQAaAAgACIATABvAGMAYQB0AGkAbwBuAHMALgB0AHgAdAAiACkADQAKAAkAewANAAoACQAJACQAbABvAGMAYQB0AGkAbwBuAHMAIAA9ACAARwBlAHQALQBDAG8AbgB0AGUAbgB0ACAAIgBMAG8AYwBhAHQAaQBvAG4AcwAuAHQAeAB0ACIADQAKAAkACQBGAG8AcgBFAGEAYwBoACAAKAAkAGwAbwBjAGEAdABpAG8AbgAgAGkAbgAgACQAbABvAGMAYQB0AGkAbwBuAHMAKQANAAoACQAJAHsADQAKAAkACQAJAGkAZgAgACgAWwBzAHQAcgBpAG4AZwBdADoAOgBJAHMATgB1AGwAbABPAHIARQBtAHAAdAB5ACgAJABsAG8AYwBhAHQAaQBvAG4AKQApACAAewAgAGMAbwBuAHQAaQBuAHUAZQA7ACAAfQANAAoACQAJAAkAcgBvAGIAbwBjAG8AcAB5AC4AZQB4AGUAIAAvAGUAIAAvAG4AZABsACAALwByADoAMAAgACIAJABiAHUAaQBsAGQAZABpAHIAIgAgACIAJABsAG8AYwBhAHQAaQBvAG4AIgANAAoACQAJAH0ADQAKAAkAfQANAAoAfQANAAoADQAKAFMAdgBuAEMAbABlAGEAbgBBAG4AZABVAHAAZABhAHQAZQANAAoAQgB1AGkAbABkAA0ACgANAAoAVwByAGkAdABlAC0ASABvAHMAdAAoACIAUwB1AGMAYwBlAHMAcwAhACIAKQANAAoA
