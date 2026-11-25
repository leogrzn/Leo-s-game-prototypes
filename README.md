# Leo's game prototypes

Small game prototypes.

## Hold the Mine

An isometric tower defense / management game made in Unity 6. You are the soldier in charge of a gold mine under siege: fight in real time, place barricades, soldiers and towers, upgrade them, keep the miners fed and morale high, and survive 10 raider waves coming from the desert, the forest and the road.

### ▶ Play in your browser

**https://leogrzn.github.io/Leo-s-game-prototypes/HoldTheMine/**

Use a desktop browser with keyboard and mouse. Click inside the game once so it gets keyboard focus.

### Controls

| Input | Action |
|---|---|
| WASD | Move |
| Mouse / Left click | Aim / shoot |
| R | Reload |
| 1 / 2 / 3 | Build a barricade / soldier / tower. Left click places it near you, right click cancels, mouse wheel rotates |
| 4 | Order food (it arrives on the next food truck) |
| 5 | Hire a miner |
| Left click a unit or building | Select it: info panel, upgrade button |
| Right click the ground | Send the selected soldier there; he holds that position |
| X | Release the selected soldier back to autonomous mode |
| U | Upgrade the selection (up to level III) |
| M | Select the gold mine |
| Q | Rally nearby troops (faster fire, morale boost, cooldown) |
| F | Send nearby soldiers to the mouse position |
| Tab | Shop / management panel |
| Esc | Cancel build, deselect, or pause |

In the browser, Esc also leaves full screen. If Tab moves the page focus instead of opening the shop, click the game first.

### Open the project in Unity 6

1. Install **Unity 6000.6.9f1** (Unity 6.6) with Unity Hub. Add the **Web Build Support** module if you want to make web builds.
2. Clone this repository, then in Unity Hub choose **Add → Add project from disk** and select the `HoldTheMine/` folder.
3. Open the project. The first import takes a few minutes because `Library/` is not in git.
4. Open `Assets/Scenes/HoldTheMine.unity` and press **Play**.

Useful things in the project:

- `Assets/Config/GameConfig.asset` holds every balance value: prices, health, damage, wave sizes, truck timings, morale effects and upgrade levels.
- **Hold the Mine → Build Game** regenerates the materials, prefabs, scene, NavMesh and UI from `Assets/Scripts/Editor/HoldTheMineBuilder*.cs`. It keeps your config and overwrites the scene.
- **Hold the Mine → Build WebGL** makes a browser build in `HoldTheMine/Builds/WebGL` with compression disabled, which GitHub Pages needs. The published build is in `docs/HoldTheMine/`.

To make the web build from the command line with the [Unity CLI](https://unity.com/):

```bash
unity build HoldTheMine --target WebGL \
  --execute-method HoldTheMine.EditorTools.WebGLBuild.Build \
  --output docs/HoldTheMine --allow-dirty-build
```

## Repository layout

| Path | What it is |
|---|---|
| `HoldTheMine/` | Unity 6 project for Hold the Mine |
| `docs/` | GitHub Pages site (served from `main` / `docs`) |
| `docs/HoldTheMine/` | WebGL build of Hold the Mine |
| `OneWayTicket/` | One-Way Ticket, a single-page web prototype |
