# PixelLab Import Plan

This project is prepared for generated PixelLab assets with this folder layout:

- `Assets/Art/Characters`
- `Assets/Art/Environment`
- `Assets/Art/Props`
- `Assets/Art/Animations`
- `Assets/Art/Materials`

Recommended first batch:

1. Player character
2. Enemy grunt character
3. Crate replacement
4. Breakable wall replacement
5. Bomb replacement
6. Explosion VFX source art

Import targets:

- player visuals should replace the primitive capsule spawned in `Assets/Scripts/Bootstrap/MatchBootstrap.cs`
- enemy visuals should replace the primitive capsule spawned in `Assets/Scripts/Bootstrap/MatchBootstrap.cs`
- arena props should replace generated cubes from `Assets/Scripts/Gameplay/ArenaGrid.cs`

Current session note:

The PixelLab MCP server has been added to local Codex config, but MCP tools are not exposed to this already-running session. Restart the Codex session in this project to make the new MCP server available, then generation can start.
