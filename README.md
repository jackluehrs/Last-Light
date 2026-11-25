# Last Light: Tower Defense + Dungeon Crawler

> **How this was made:** I did not write the code in this project. I came up with the game idea and design, then directed [Claude Code](https://claude.com/claude-code) (Anthropic's AI coding agent) to build it: describing features, playtesting each version in Unity, and asking for changes and fixes. All of the C# scripts were written by Claude. This repo is my first experiment with Unity and with AI-assisted game development.

A 2D pixel-art game that mixes **tower defense** with **action RPG** exploration. You play as a hero who defends a central Hub from waves of monsters while venturing out to mine ore, level up, and clear dungeons.

## Gameplay
- **Defend the Hub:** Build towers and miners on a grid around your base. If the Hub falls, the game is over.
- **Gather resources:** The Hub makes a little gold over time. Mine gold ore near the base, and special ores (Iron, Soul Crystal, Bloodstone, Emberite) in the wild lands.
- **Explore wild lands:** Dark Forest, Crypt, Bloodmoor and Ashlands each have their own monsters, ore and a locked area to unlock.
- **Clear dungeons:** Each land has a dungeon with a chain of rooms and a boss. Beat it for gold, ore and a skill point, but your base still has to defend itself while you're inside.
- **Grow stronger:** Upgrade the Hub to unlock new buildings, upgrade towers, and spend skill points in a skill tree. Hero classes include Knight, Ranger and Pyromancer.
- **Day/night cycle:** Night lighting, fog of war, and a torch that lights up the area around the hero.

## Controls
| Key | Action |
|---|---|
| WASD / Arrow keys | Move |
| Space | Attack toward the mouse (hold to keep attacking) |
| Q / F | Class abilities |
| E | Upgrade a nearby building / enter or leave a dungeon |
| H | Recall home (Hub level 2+) |
| K | Open the skill tree |
| 1-6 | Pick a building: Crystal Tower, Miner, Cannon, Frost Spire, Storm Spire, Inferno |
| Left click | Place building |
| Right click / Esc | Cancel; with nothing selected, right-click a building to sell it for 50% |

## Running it
1. Install **Unity 6** (project editor version `6000.6.9f1`) through Unity Hub.
2. Clone this repo and open the folder in Unity Hub.
3. Open `Assets/Scenes/SampleScene.unity` and press **Play**. The game manager creates itself automatically, or you can add it with **Tools > Create Tower Defense Game** to tweak values in the Inspector.

## Project structure
- `Assets/Scripts/TowerDefense/`: core game: GameManager, buildings and towers, enemies and spawner, bosses and dungeons, hero, skill tree, UI
- `Assets/Scripts/Art/`: all sprites are generated in code as pixel art (no image assets)
- `Assets/Scripts/MapGenerator.cs`: procedural map with different lands (biomes)
- `Assets/Scripts/Editor/`: Unity menu tools for setting up the scene

## Credits
- Game idea, design direction and playtesting: Jack Luehrs
- Code: written by Claude Code (Anthropic), at my direction
- Font: [Pixelify Sans](https://fonts.google.com/specimen/Pixelify+Sans), SIL Open Font License
