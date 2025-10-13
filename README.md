# 🌙 Found Again

A **2D side-scrolling adventure** built with Unity — about discovery, memory, and finding meaning in small moments.  
This project emphasizes **clean architecture**, **readable code**, and **modular systems** for a smooth development workflow.

---

## 🧩 Overview

**Found Again** is a pixel-art inspired side-scroller that focuses on exploration and atmosphere rather than combat.  
The goal of this repository is to demonstrate solid Unity project structure and maintainable C# scripting for small-team or solo dev projects.

---

## 🗂️ Project Structure

```
Assets/
├── Art/ # Sprites, tilesets, UI, and animations
│ ├── Characters/ # Player + NPC sprites and animations
│ ├── Environment/ # Tilesets, parallax backgrounds
│ ├── Effects/ # Particle sprites, shaders
│ └── UI/ # HUD, menus, icons
│
├── Tiles/
│   ├── Palettes/            # Tile Palettes for painting
│   ├── RuleTiles/           # RuleTile assets (auto-tiling)
│   ├── TileAssets/          # Generated Tile assets from sprites
│   └── Sprites/             # Source tileset textures (sliced)
│
├── Audio/ # Sound effects and background music
│ ├── SFX/
│ └── BGM/
│
├── Materials/ # Materials for shaders and visual effects
│
├── Prefabs/ # Reusable prefabs (player, triggers, enemies, UI elements)
│
├── Scenes/ # Game scenes
│ ├── MainMenu.unity
│ ├── Level01.unity
│ └── TestRoom.unity
│
├── Scripts/ # All C# scripts
│ ├── Core/ # GameManager, InputManager, SceneLoader
│ ├── Player/ # Movement, animation, interaction logic
│ ├── Enemy/ # Patrol, idle, and trigger systems (if applicable)
│ ├── World/ # Environment logic, triggers, parallax control
│ ├── UI/ # Menu and dialogue UI controllers
│ └── Utils/ # Helper scripts, constants, extensions
│
├── Settings/ # ScriptableObjects for global configuration
│
└── Resources/ # Assets loaded dynamically (if needed)
```

ProjectSettings/ # Unity project and build settings
Packages/ # Dependency definitions (manifest.json)


---

## ✨ Features

- 2D **side-scrolling exploration** gameplay  
- Layered **parallax backgrounds** for depth  
- Modular and reusable **prefab architecture**  
- **ScriptableObject**-driven configurations for tuning gameplay  
- Organized code by responsibility (Core / Player / UI / World)

---

## ⚙️ Setup Instructions

1. **Clone the Repository**
   ```bash
   git clone https://github.com/hayyaoe/found-again.git

    Open in Unity

        Open the project folder in Unity Hub

    Play the Game

        Load Scenes

        Press ▶️ in the Unity Editor to start

🧱 Development Notes

    Language: C# (Unity API)

    Code Style: Follows Unity conventions (PascalCase for public, camelCase for private)

    Version Control: .gitignore excludes Library/, Logs/, and Builds/

    Assets: All original or free-to-use under appropriate licenses

    Builds: Exported to /Builds folder (ignored in git)

🗺️ Roadmap

Add collectible memory fragments system

Implement save/load checkpoints

Polish UI transitions

    Add dynamic lighting for emotional ambience

📜 License

This project is released under the MIT License.
You can use, modify, and distribute it freely — please credit Found Again.


