# Eko Lockdown

A third-person open-city game set in Lagos during the early days of an outbreak, built in Unity 6 for the GameUp Academy vibe-coding game jam.

Lagos has Eko Fever. The government says it is under control, and the Taskforce takes anyone with dark veins to the quarantine camp in Ikorodu. You are Dayo, a danfo driver who is already infected. Work odd jobs across the city to raise ₦25,000 for ZX, the one dose that stops the fever, before the Taskforce sees your arm.

## Playing

- **Move:** WASD. **Run:** Shift. **Sneak:** C. **Jump:** Space.
- **Talk / use / doors:** E. **Get in or out of a vehicle:** F. **Horn:** H.
- **Take a street dose:** Q. **Map:** M (scroll to zoom). **Radio:** R / T.
- **Camera:** right mouse drag or arrow keys. Scroll to zoom.
- **Phone (landscape):** left thumb is a joystick (push to the edge to run, or drive). Drag with the right thumb to look, pinch to zoom. The buttons on the right change with what you are doing. Tap the minimap for the full map.

Your fever climbs on its own. Past the white line your veins show, and Taskforce officers in orange vests will chase you. They are slower than your sprint and cannot catch a car. If they catch you, you are arrested: game over, with a retry from your last checkpoint.

## What is in it

- Six districts (Ikeja, Mushin, Yaba, Ajegunle, Apapa, Lekki) joined by the Lekki Link Bridge, with traffic, pedestrians and drivable danfos, okadas, kekes, cars and trucks.
- A 16-step story with jobs, a bar and a hospital you can walk into, a stealth section at the port, and two endings.
- A radio with three music stations and a news station.
- Everything visual is generated in code at runtime: there are no 3D model or texture files.

## Building

Open the folder in Unity 6000.3. The scene is generated on load. From the command line:

```
Unity -batchmode -projectPath . -executeMethod BuildTools.BuildWebGL   # -> Builds/WebGL
Unity -batchmode -projectPath . -executeMethod BuildTools.BuildMac     # -> Builds/Mac
Unity -batchmode -nographics -projectPath . -executeMethod StoryTest.Run   # walks the whole story headlessly
```

Serve `Builds/WebGL` with any static file server, for example `python3 -m http.server 8080`.

## Credits

Music by Alex Morgan, via Free Music Archive, licensed CC BY 4.0. Track list and links: [MUSIC_CREDITS.txt](MUSIC_CREDITS.txt).
