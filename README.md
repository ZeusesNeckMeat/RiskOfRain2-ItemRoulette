## About

This mod allows you to randomize your run in a unique way. You can specify the maximum number of items that each tier will be allowed to generate. For example, say you only want 10 different Tier 1/White items to spawn then you can set the value in the config to 10. When the new run starts, it will select 10 white items at random and add those to the available drop items list, so those 10 white items will be the only white items you will see this run.

## Important Commands
* F9 - This will print out all of the available items in the current loot pool to the console, if you so wish to know what was generated
* F10 - This will force the config file to refresh, and update your currently running game with the new values you added to the config. This serves as a way to make changes however you please without needing to close and relaunch the game

## How the item pool limitation works

There is a section in the config file called `New Total Item Counts`. This is where you will specify the total number of items for each tier you would like to be available to you throughout the run. If you want all items available for that tier, set the value to `0`.

If any of these values are "invalid", i.e. less than 0 or more than the total number of items in that tier, then the mod will just default to using all available items for that tier, so there will be no limit.

The list of available tiers to modify are:
* Tier 1 (White)
* Tier 2 (Green)
* Tier 3 (Red)
* Boss
* Lunar

**Important info:**
* Boss items can be limited, however this won't actually affect drop from a boss. These drops are guaranteed drops by those bosses. Instead any other source where you would get a boss item, such as a 3D Printer will use this list

## Customizing items by tag

There is a section in the config file called `Item Tag Percents`. In this section, you can customize how many Healing, Utility, and Damage items drop in your runs. If you want half of your items to be Healing and the other half to be Damage then you would set `HealingTypePercentage` to 50, and `DamageTypePercentage` to 50. 

0 means you want 0 items of that type. The sum of the three fields, obviously, should not exceed 100.

If you really don't want to do math and you really only care about a specific tag, say `Damage`, then you can set `Damage` to your desired percent, and then set the other two to -1. When the config file regenerates it will default the values that were at `-1` to split the remaining percentage down the middle. 
For example -> `43, -1, -1` would become `43, 28.5, 28.5`

**Important info:**
* If the sum of the three fields ever does exceed 100, then the config will be updated to set all three values to 33%
* This section does not modify the `Boss` or `Lunar` tiers. This is only for Tiers 1-3.
* The values you have in your config for Tiers 1, 2, and 3 WILL take precendence over the Type percentages. For example if you have 8 items allowed between Tiers 1, 2, and 3 and you have `DamageTypePercentage` = 25, `UtilityTypePercentage` = 25, and `HealingTypePercentage` = 0, then 2/8 items will be Damage, 2/8 items will be Utility, and the other 4/8 items will be selected at random from all three types. 
* Each Tag (Damage, Healing, Utility) only has a set amount of items in those categories. Say the `Utility` tag only has 10 items, you have `UtilityTypePercentage` set to 50, and you have a combined total of 30 items allowed in your run between Tier 1, Tier 2, and Tier 3, then all 10 Utility items will be added to the pool, and the remaining 5 items that would have been Utility will be pulled randomly from the other two tags. 
* If the percentage is too low for a Tag, it's entirely possible an item doesn't generate at all for this tag

## Changing when the item pool can refresh

There is a config option called `ItemPoolRefreshOptions` under the `General` section. This will allow you to customize when this mod refreshes the item drop pool. 

The three current available choices are:
* NewRun - Only refresh on the start of a brand new run
* EachStage - Refresh item pool at the start of each stage
* EachLoop - Refresh item pool at the start of each new loop

These options should provide a little more variety for longer runs, if wanted. EachStage is probably not needed as it's really not that different from vanilla, but I threw it in there just in case.

## Syncing void items

There is a config option called `ShouldSyncVoidItems` under the `General` section. This will make it so any non-void items generated for the run through this mod will have their void item counterpart loaded. Only those void items will show up, unless the random items generated for a tier don't have any void counterpart between all items within that tier. In this situation a single void item from that tier is pulled at random instead in order to prevent weirdness with having an empty item tier.

