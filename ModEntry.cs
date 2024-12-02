using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace RentedToolsRefresh
{
   
    public class ModEntry : Mod
    {

        private ModConfig Config;
        private bool IsInited;
        private Farmer Player;
        private NPC? BlacksmithNPC;
        private ITranslationHelper i18n;
        private List<Vector2> BlacksmithCounterTiles;

        public override void Entry(IModHelper helper)
        {
            i18n = Helper.Translation;

            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Config = Helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.SaveLoaded += InitOnSaveLoaded;
            Helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<GenericModConfigMenu.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            // add some config options
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Mod enabled",
                tooltip: () => "Enable or disable the functioning of Rented Tools Refresh.",
                getValue: () => Config.modEnabled,
                setValue: value => Config.modEnabled = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Tool rental cost",
                tooltip: () => "Flat cost to rent a tool.",
                getValue: () => Config.toolRentalFee,
                setValue: value => Config.toolRentalFee = value,
                min: 0
            );
        }

        private void InitOnSaveLoaded(object? sender, EventArgs e)
        {
            
            IsInited = false;
            BlacksmithNPC = null;

            BlacksmithCounterTiles = new List<Vector2>();

            Player = Game1.player;
            BlacksmithCounterTiles.Add(new Vector2(3f, 15f));
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (npc.Name == "Clint")
                {
                    BlacksmithNPC = npc;
                    break;
                }
            }

            if (BlacksmithNPC == null)
            {
                Monitor.Log("blacksmith NPC not found", LogLevel.Info);
            }
            
            IsInited = true;
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (Config.modEnabled && IsInited && AtBlacksmithCounter(Player))
            {
                if (ShouldOfferRental(e))
                {
                    DisplayRentalOfferDialog(Player);
                }
                else if (ShouldReturnRental(e))
                {
                    DisplayRentalRemovalDialog(Player);
                }
            }
        }

        private bool ShouldOfferRental(MenuChangedEventArgs e)
        {
            bool result = false;

            if(Game1.activeClickableMenu == null)
            {
                if(e.OldMenu != null && e.OldMenu is DialogueBox dialogueBox)
                {
                    if(dialogueBox.characterDialogue != null)
                    {
                        // first, ensure the offer is only ever made after one of two very specific lines of dialogue
                        if(dialogueBox.characterDialogue.TranslationKey == @"Strings\StringsFromCSFiles:Tool.cs.14317"
                            || dialogueBox.characterDialogue.TranslationKey == @"Data\ExtraDialogue:Clint_StillWorking")
                        {
                            // next, ensure player doesn't already have a rented tool
                            result = HasRentedTools(Player) == false;
                        }
                    }
                }
            }

            return result;
        }

        private bool ShouldReturnRental(MenuChangedEventArgs e)
        {
            bool result = false;

            if(Game1.activeClickableMenu == null
                && Player.toolBeingUpgraded.Value == null
                && e.NewMenu == null)
            {
                if(e.OldMenu != null
                    && ((e.OldMenu is DialogueBox dialogueBox && dialogueBox.dialogues.FirstOrDefault() != i18n.Get("Blacksmith_RecycleTools_Menu"))
                        || (e.OldMenu is ShopMenu shopMenu && shopMenu.ShopId == "Blacksmith")))
                {
                    result = HasRentedTools(Player);
                }
            }

            return result;
        }

        private Tool? GetToolBeingUpgraded(Farmer who)
        {
            if (who.toolBeingUpgraded.Value != null)
            {
                if (who.toolBeingUpgraded.Value is Axe || who.toolBeingUpgraded.Value is Pickaxe || who.toolBeingUpgraded.Value is Hoe || who.toolBeingUpgraded.Value is WateringCan)
                {
                    return who.toolBeingUpgraded.Value;
                }
            }
            return null;
        }

        private bool AtBlacksmithCounter(Farmer who)
        {
            return who.currentLocation.Name == "Blacksmith" && BlacksmithCounterTiles.Contains(who.Tile);
        }

        private bool HasRentedTools(Farmer who)
        {
            
            bool result = false;

            IList<Item> inventory = who.Items;
            List<Tool> tools = inventory
                .Where(tool => tool is Axe || tool is Pickaxe || tool is WateringCan || tool is Hoe)
                .OfType<Tool>()
                .ToList();

            if (GetToolBeingUpgraded(who) != null)
            {
                result = tools.Exists(item => item.GetType().IsInstanceOfType(GetToolBeingUpgraded(who)));
            }
            else
            {
                foreach (Tool tool in tools)
                {
                    if (tools.Exists(item => item.Equals(tool) == false && item.GetType().IsInstanceOfType(tool) && item.UpgradeLevel <= tool.UpgradeLevel))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        private void DisplayRentalRemovalDialog(Farmer who)
        {
            who.currentLocation.createQuestionDialogue(
                i18n.Get("Blacksmith_RecycleTools_Menu"),
                new Response[1]
                {
                    new Response("Confirm", i18n.Get("Blacksmith_RecycleToolsMenu_Confirm")),
                },
                (Farmer whoInCallback, string whichAnswer) =>
                {
                    switch (whichAnswer)
                    {
                        case "Confirm":
                            ReturnRentals(whoInCallback);
                            break;
                    }
                    return;
                },
                BlacksmithNPC
            );
        }

        private void DisplayRentalOfferDialog(Farmer who)
        {
            who.currentLocation.createQuestionDialogue(
                i18n.Get("Blacksmith_OfferTools_Menu",
                    new
                    {
                        oldToolName = GetFreshTool(GetToolBeingUpgraded(who))?.DisplayName,
                        newToolName = GetToolBeingUpgraded(who)?.DisplayName
                    }),
                new Response[2]
                    {
                        new Response("Confirm", i18n.Get("Blacksmith_OfferToolsMenu_Confirm")),
                        new Response("Leave", i18n.Get("Blacksmith_OfferToolsMenu_Leave")),
                    },
                (Farmer whoInCallback, string whichAnswer) =>
                    {
                        switch (whichAnswer)
                        {
                            case "Confirm":
                                RentTool(whoInCallback);
                                break;
                            case "Leave":
                                break;
                        }
                        return;
                    },
                BlacksmithNPC
            );
        }

        private void DisplaySuccessDialog(Farmer who)
        {
            i18n.Get("Blacksmith_HowToReturn");
        }

        private void DisplayFailureDialog(Farmer who)
        {
            if (who.freeSpotsInInventory() <= 0)
            {
                Game1.drawObjectDialogue(i18n.Get("Blacksmith_NoInventorySpace"));
            }
            else
            {
                Game1.drawObjectDialogue(i18n.Get("Blacksmith_InsufficientFundsToRentTool"));
                //Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
            }
        }

        private Tool? GetFreshTool(Item? tool)
        {
            if(tool == null)
            {
                return null;
            }
            else if (tool is Axe)
            {
                return new Axe();
            }
            else if (tool is Pickaxe)
            {
                return new Pickaxe();
            }
            else if (tool is WateringCan)
            {
                return new WateringCan();
            }
            else if (tool is Hoe)
            {
                return new Hoe();
            }
            else
            {
                Monitor.Log($"unsupported upgradable tool: {tool?.ToString()}");
                return null;
            }
        }

        private void RentTool(Farmer who)
        {
            Tool? toolBeingUpgraded = GetToolBeingUpgraded(who);
            Item? toolToBuy = GetFreshTool(toolBeingUpgraded);
            if (toolBeingUpgraded == null || toolToBuy == null)
            {
                return;
            }
            else if (toolToBuy is Tool actual)
            {
                //Sets rental tool quality to the quality of the current tool
                actual.UpgradeLevel = toolBeingUpgraded.UpgradeLevel - 1;
            }

            int toolCost = GetToolRentalCost(toolToBuy);

            if(who.Money < toolCost)
            {
                DisplayFailureDialog(Player);
            }
            else if(who.freeSpotsInInventory() <= 0)
            {
                DisplayFailureDialog(Player);
            }
            else
            {   
                ShopMenu.chargePlayer(who, 0, toolCost);
                Item item = who.addItemToInventory(toolToBuy);
                DisplaySuccessDialog(Player);
            }
        }

        private void ReturnRentals(Farmer who)
        {
            // recycle all rented tools
            IList<Item> inventory = who.Items;
            List<Tool> tools = inventory
                .Where(tool => tool is Axe || tool is Pickaxe || tool is WateringCan || tool is Hoe)
                .OfType<Tool>()
                .ToList();

            List<Tool> toolsToRemove = new List<Tool>();
            foreach (Tool tool in tools)
            {
                if(tools.Exists(e => e.GetType().IsInstanceOfType(tool) && e.UpgradeLevel > tool.UpgradeLevel))
                    toolsToRemove.Add(tool);
            }

            foreach (Tool tool in toolsToRemove)
            {
                who.removeItemFromInventory(tool);
            }
        }

        private int GetToolRentalCost(Item tool)
        {
            return Config.toolRentalFee;
        }
    }
}