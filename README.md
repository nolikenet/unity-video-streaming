# unity-video-streaming
Provides ability to stream videos from Unity3d camera to the file or over network

# Overview
- currently works with Unity version 6 and above
- requires pre-installed ffmpeg on your system
- currently supports only streaming to file, UDP streaming feature is going to be added soon
- tested on Linux and Mac, should work on Windows too if ffmpeg is properly installed.

# How to use
- install ffmpeg on your system
- copy contents to your Plugins or Assets/Scripts folder
- add StreamCapture.cs to any suitable gameobject in your scene
- define settings you would like to use. These settings are exposed to public fields in the unity editor on StreamCapture component. Do not forget to set your camera reference. 
- Use StartStreaming() and StopStreaming() public methods to capture stream.
