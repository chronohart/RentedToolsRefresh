![Rented Tools Refresh](images/title.png "Rented Tools Refresh")

A mod for Stardew Valley, allowing players to rent a replacement tool when getting their own upgraded from Clint.

## Contents
* [Installation](#installation)
* [Configuration](#configuration)
* [Planned Features](#planned-features)
* [Translations](#translations)
* [Incompatibilities](#incompatibilities)
* [Credits](#credits)

<a name="installation"></a>![Installation](images/installation.png "Installation")

1. **Install the latest version of [SMAPI](https://smapi.io/).**
2. **Download Rented Tools Refresh** from [the Releases page on GitHub](https://github.com/chronohart/RentedToolsRefresh/releases) or [the mod page on NexusMods](https://www.nexusmods.com/stardewvalley/mods/29611/).
3. **Unzip RentedToolsRefresh.zip** into the `Stardew Valley\Mods` folder.

<a name="configuration"></a>![Configuration](images/configuration.png "Configuration")

Rented Tools Refresh includes options to enable or disable the functioning of the mod and change the cost to rent.

To edit these options:
1. **Run the game** using SMAPI. This will generate the mod's **config.json** file in the `Stardew Valley\Mods\RentedToolsRefresh` folder.
2. **Exit the game** and open the **config.json** file with any text editing program.

This mod also supports [spacechase0](https://github.com/spacechase0)'s [Generic Mod Config Menu](https://spacechase0.com/mods/stardew-valley/generic-mod-config-menu/) (GMCM). Players with that mod will be able to change config.json settings from within Stardew Valley.

The available settings are:

Name | Valid settings | Description
-----|----------------|------------
ModEnabled | **true**, false | Disable this to stop the mod from functioning.
AllowRentBasicLevelTool | true, **false** | Enable this to make Clint offer basic level tools for rent.
AllowRentCurrentLevelTool | **true**, false | Disable this to stop Clint from offering current level tools for rent.
RentalFee | numeric, **Default: 0** | Change this to change the cost to rent a tool.*
ApplyFeeToBasicLevel | **true**, false | Disable this to make **RentalFee** not apply to basic level tool rental.

*Note: If both "AllowRent" options are true and the player's current tool being upgraded is the basic level of the tool, the rental offered will be considered basic when determining the cost to rent.

<a name="planned-features"></a>![Planned Features](images/planned-features.png "Planned Features")

- Add option for daily cost for rentals

<a name="translations"></a>![Translations](images/translations.png "Translations")

This mod includes translations for all in-game dialog as well as GMCM settings and descriptions, into any language supported by the base game. That being said, I only speak English, so translations provided by myself may be poor translations from DeepL or similar. If you would like to submit a translation from an actual person, please don't hesitate to do so via GitHub pull request, linking a translation file in a GitHub issue, or sending me a file directly.

See the Stardew Valley Wiki's [Modding:Translations](https://stardewvalleywiki.com/Modding:Translations) page for more information about how these translations work.

Translation | Status
------------|------------------
default     | [fully translated](/i18n/default.json)
Chinese     | [partially translated](/i18n/zh.json)
French      | [translated with DeepL](/i18n/fr.json)
German      | [translated with DeepL](/i18n/de.json)
Hungarian   | [translated with DeepL](/i18n/hu.json)
Italian     | [partially translated with DeepL](/i18n/it.json)
Japanese    | [partially translated with DeepL](/i18n/ja.json)
Korean      | [partially translated with DeepL](/i18n/ko.json)
Portuguese  | [partially translated with DeepL](/i18n/pt.json)
Russian     | [partially translated with DeepL](/i18n/ru.json)
Spanish     | [translated with DeepL](/i18n/es.json)
Turkish     | [partially translated with DeepL](/i18n/tr.json)

<a name="incompatibilities"></a>![Incompatibilities](images/incompatibilities.png "Incompatibilities")

This mod is incompatible with any mods that prevent the following standard dialogs from activating, while standing in front of Clint's shop counter:
- "Thanks. I'll get started..." upon purchasing a tool upgrade
- "Um, I'm still working..." upon selecting "Upgrade Tool" while a tool upgrade is already in process
- "You received..." dialog when the player receives their upgraded tool

<a name="contributors"></a>![Contributors](images/contributors.png "Contributors")

[@txyyh](https://github.com/txyyh) contributed some of the Simplified Chinese translation

<a name="credits"></a>![Credits](images/credits.png "Credits")

This mod is based on the excellent work of SolusCleansing and their RentedToolsImproved mod.
Mod link: https://www.nexusmods.com/stardewvalley/mods/18909
Source link: https://github.com/SolusCleansing/RentedToolsImproved

RentedToolsImproved by SolusCleansing is based on the work of JarvieK and their Rented Tools mod.
Mod link: https://www.nexusmods.com/stardewvalley/mods/1307
Source link: https://github.com/Jarvie8176/StardewMods
