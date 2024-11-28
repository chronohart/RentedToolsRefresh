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
        private NPC BlacksmithNPC;

        private Tool? ToolBeingUpgraded;

        private Dictionary<Tuple<List<Item>, int>, Item> rentedToolRefs;
        private ITranslationHelper i18n;
        private List<Vector2> blacksmithCounterTiles = new List<Vector2>();

        public override void Entry(IModHelper helper)
        {
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            config = Helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.SaveLoaded += Bootstrap;
            Helper.Events.Display.MenuChanged += OnMenuChanged;

            i18n = Helper.Translation;
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
            this.BlacksmithNPC = null;

            this.rentedToolRefs = new Dictionary<Tuple<List<Item>, int>, Item>();
            this.blacksmithCounterTiles = new List<Vector2>();

            
            this.Player = Game1.player;
            this.blacksmithCounterTiles.Add(new Vector2(3f, 15f));
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (npc.Name == "Clint")
                {
                    this.BlacksmithNPC = npc;
                    break;
                }
            }

            if (this.BlacksmithNPC == null)
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
            if (config.modEnabled && inited && IsPlayerAtCounter(Player))
            {
                if (ShouldOfferRental(e))
                {
                    SetupRentToolsOfferDialog(Player);
                }
                //*********************************************
                // Upon receiving upgraded tool:
                //  e.NewMenu == DialogueBox with .dialogues containing:
                //      You received {a/an} {upgrade level} {tool}!
                //    closing sends this dialogue to e.OldMenu
                //  e.NewMenu == DialogueBox with .dialogues containing (awaiting answer):
                //      Can you hand me over that rental tool I gave you?
                //    choosing an answer sends this dialogue to e.OldMenu
                //*********************************************
                // if (Player.toolBeingUpgraded.Value == null && HasRentedTools(Player))
                // {
                //     SetupRentToolsRemovalDialog(Player);
                // }                
                else if (ShouldReturnRental(e))
                {
                    SetupRentToolsRemovalDialog(Player);
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
                            if(HasRentedTools(Player) == false)
                                result = true;
                        }
                    }
                }
            }

            return result;
        }

        private bool ShouldReturnRental(MenuChangedEventArgs e)
        {
            bool result = false;

            if(Game1.activeClickableMenu == null)
            {
                if(Player.toolBeingUpgraded.Value == null)
                {
                    // take rentals immediately after receiving upgraded tool back
                    if(e.OldMenu != null)
                    {
                        Monitor.Log(e.OldMenu.GetType().ToString());
                        // Monitor.Log("***");
                        // Monitor.Log(" dialogueBox.dialogues ");
                        // Monitor.Log("***");
                        // foreach(string line in dialogueBox.dialogues)
                        //     Monitor.Log(line);
                        // Monitor.Log("***");
                        // Monitor.Log(" dialogueBox.characterDialogue.dialogues ");
                        // Monitor.Log("***");
                        // if(dialogueBox.characterDialogue != null)
                        //     foreach(DialogueLine line in dialogueBox.characterDialogue.dialogues)
                        //         Monitor.Log(line.Text);
                        // Monitor.Log("END");
                    }

                    // take rentals before starting any other dialogue


                    Monitor.Log("*** Player.toolBeingUpgraded.Value == null");
                    if (e.NewMenu == null)
                    {
                        Monitor.Log("**** e.NewMenu == null");
                        if(e.OldMenu != null)
                        {
                            Monitor.Log("***** e.OldMenu != null");
                            if (e.OldMenu is DialogueBox dialogueBox1)
                            {
                                Monitor.Log("****** e.OldMenu is DialogueBox");
                                if((dialogueBox1.dialogues != null && dialogueBox1.dialogues.Count > 0 ))
                                {
                                    Monitor.Log("******* dialogueBox.dialogues.Any()");
                                    if((dialogueBox1.dialogues.Contains(i18n.Get("Blacksmith_RecycleTools_Menu")) == false))
                                    {
                                        Monitor.Log("******* dialogueBox.dialogues contains");
                                        result = HasRentedTools(Player);
                                    }
                                }
                                else if((dialogueBox1.characterDialogue != null))
                                {
                                    Monitor.Log("******* dialogueBox.characterDialogue != null");
                                    if((dialogueBox1.characterDialogue.dialogues != null && dialogueBox1.characterDialogue.dialogues.Count > 0))
                                    {
                                        Monitor.Log("******* dialogueBox.characterDialogue.dialogues.Any()");
                                        result = HasRentedTools(Player);
                                    }
                                }
                                else
                                    Monitor.Log("******* e.OldMenu failed to match the dialogue conditions");
                            }
                            else
                                Monitor.Log("****** e.OldMenu is NOT DialogueBox");
                        }
                        else
                            Monitor.Log("***** e.OldMenu == null");
                    }
                    else
                        Monitor.Log("**** e.NewMenu != null");
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
                Monitor.Log("*** HasRentedTools: Tool is being upgraded");
                result = tools.Exists(item => item.GetType().IsInstanceOfType(this.GetToolBeingUpgraded(who)));
            }
            else
            {
                Monitor.Log("*** HasRentedTools: Tool is not being upgraded");
                foreach (Tool tool in tools)
                {
                    Monitor.Log($"**** HasRentedTools: checking {tool.DisplayName}");
                    if (tools.Exists(item => item.Equals(tool) == false && item.GetType().IsInstanceOfType(tool) && item.UpgradeLevel <= tool.UpgradeLevel))
                    {
                        Monitor.Log("***** HasRentedTools: found a match");
                        result = true;
                        break;
                    }
                }
            }

            return result;
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
                            RecycleTempTools(whoInCallback);
                            break;
                    }
                    return;
                },
                BlacksmithNPC
            );
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
                                BuyTempTool(whoInCallback);
                                break;
                            case "Leave":
                                break;
                        }
                        return;
                    },
                BlacksmithNPC
            );
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
            Item toolToBuy = GetRentedToolByTool(GetToolBeingUpgraded(who));
            if (toolToBuy == null)
            {
                return;
            }
            //Sets rental tool quality to the quality of the current tool
            else if (toolToBuy is Tool actual)
            {
                actual.UpgradeLevel = GetToolBeingUpgraded(who).UpgradeLevel - 1;
            }

            int toolCost = GetToolCost(toolToBuy);

            if(who.Money < toolCost)
            {
                SetupFailedToRentDialog(Player);
            }
            else if(who.freeSpotsInInventory() <= 0)
            {
                SetupFailedToRentDialog(Player);
            }
            else
            {   
                ShopMenu.chargePlayer(who, 0, toolCost);
                Item item = who.addItemToInventory(toolToBuy);
                SetupSucceededToRentDialog(Player);
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

            List<Tool> toolsToCheck = tools.ToList();
            List<Tool> toolsToRemove = new List<Tool>();
            foreach (Tool tool in tools)
            {
                toolsToCheck.Remove(tool);
                toolsToRemove = toolsToCheck.Where(w => w.Equals(tool) == false && w.GetType().IsInstanceOfType(tool) && w.UpgradeLevel <= tool.UpgradeLevel).ToList();
                if(toolsToRemove.Count > 0)
                    toolsToCheck.RemoveAll(ra => toolsToRemove.Contains(ra));
            }

            foreach (Tool tool in toolsToRemove)
            {
                Monitor.Log("*********** Processing RecycleTempTools");
                who.removeItemFromInventory(tool);
            }
        }

        private int GetToolCost(Item tool)
        {
            return config.toolRentalFee;
        }
    }
}