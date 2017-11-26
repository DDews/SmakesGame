
# NATPunchthroughClient
An example implementation of NAT Punchthrough combined with Unity's UNet HLAPI.

## How it Works
Punchthrough is accomplished using RakNet's NATPunchthroughClient plugin.
Once the connection is established all the HLAPI stuff works as normal.

To read more about RakNet see: http://www.jenkinssoftware.com/features.html
and: https://github.com/OculusVR/RakNet

To read more about NAT Punchthrough see: http://www.raknet.net/raknet/manual/natpunchthrough.html

RakNet DLL's are included for 32bit and 64bit windows, but it shouldn't be a problem to generate libraries for mac / linux as well.

## Smakes
Special thanks to ninjapretzel for the Unity kata and help.
This is a Unity project that lets people host and play my 3DS Homebrew Snakes game on PC over the internet.

## How To Play
Runs on Windows 10. More OS's will be supported in the near future.

Go to [releases](../../releases) to download the Smakes.zip.
Extract all to a folder of your choosing. Run Smakes.exe.
Click Join server. If no rooms are available, try Host instead.

##TODO
- Hide the Host and Join Server buttons, they stick around longer than they should.
- Add Game settings collapsable menu for Host.
- List available rooms if more than one exists.
- Implement joinable rounds.

