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

## Customizing items by tag

There is a section in the config file called `Item Tag Percents`. In this section, you can customize how many Healing, Utility, and Damage items drop in your runs. If you want half of your items to be Healing and the other half to be Damage then you would set `HealingTypePercentage` to 50, and `DamageTypePercentage` to 50. 

0 means you want 0 items of that type. The sum of the three fields, obviously, should not exceed 100.

If you really don't want to do math and you really only care about a specific tag, say `Damage`, then you can set `Damage` to your desired percent, and then set the other two to -1. When the config file regenerates it will default the values that were at `-1` to split the remaining percentage down the middle. 
For example -> `43, -1, -1` would become `43, 28.5, 28.5`

**Important things to note:**
* If the sum of the three fields ever does exceed 100, then the config will be updated to set all three values to 33%
* This section does not modify the `Boss` or `Lunar` tiers. This is only for Tiers 1-3.
* The values you have in your config for Tiers 1, 2, and 3 WILL take precendence over the Type percentages. For example if you have 8 items allowed between Tiers 1, 2, and 3 and you have `DamageTypePercentage` = 25, `UtilityTypePercentage` = 25, and `HealingTypePercentage` = 0, then 2/8 items will be Damage, 2/8 items will be Utility, and the other 4/8 items will be selected at random from all three types. 
* Each Tag (Damage, Healing, Utility) only has a set amount of items in those categories. Say the `Utility` tag only has 10 items, you have `UtilityTypePercentage` set to 50, and you have a combined total of 30 items allowed in your run between Tier 1, Tier 2, and Tier 3, then all 10 Utility items will be added to the pool, and the remaining 5 items that would have been Utility will be pulled randomly from the other two tags. 

## Changing when the item pool can refresh

There is a config option called `ItemPoolRefreshOptions` under the `General` section. This will allow you to customize when this mod refreshes the item drop pool. 

The three current available choices are:
* NewRun - Only refresh on the start of a brand new run
* EachStage - Refresh item pool at the start of each stage
* EachLoop - Refresh item pool at the start of each new loop

These options should provide a little more variety for longer runs, if wanted. EachStage is probably not needed as it's really not that different from vanilla, but I threw it in there just in case.

## Current plans for future updates
* Add sections to the config to be able to add items to a sort of "guaranteed" list. So if you want to play with random items every time, but you also want to make sure Paul's Goat Hoofs is always an option, then this will provide that functionality
	* I would probably also want to add something to be able to say whether or not the "guaranteed" items will be taken into consideration when counting the number of items per tier. So if you want 5 `Tier 1` items and also want to make sure you have Paul's Goat Hoofs, do you want Paul's Goat Hoof to be one of those 5 items and pick the other 4 at random, or still pick 5 at random, and just add Paul's Goat Hoofs to that list
* Update the Tag settings to be able to set the percentage of tags at a Tier level. So if you wanted only 1 `Tier 3` item, and wanted to guaratee that it was always a `Damage` item, this would allow you to do that

The above updates aren't exactly small, but I will try to work on them when I have the free time. I actually really want these added myself.

## Other info

If you find any bugs, incompatibilities with other mods, or just general suggestions please feel free to add them to the [Issues section](https://github.com/ZeusesNeckMeat/RiskOfRain2-ItemRoulette/issues) in Github!
	
## Changelog

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