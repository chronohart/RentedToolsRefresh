using System;
using Microsoft.Xna.Framework;
using RentedToolsImproved;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Shops;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;

namespace RentedToolsRefresh
{
   
    public class ModEntry : Mod
    {

        private ModConfig config;
        
        private bool inited;
        private Farmer Player;
        private NPC blacksmithNPC;
        private bool shouldCreateFailedToRentTools;
        private bool shouldCreateSucceededToRentTools;
        private bool rentedToolsOffered;
        private bool recycleOffered;
        private bool SkipOfferToolsOnce;

        private Tool? ToolBeingUpgraded;

        private Dictionary<Tuple<List<Item>, int>, Item> rentedToolRefs;
        private ITranslationHelper i18n;
        private List<Vector2> blacksmithCounterTiles = new List<Vector2>();

        public override void Entry(IModHelper helper)
        {
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            config = Helper.ReadConfig<ModConfig>();

            if(config.modEnabled)
            {
                helper.Events.GameLoop.SaveLoaded += Bootstrap;
                Helper.Events.Display.MenuChanged += OnMenuChanged;

                i18n = Helper.Translation;
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<GenericModConfigMenu.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.config)
            );

            // add some config options
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Mod enabled",
                tooltip: () => "Enable or disable the functioning of Rented Tools Refresh.",
                getValue: () => this.config.modEnabled,
                setValue: value => this.config.modEnabled = value
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Tool rental cost",
                tooltip: () => "Flat cost to rent a tool.",
                getValue: () => this.config.toolRentalFee,
                setValue: value => this.config.toolRentalFee = value,
                min: 0
            );
        }

        private void Bootstrap(object sender, EventArgs e)
        {
            
            this.inited = false;
            this.Player = null;
            this.blacksmithNPC = null;

            this.shouldCreateFailedToRentTools = false;
            this.shouldCreateSucceededToRentTools = false;
            this.rentedToolsOffered = false;
            this.recycleOffered = false;
            this.SkipOfferToolsOnce = false;

            this.rentedToolRefs = new Dictionary<Tuple<List<Item>, int>, Item>();
            this.blacksmithCounterTiles = new List<Vector2>();

            
            this.Player = Game1.player;
            this.blacksmithCounterTiles.Add(new Vector2(3f, 15f));
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (npc.Name == "Clint")
                {
                    this.blacksmithNPC = npc;
                    break;
                }
            }

            if (this.blacksmithNPC == null)
            {
                Monitor.Log("blacksmith NPC not found", LogLevel.Info);
            }

            if(Player.toolBeingUpgraded != null)
            {
                ToolBeingUpgraded = Player.toolBeingUpgraded.Value;
            }
            
            this.inited = true;
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (this.inited && this.IsPlayerAtCounter(this.Player))
            {
                //*********************************************
                // Upon receiving upgraded tool:
                //  e.NewMenu == DialogueBox with .dialogues containing:
                //      You received {a/an} {upgrade level} {tool}!
                //    closing sends this dialogue to e.OldMenu
                //  e.NewMenu == DialogueBox with .dialogues containing (awaiting answer):
                //      Can you hand me over that rental tool I gave you?
                //    choosing an answer sends this dialogue to e.OldMenu
                // Cancel out of Upgrade Tool:
                //  e.OldMenu == ShopMenu.ShopId == "ClintUpgrade"
                // Upon purchasing Upgrade Tool:
                //  e.OldMenu == ShopMenu.ShopId == "ClintUpgrade"
                //  e.NewMenu == DialogueBox with .CharacterDialogue.dialogues containing:
                //      Thanks. I'll get started on this as soon as I can. It should be ready in a couple days.
                //    closing sends this dialogue to e.OldMenu
                //  e.NewMenu == DialogueBox with .dialogues containing (awaiting answer):
                //      You can take my old {tool} while I'm upgrading your {new upgrade level} {tool}.
                //    answering sends this dialogue to e.OldMenu
                // Selecting Upgrade Tools while upgrade in process:
                //  e.OldMenu == DialogueBox with .dialogues empty
                //  e.NewMenu == DialogueBox with .CharacterDialogue.dialogues containing:
                //      Um, I'm still working on your {upgrade level} {tool}. It won't be ready today.
                //    closing sends this dialogue to e.OldMenu
                // Trying to take a rental without enough money:
                //  ************
                //  currently broken. simply makes rental offer again with no message about cost
                //*********************************************
                if (shouldCreateFailedToRentTools)
                {
                    SetupFailedToRentDialog(Player);
                    shouldCreateFailedToRentTools = false;
                }
                else if (shouldCreateSucceededToRentTools)
                {
                    SetupSucceededToRentDialog(Player);
                    shouldCreateSucceededToRentTools = false;
                }
                else if (rentedToolsOffered)
                {
                    rentedToolsOffered = false;
                }
                else if (recycleOffered)
                {
                    recycleOffered = false;
                }
                else if (Player.toolBeingUpgraded.Value == null && HasRentedTools(Player))
                {
                    SetupRentToolsRemovalDialog(Player);
                }
                else if (ShouldOfferTools(Player))
                {
                    if (SkipOfferToolsOnce)
                    {
                        SkipOfferToolsOnce = false;
                    }
                    else
                    {
                        SetupRentToolsOfferDialog(Player);
                    }
                }

                
                else if (ShouldOfferRental(e))
                {
                    SetupRentToolsOfferDialog(Player);
                }
            }
        }

        private bool RentalFailed(MenuChangedEventArgs e)
        {
            bool result = false;

            return result;
        }

        private bool RentalSucceeded(MenuChangedEventArgs e)
        {
            bool result = false;

            return result;
        }

        private bool ShouldOfferRental(MenuChangedEventArgs e)
        {
            bool result = false;

            if(e.OldMenu != null && e.OldMenu is DialogueBox dialogueBox)
            {
                foreach(DialogueLine dialogueLine in dialogueBox.characterDialogue.dialogues)
                {
                    if(dialogueLine.Text == blacksmithNPC.TryGetDialogue())//"Thanks. I'll get started on this as soon as I can. It should be ready in a couple days.")
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        private bool ShouldReturnRental(MenuChangedEventArgs e)
        {
            bool result = false;

            if(e.NewMenu != null && e.NewMenu is DialogueBox dialogueBox)
            {
                if(ToolBeingUpgraded != null)
                {
                    string dialogueToMatch = "You receieved ";
                    if(ToolBeingUpgraded.UpgradeLevel == 2 || ToolBeingUpgraded.UpgradeLevel == 4)
                    {
                        dialogueToMatch += "an " + ToolBeingUpgraded.DisplayName + "!";
                    }
                    else
                    {
                        dialogueToMatch += "a " + ToolBeingUpgraded.DisplayName + "!";
                    }
                    foreach(string dialogue in dialogueBox.dialogues)
                    {
                        if(dialogue == dialogueToMatch)
                        {
                            ToolBeingUpgraded = null;
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        private Tool GetToolBeingUpgraded(Farmer who)
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

        private bool IsPlayerAtCounter(Farmer who)
        {
            return who.currentLocation.Name == "Blacksmith" && this.blacksmithCounterTiles.Contains(who.Tile);
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
                result = tools.Exists(item => item.GetType().IsInstanceOfType(this.GetToolBeingUpgraded(who)));
            }
            else
            {
                foreach (Tool tool in tools)
                {
                    if (tools.Exists(item => item.GetType().IsInstanceOfType(tool) && item.UpgradeLevel < tool.UpgradeLevel))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        private bool ShouldOfferTools(Farmer who)
        {
            if (GetToolBeingUpgraded(who) != null )
            {
                return (GetToolBeingUpgraded(who) != null && !this.HasRentedTools(who));
            }
            return false;
        }

        private void SetupRentToolsRemovalDialog(Farmer who)
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
                            this.RecycleTempTools(whoInCallback);
                            break;
                    }
                    return;
                },
                this.blacksmithNPC
            );
            this.recycleOffered = true;
        }

        private void SetupRentToolsOfferDialog(Farmer who)
        {
            who.currentLocation.createQuestionDialogue(
                i18n.Get("Blacksmith_OfferTools_Menu",
                new
                {
                    oldToolName = GetRentedToolByTool(GetToolBeingUpgraded(who))?.DisplayName,
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
                            this.BuyTempTool(whoInCallback);
                            break;
                        case "Leave":
                            // set to skip making this offer once in order to prevent this menu from popping off from this very menu closing
                            this.SkipOfferToolsOnce = true;
                            break;
                    }
                    return;
                },
                this.blacksmithNPC
            );
            rentedToolsOffered = true;
        }

        private void SetupSucceededToRentDialog(Farmer who)
        {
            i18n.Get("Blacksmith_HowToReturn");
        }

        private void SetupFailedToRentDialog(Farmer who)
        {
            if (who.freeSpotsInInventory() <= 0)
            {
                Game1.drawObjectDialogue(i18n.Get("Blacksmith_NoInventorySpace"));
            }
            else
            {
                Game1.drawObjectDialogue(i18n.Get("Blacksmith_InsufficientFundsToRentTool"));
            }
        }

        private Tool GetRentedToolByTool(Item tool)
        {
           
            if (tool is Axe)
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

        private void BuyTempTool(Farmer who)
        {
            //Get tool that is gonna be upgraded
            Item toolToBuy = this.GetRentedToolByTool(GetToolBeingUpgraded(who));
            if (toolToBuy == null)
            {
                return;
            }
            //Sets rental tool quality to the quality of the current tool
            else if (toolToBuy is Tool actual)
            {
                actual.UpgradeLevel = GetToolBeingUpgraded(who).UpgradeLevel - 1;
            }


            int toolCost = this.GetToolCost(toolToBuy);
            
            if (who.Money >= toolCost && who.freeSpotsInInventory() > 0)
            {
                
                ShopMenu.chargePlayer(who, 0, toolCost);
                Item item = who.addItemToInventory(toolToBuy);
                this.shouldCreateSucceededToRentTools = true;
            }
            else
            {
                this.shouldCreateFailedToRentTools = true;
            }

        }

        private void RecycleTempTools(Farmer who)
        {
            // recycle all rented tools
            IList<Item> inventory = who.Items;
            List<Tool> tools = inventory
                .Where(tool => tool is Axe || tool is Pickaxe || tool is WateringCan || tool is Hoe)
                .OfType<Tool>()
                .ToList();

            foreach (Tool tool in tools)
            {
                if (tools.Exists(item => tool.GetType().IsInstanceOfType(item) && tool.UpgradeLevel < item.UpgradeLevel))
                {
                    who.removeItemFromInventory(tool);
                }
            }

            return;
        }

        private int GetToolCost(Item tool)
        {
            return config.toolRentalFee;
        }
    }
}