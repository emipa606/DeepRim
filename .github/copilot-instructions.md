# GitHub Copilot Instructions for DeepRim (Continued)

## Mod Overview and Purpose

DeepRim is a RimWorld mod that allows players to construct mining shafts, enabling them to drill down and explore new underground maps filled with resources. The mod introduces various features to enhance underground exploration and resource management, such as automatic item transfer between layers, power distribution, and manual drilling options.

## Key Features and Systems

1. **Mining Shafts**: Players can build mining shafts that drill down into the earth, revealing new underground maps.
2. **Automatic Item Transfer**: Items can be automatically transferred between layers using storage buildings.
3. **Power Distribution**: Power can be transferred from the surface to lower layers, allowing for more efficient operation of mining equipment.
4. **Manual Drilling**: An option for manual drilling when the "Low-tech" setting is enabled.
5. **Layer Renaming**: Players can rename underground layers for better organization.
6. **Cave Biome Generation**: Compatibility with mods that generate cave biomes, such as Biomes! Caverns and CaveBiome.
7. **Ore Density Adjustment**: Players can adjust the ore density of underground layers.

## Coding Patterns and Conventions

- **Consistent Naming**: Variables, methods, and classes should be named descriptively and consistently.
- **Modular Code**: Code should be organized into logical modules, such as separate classes for different features (e.g., `Building_MiningShaft`, `Dialog_RenameLayer`).
- **Comments and Documentation**: Include comments and documentation to explain complex logic and system interactions.
- **Error Handling**: Implement robust error handling to manage unexpected scenarios gracefully.

## XML Integration

DeepRim uses XML files to define various game elements such as biomes, job definitions, map generators, and thing definitions. When working with XML files, ensure that:

- **Correct Def Types**: Each XML file should contain the appropriate def type (e.g., `BiomeDef`, `JobDef`, `MapGeneratorDef`, `ThingDef`).
- **Valid Definitions**: Definitions should be valid and follow RimWorld's XML schema.
- **Consistent Attributes**: Attributes should be consistent and follow RimWorld's conventions.

## Harmony Patching

DeepRim uses Harmony for patching existing RimWorld code to add new functionality. When creating Harmony patches, ensure that:

- **Patch Location**: Patches should be located in the appropriate namespace and class.
- **Patch Metadata**: Use the `[HarmonyPatch]` attribute to specify the target method and patch type.
- **Patch Logic**: Implement the patch logic carefully to avoid unintended side effects.

## Suggestions for Copilot

- **Feature Requests**: If you have a specific feature in mind, feel free to suggest it in the GitHub Discussions.
- **Code Contributions**: If you have improvements or bug fixes, consider contributing your code to the project.
- **Error Reporting**: If you encounter any issues, please report them using the provided Discord channel or Log Uploader.
- **Mod Sorting**: Use RimSort to manage your mods for better compatibility and performance.

By following these guidelines, you can contribute effectively to the DeepRim (Continued) mod and enhance your experience in RimWorld.

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
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

