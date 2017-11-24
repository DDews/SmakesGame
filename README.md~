This project has evolved into a much more full-featured unity asset available here: http://u3d.as/qhh

# NATPunchthroughClient
An example implementation of NAT Punchthrough combined with Unity's UNet HLAPI.

## How it Works
Punchthrough is accomplished using RakNet's NATPunchthroughClient plugin.
Once the connection is established all the HLAPI stuff works as normal.

To read more about RakNet see: http://www.jenkinssoftware.com/features.html
and: https://github.com/OculusVR/RakNet

To read more about NAT Punchthrough see: http://www.raknet.net/raknet/manual/natpunchthrough.html

RakNet DLL's are included for 32bit and 64bit windows, but it shouldn't be a problem to generate libraries for mac / linux as well.

Unity's Matchmaking system is abused in order to pass around connection info in this example 
so you will have to have access to the Unity's [Online Services](https://unity3d.com/services/multiplayer) even though no
match is ever actually joined and no bandwidth is used. 
Make sure to [create a project](https://developer.cloud.unity3d.com/projects) and link it in the editor.

## How to Use
NAT Punchthrough requires an external server, often referred to as a Facilitator, in order to broker the connections between peers.

1. Get the Facilitator running
  1. Beg, borrow, or steal a linux server
  2. Grab the RakNet source https://github.com/OculusVR/RakNet
  3. Compile the NATCompleteServer example
    - Hint: Read the README.md file included with the RakNet source, it tells you the exact command you need
  4. Start the server
2. Set up the Client
  1. Open this project in Unity
  2. Select the NetworkManager
  3. Enter the IP address of your linux server where it says Facilitator IP on the NATHelper component
3. Test it
  1. Create a build
  2. Put the build on a computer outside your local network that would not normally be able to directly connect to you
  3. Click "Play" in the editor
    - You will see a "Connected" message printed in the console if the Facilitator was connected to
  4. Click "Host"
    - If succesful, a pink square will appear that you can control with the arrow keys
  5. Run the other build. Click "Join server"
  6. Pour yourself a beer.
  
## But Why?
The primary motivation for creating this was so that I could be less dependent on Unity's relay servers. 
For one, they are bandwidth restricted, and they seem to introduce some pretty high latency. Plus it's just one more point of failure
that us lowly developers have absolutely no control over. This solution, when combined with the automatic port forwarding provided
by something like [Open.NAT](https://github.com/lontivero/Open.NAT) or [miniupnp](http://miniupnp.free.fr/) should mean that
connecting through Unity's relay servers is almost never necessary.

## Known Issues
- The biggest issue is a lack of testing. I've only tested on one set of routers and only with 3 clients connected at once.
- This example forces NAT Punchthrough even when a direct connection is possible. In a real world implementation you would want to
attempt a direct connection first, if that fails attempt to connect via NAT Punchthrough, and if that also fails fall back to
connecting via Unity's relay servers.
- I removed all of the automatic port forwarding stuff from here just to keep the code as simple as possible, 
but a real world implementation would also attempt to forward ports using something like Open.NAT before resorting to NAT Punchthrough

## More thoughts
RakNet also comes with a Relay server and some other cool features. It would probably be possible to completely replace
Unity's relay system with RakNet relays. That way there is no dependence on Unity's Cloud services at all, as long as you're 
willing to host and maintain your own relay servers that is.
