@echo off
echo Sending custom icon payload to CUSA04294 on 192.168.0.226:9090
"D:\PS4 PKGs\Vue-Icons-main\Vue-Icons-main\PS4 App Icon Studio\bin\Debug\net8.0-windows\PayloadKit\socat\socat.exe" -t 99999999 - TCP:192.168.0.226:9090 < "D:\PS4 PKGs\Vue-Icons-main\Vue-Icons-main\PS4 App Icon Studio\bin\Debug\net8.0-windows\generated\Img_24r_custom_CUSA04294.elf"
echo Done. If the PS4 icon did not update immediately, back out and refresh the home screen.
pause