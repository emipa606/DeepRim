# DeepRim (Continued) - GitHub Copilot Instructions

## Mod Overview and Purpose

DeepRim (Continued) enhances the mining experience in RimWorld, allowing players to construct mining shafts and explore new underground maps rich with resources. This mod updates the original by meowmeow and incorporates new features for better gameplay immersion and quality of life improvements.

## Key Features and Systems

- **Drill Smoke Effect**: The drill now emits smoke, adding a visual cue while drilling.
- **Send Up-button**: Allows easy transfer of materials from underground to the surface through a button interface.
- **Multi-language Support**: Russian translation available, courtesy of WolF8RocK.
- **Custom Map Layer Sizes**: Mod-settings enable players to adjust generated map layer sizes to their preference.
- **Low-tech Mode**: Offers a manual operation mode for drills that do not require power.
- **Automatic Item Transfer**: Seamless item transfer between layers if storage is built both at the surface and underground.
- **Power Transfer System**: Shares power supply between layers, distributing it equitably across all underground levels.
- **New Textures**: Retextures for drills and icons to enhance the mod aesthetics.
- **Layer Renaming**: Improves layer management by naming them.
- **Ore Density Adjustment**: Customize ore density across underground layers.
- **Cave Biome Compatibility**: Generates customized cave biomes when using mods like Biomes! Caverns and CaveBiome.

## Coding Patterns and Conventions

- Follow standard C# and XML naming conventions (PascalCase for classes, camelCase for methods, and UPPER_SNAKE_CASE for constants).
- Organize code by functionality, segregating it into classes with clear responsibilities.
- Use comments to document complex logic and provide context for future developers.

## XML Integration

- Utilize XML for defining various game entities such as biomes and building configurations. Regularly validate XML files with the RimWorld schema.
- Ensure XML files are organized and follow a consistent structure for readability and maintainability.

## Harmony Patching

- Utilize Harmony for patching existing RimWorld methods. Responsible patching ensures compatibility with other mods and prevents unexpected behaviors.
- Always include a descriptive prefix when creating Harmony patches to maintain a unique namespace.
- Properly categorize patches into `Postfix`, `Prefix`, and `Transpiler` to maintain clear code logic.

## Suggestions for Copilot

- **Autogenerate XML Templates**: Leverage Copilot to assist with generating XML templates for new game definitions.
- **Implement New Methods**: Propose new methods for enhanced features such as improved item transfer algorithms or optimized layer generation logic.
- **Suggest Optimizations**: Analyze current implementations and suggest performance optimizations, particularly in code responsible for map generation and resource handling.
- **Code Cleanliness**: Aid in maintaining code cleanliness by automatically formatting and identifying redundant code snippets.

This guide provides the necessary instructions for contributors working with GitHub Copilot on the DeepRim (Continued) mod. This comprehensive coverage supports efficient updates, maintenance, and expansion of the project while ensuring high-quality coding standards.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).