## Adding Guaranteed Items to the pool

There is a new section in the config file called `Guaranteed Items`. This section will show you all of the items generated for each tier with a number next to them. You can specify any number of items per tier, and you would place the numbers of that items into the config setting. These items will be added as "extra" items to that tier. So if you specify 5 items for `Tier 1` and you have `Paul's Goat Hoof` as your guaranteed item, then `Tier 1` will have 6 total items.

Along with being able to set guaranteed items per tier, there is also a setting for whether or not you want the items you've set as guaranteed to be the ***only*** items in that tier. So for whatever reason if you want to have `Soldier's Syringe` as your only `Tier 1` item, then you would add it's number to the appropriate setting, and set `Should only use guaranteed items for Tier 1` to `true`. This will ignore your `Tier 1` settings while you have both `Should only use guaranteed items for Tier 1` set to `true` and any item numbers in the `Tier 1 Guaranteed Items` section of the settings in this section. Each Tier has its own setting for "Should Use" and "Guaranteed Items"

To add multiple items for each tier you would enter each number of the item in the appropriate section, separated by just a comma. For example, if you want `1: Soldier's Syringe` and `2: Paul's Goat Hoof` as your items you would enter `1,2` as your values for the `Tier 1` items. These numbers for these items are just examples and not the real values. (The "real" values can also change with mods that install more items as well) 

## Current plans for future updates
* Update the Tag settings to be able to set the percentage of tags at a Tier level. So if you wanted only 1 `Tier 3` item, and wanted to guaratee that it was always a `Damage` item, this would allow you to do that

The above updates aren't exactly small, but I will try to work on them when I have the free time. I actually really want these added myself.

## General Notes / Known Issues
* Damage/Healing/Utility chests might not give any item when opened. The chances of receiving nothing all depends on the settings specified in the config.
* Items available for monsters to have, either through `Void Fields` or the `Artifact of Evolution` completely ignore this mod entirely, and are pulled from all items in the game.

## Other info

If you find any bugs, incompatibilities with other mods, or just general suggestions please feel free to add them to the [Issues section](https://github.com/ZeusesNeckMeat/RiskOfRain2-ItemRoulette/issues) in Github!
	
## Changelog

**3.0.1**
*Updated to be compatible with MysticsItems

**3.0.0**
* Updated to be compatible with SotV
* Added new config option to sync Void items with the non-Void items generated through this mod

**2.3.0**
* Refactored the custom hooks to split some into separate files that made more sense
* Added a new CustomHookTracker class to track the state of the game to be used by each custom hooks
* Added SectionKey as a virtual property on ConfigBase to more easily expand upon adding new configs with keys
* Added a new set of configs to be able to control whether or not you want to use only the guaranteed items at a specific tier level, so each tier can be controlled separately

**2.2.0**
* Updated Guaranteed Items config to allow multiple items through a comma delimited list

**2.1.1**
* Sorted the Guaranteed Items section alphabetically to make it easier to find the items in the list
* Refactored ConfigSettings into multiple files, each section getting its own file

**2.1.0**
* Added config settings for Guaranteed Items
* Refactored the main file to break out the code into some, somewhat, more meaningful groups

**2.0.1**
* Fixed a bug where the minimum number of items per ItemTag was not correctly being taken into consideration when Boss and/or Lunar item pool limiters were set
* Updated Readme to add new info for the `How the item pool limitation works` and `Cusomizing items by tag` sections

**2.0.0**
* Updated the mod to be compatible with the Aniversary Updated
* Added the ability to customize when the item drop pool refreshes
* Removed Custom commands as these felt unnecessary

**1.0.0**
* Added the ability to customize the number of Healing, Utility, and Damage items

**0.7.0**
* Fixed incompatibility issue where chests would not drop items when using the following other mods: Item Pack, Bigger Bazaar, ShareSuite.

**0.6.0**
* Added custom commands to allow config values to be updated without closing the game

**0.5.0**
* Beta release