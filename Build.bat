@ECHO OFF
powershell -EncodedCommand IwAgAEUAbgBjAG8AZABpAG4AZwA6ACAAQgBhAHMAZQA2ADQAIABVAFQARgAxADYAIABMAGkAdAB0AGwAZQAgAGUAbgBkAGkAYQBuAA0ACgANAAoAIwAgAFQAbwAgAHIAdQBuACAAbwBuACAAYwBvAG0AbQBhAG4AZAAgAGwAaQBuAGUAOgAgAA0ACgAjACAAcABvAHcAZQByAHMAaABlAGwAbAAgAC0AQwBvAG0AbQBhAG4AZAAgAC0AIAA8ACAAQgB1AGkAbABkAC4AYgBhAHQADQAKAA0ACgAjACQAZABlAGIAdQBnACAAPQAgADEADQAKAA0ACgBGAHUAbgBjAHQAaQBvAG4AIABGAGEAaQBsACAAKAAkAGUAcgByAG8AcgApAA0ACgB7AA0ACgAJAFcAcgBpAHQAZQAtAEgAbwBzAHQAKAAkAGUAcgByAG8AcgApAA0ACgAJAFcAcgBpAHQAZQAtAEgAbwBzAHQAKAAiAFAAcgBlAHMAcwAgAGEAbgB5ACAAawBlAHkAIAB0AG8AIABlAHgAaQB0AC4AIgApAA0ACgAJACQAeAAgAD0AIAAkAGgAbwBzAHQALgBVAEkALgBSAGEAdwBVAEkALgBSAGUAYQBkAEsAZQB5ACgAIgBOAG8ARQBjAGgAbwAsAEkAbgBjAGwAdQBkAGUASwBlAHkARABvAHcAbgAiACkADQAKAAkAZQB4AGkAdAANAAoAfQANAAoADQAKAEYAdQBuAGMAdABpAG8AbgAgAFMAdgBuAEMAbABlAGEAbgBBAG4AZABVAHAAZABhAHQAZQAgACgAKQANAAoAewANAAoACQAkAGYAYQBpAGwAIAA9ACAAMAA7AA0ACgAJACQAcwB0AGEAdAB1AHMAIAA9ACAAcwB2AG4AIABzAHQAYQB0AHUAcwAgAC0ALQBuAG8ALQBpAGcAbgBvAHIAZQANAAoACQBpAGYAIAAoACQAcwB0AGEAdAB1AHMAKQANAAoACQB7AA0ACgAJAAkARgBvAHIARQBhAGMAaAAgACgAJABsAGkAbgBlACAAaQBuACAAJABzAHQAYQB0AHUAcwApAA0ACgAJAAkAewANAAoACQAJAAkAaQBmACAAKAAkAGwAaQBuAGUAIAAtAE4AbwB0AE0AYQB0AGMAaAAgACcAXgBpACcAKQANAAoACQAJAAkAewANAAoACQAJAAkACQBXAHIAaQB0AGUALQBIAG8AcwB0ACgAIgAkAGwAaQBuAGUAIgApAA0ACgAJAAkACQAJACQAZgBhAGkAbAAgAD0AIAAxADsADQAKAAkACQAJAH0ADQAKAAkACQB9AA0ACgANAAoACQAJAGkAZgAgACgAKAAkAGYAYQBpAGwAKQAgAC0AYQBuAGQAIAAoACEAJABkAGUAYgB1AGcAKQApACAAewAgAEYAYQBpAGwAKAAiAEkAbgB2AGEAbABpAGQAIABmAGkAbABlAHMAIABwAHIAZQBzAGUAbgB0ACIAKQAgAH0ADQAKAA0ACgAJAAkAJABzAHQAYQB0AHUAcwAgAC0ATQBhAHQAYwBoACAAJwBeAGkAJwAgAC0AUgBlAHAAbABhAGMAZQAgACcAXgBpAFwAcwArACcAIAB8ACAAcgBtACAALQByAGUAYwB1AHIAcwBlACAALQBmAG8AcgBjAGUADQAKAAkACQBpAGYAIAAoACEAJAA/ACkAIAB7ACAARgBhAGkAbAAoACIARgBhAGkAbABlAGQAIAB0AG8AIABkAGUAbABlAHQAZQAgAGYAaQBsAGUAcwAuACIAKQAgAH0ADQAKAAkAfQANAAoACQBzAHYAbgAgAHUAcAAgAC0ALQBuAG8AbgAtAGkAbgB0AGUAcgBhAGMAdABpAHYAZQANAAoACQBzAHYAbgAgAGMAbABlAGEAbgB1AHAAIAAtAC0AbgBvAG4ALQBpAG4AdABlAHIAYQBjAHQAaQB2AGUADQAKAH0ADQAKAA0ACgBGAHUAbgBjAHQAaQBvAG4AIABCAHUAaQBsAGQAIAAoACkADQAKAHsADQAKAAkAJABkAGUAdgBlAG4AdgAgAD0AIAAiAEMAOgBcAFAAcgBvAGcAcgBhAG0AIABGAGkAbABlAHMAIAAoAHgAOAA2ACkAXABNAGkAYwByAG8AcwBvAGYAdAAgAFYAaQBzAHUAYQBsACAAUwB0AHUAZABpAG8AIAAxADIALgAwAFwAQwBvAG0AbQBvAG4ANwBcAEkARABFAFwAZABlAHYAZQBuAHYALgBjAG8AbQAiAA0ACgAJACQAcwBvAGwAdQB0AGkAbwBuACAAPQAgACIATgBlAG8ARQBkAGkAdAAuAHMAbABuACIADQAKAAkAJABjAG8AbgBmAGkAZwB1AHIAYQB0AGkAbwBuACAAPQAgACIAUgBlAGwAZQBhAHMAZQAiAA0ACgAJACQAcABsAGEAdABmAG8AcgBtACAAPQAgACIAeAA2ADQAIgANAAoACQAkAGIAdQBpAGwAZABkAGkAcgAgAD0AIAAiAGIAaQBuAFwAJABjAG8AbgBmAGkAZwB1AHIAYQB0AGkAbwBuAC4AJABwAGwAYQB0AGYAbwByAG0AIgANAAoADQAKAAkASQBuAHYAbwBrAGUALQBFAHgAcAByAGUAcwBzAGkAbwBuACAAJwAmACAAIgAkAGQAZQB2AGUAbgB2ACIAIAAiACQAcwBvAGwAdQB0AGkAbwBuACIAIAAvAGIAdQBpAGwAZAAgACIAJABjAG8AbgBmAGkAZwB1AHIAYQB0AGkAbwBuAHwAJABwAGwAYQB0AGYAbwByAG0AIgAgAC8AbwB1AHQAIABCAHUAaQBsAGQALgBsAG8AZwAnAA0ACgAJAGkAZgAgACgAJABMAEEAUwBUAEUAWABJAFQAQwBPAEQARQAgAC0AbgBlACAAMAApACAAewAgAEYAYQBpAGwAKAAiAEYAYQBpAGwAZQBkACAAdABvACAAYgB1AGkAbABkAC4AIgApACAAfQANAAoADQAKAAkAcgBtACAAJABiAHUAaQBsAGQAZABpAHIAXAAqAC4AcABkAGIADQAKAAkAcgBtACAAJABiAHUAaQBsAGQAZABpAHIAXAAqAC4AbQBlAHQAYQBnAGUAbgANAAoACQByAG0AIAAkAGIAdQBpAGwAZABkAGkAcgBcACoALgBpAGwAawANAAoACQByAG0AIAAkAGIAdQBpAGwAZABkAGkAcgBcACoALgBsAGkAYgANAAoADQAKAAkAJgAgACIAQwA6AFwAVwBpAG4AZABvAHcAcwBcAE0AaQBjAHIAbwBzAG8AZgB0AC4ATgBFAFQAXABGAHIAYQBtAGUAdwBvAHIAawA2ADQAXAB2ADQALgAwAC4AMwAwADMAMQA5AFwAbgBnAGUAbgAuAGUAeABlACIAIABpAG4AcwB0AGEAbABsACAAIgBiAGkAbgBcAFIAZQBsAGUAYQBzAGUALgB4ADYANABcAE4AZQBvAEUAZABpAHQALgBlAHgAZQAiAA0ACgB9AA0ACgANAAoAUwB2AG4AQwBsAGUAYQBuAEEAbgBkAFUAcABkAGEAdABlAA0ACgBCAHUAaQBsAGQADQAKAA0ACgBXAHIAaQB0AGUALQBIAG8AcwB0ACgAIgBTAHUAYwBjAGUAcwBzACEAIgApAA0ACgA=
