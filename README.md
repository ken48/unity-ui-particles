# UnityUiParticles
Unity ParticleSystem for built-in UI

[![](https://img.shields.io/badge/requirement-Unity%202022.2%2B-green.svg)](https://unity.com)

The baked particle mesh is rendered by `CanvasRenderer`, so it participates in Canvas hierarchy
sorting and supports `Mask`/`RectMask2D` when used with a compatible UI shader. Particle size and
speed use the Canvas coordinate system.

## Usage
Just add ParticleSystemMeshGenerator to gameobject with ParticleSystem.

Disable the regular `ParticleSystemRenderer` and assign the particle materials to
`ParticleSystemMeshGenerator`. The inspector reports incompatible settings.

## Materials and Canvas sorting
Use a UI-compatible material (for example, `UI/Default`). Built-in particle shaders may ignore Canvas
hierarchy order and UI masks.

## Restrictions

* Texture Sheet Animation `Sprites` mode is unsupported; use `Grid` with a texture atlas.
* The internal camera used for mesh baking is orthographic.

## Requirements
The UPM package targets Unity **2022.2+**. Unity 6 uses the current `ParticleSystemBakeMeshOptions`
API; older supported editors use the legacy overload for compatibility.

The renderer is based on the particle mesh baking APIs:
* BakeMesh
* BakeTrailsMesh

## License
* MIT
