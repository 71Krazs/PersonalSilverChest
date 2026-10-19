# PersonalSilverChest

Adds a buildable **PersonalSilverChest** to the Hammer's Furniture tab. It uses the Black Metal Chest model, with silver metal and the Personal Chest's finewood color. It retains the Personal Chest's owner-only access and removal restriction, and uses the live Reinforced Chest inventory dimensions (normally 24 slots).

The chest is uniformly scaled to 90% of the Black Metal Chest's original size.

## Recipe

- 10 Fine Wood
- 2 Iron
- 10 Silver
- Forge required

Install `PersonalSilverChest.dll` on the server and every client. Jotunn is required.

## Download

Download the install archive from [GitHub Releases](https://github.com/71Krazs/PersonalSilverChest/releases). Install the DLL on the server and every client. Jotunn is required on both.

## License

MIT. See [LICENSE](LICENSE). Valheim and BepInEx remain the property of their respective owners and are not included here.

## Build

```powershell
dotnet build -c Release -p:ValheimManagedDir="H:\Codex\Valheim\Client\Valheim\valheim_Data\Managed" -p:BepInExCoreDir="H:\Codex\Valheim\Client\Valheim\BepInEx\core" -p:JotunnDir="H:\Codex\Valheim\Client\Valheim\BepInEx\plugins\Jotunn"
```
