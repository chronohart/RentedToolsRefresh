using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
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
        private RentalTracking? RentalTracker;

        public override void Entry(IModHelper helper)
        {
            i18n = Helper.Translation;

            Config = Helper.ReadConfig<ModConfig>();
            Config.ValidateConfigFile();
            helper.WriteConfig(Config);

            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            helper.Events.GameLoop.SaveLoaded += InitOnSaveLoaded;
            Helper.Events.GameLoop.DayStarted += OnStartNewDay;
            Helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry
                .GetApi<GenericModConfigMenu.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod with GMCM
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            // add "Mod Enabled" option
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => i18n.Get("config.modEnabled.name"),
                tooltip: () => i18n.Get("config.modEnabled.tooltip"),
                getValue: () => Config.ModEnabled,
                setValue: value => Config.modEnabled = value
            );

            //**************************
            //*  Rental Options section
            //**************************
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => i18n.Get("config.rentalSection.text"),
                tooltip: () => i18n.Get("config.rentalSection.tooltip")
            );
            // options
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => i18n.Get("config.allowRentBasicLevelTool.name"),
                getValue: () => Config.AllowRentBasicLevelTool,
                setValue: value => Config.AllowRentBasicLevelTool = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => i18n.Get("config.allowRentCurrentLevelTool.name"),
                tooltip: () => i18n.Get("config.allowRentCurrentLevelTool.tooltip"),
                getValue: () => Config.AllowRentCurrentLevelTool,
                setValue: value => Config.AllowRentCurrentLevelTool = value
            );

            //******************************
            //*  Rental Fee Options section
            //******************************
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => i18n.Get("config.rentalFeeOptionsSection.text")
            );
            // options

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => i18n.Get("config.rentalCost.name"),
                tooltip: () => i18n.Get("config.rentalCost.tooltip"),
                getValue: () => Config.RentalFee,
                setValue: value => Config.RentalFee = value,
                min: 0
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => i18n.Get("config.applyFeeToBasicLevel.name"),
                tooltip: () => i18n.Get("config.applyFeeToBasicLevel.tooltip"),
                getValue: () => Config.ApplyFeeToBasicLevel,
                setValue: value => Config.ApplyFeeToBasicLevel = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => i18n.Get("config.dailyFee.name"),
                tooltip: () => i18n.Get("config.dailyFee.tooltip"),
                getValue: () => Config.DailyFee,
                setValue: value => Config.DailyFee = value,
                min: 0
            );
        }

        private void InitOnSaveLoaded(object? sender, EventArgs e)
        {
            
            IsInited = false;
            BlacksmithNPC = null;

            BlacksmithCounterTiles = new List<Vector2>();

            Player = Game1.player;
            RentalTracker = Helper.Data.ReadSaveData<RentalTracking>("RentalTracking");
            RentalTracker ??= new RentalTracking();
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

        private void OnStartNewDay(object? sender, DayStartedEventArgs e)
        {
            RentalTracking? tracking = Helper.Data.ReadSaveData<RentalTracking>("RentalTracking");
            if(tracking != null)
                ChargeDailyFee();
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (Config.ModEnabled && IsInited && AtBlacksmithCounter(Player))
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

        private void ChargeDailyFee()
        {
            int dailyFee = Config.DailyFee;

            if(Player.Money < dailyFee)
            {
                Game1.drawObjectDialogue(i18n.Get("notify.insufficientFunds"));
            }
            else
            {
                RentalTracker ??= new RentalTracking();
                    
                RentalTracker.AccruedDebt += dailyFee;
                //ShopMenu.chargePlayer(Player, 0, dailyFee);
            }
        }

        private bool ShouldOfferRental(MenuChangedEventArgs e)
        {
            bool result = false;

            if(HasRentedTools(Player) == false)
            {
                if(Game1.activeClickableMenu == null)
                {
                    if(GetToolBeingUpgraded(Player) != null)
                    {
                        if(e.OldMenu != null && e.OldMenu is DialogueBox dialogueBox)
                        {
                            if(dialogueBox.characterDialogue != null)
                            {
                                // first, ensure the offer is only ever made after one of two very specific lines of dialogue
                                if(dialogueBox.characterDialogue.TranslationKey == @"Strings\StringsFromCSFiles:Tool.cs.14317"
                                    || dialogueBox.characterDialogue.TranslationKey == @"Data\ExtraDialogue:Clint_StillWorking")
                                {
                                    result = true;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private bool ShouldReturnRental(MenuChangedEventArgs e)
        {
            bool result = false;

            if(HasRentedTools(Player))
            {
                if(Game1.activeClickableMenu == null
                    && Player.toolBeingUpgraded.Value == null
                    && e.NewMenu == null)
                {
                    if(e.OldMenu != null
                        && ((e.OldMenu is DialogueBox dialogueBox 
                                && dialogueBox.dialogues.FirstOrDefault() != i18n.Get("blacksmith.rentalReturn"))
                            || (e.OldMenu is ShopMenu shopMenu && shopMenu.ShopId == "Blacksmith")))
                    {
                        result = CarryingRentedTools(Player);
                    }
                }
            }

            return result;
        }

        private Tool? GetToolBeingUpgraded(Farmer who)
        {
            if (who.toolBeingUpgraded.Value != null)
            {
                if (who.toolBeingUpgraded.Value is Axe || who.toolBeingUpgraded.Value is Pickaxe || who.toolBeingUpgraded.Value is Hoe 
                    || who.toolBeingUpgraded.Value is WateringCan || who.toolBeingUpgraded.Value is Pan)
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

            if(RentalTracker != null)
                result = RentalTracker.HasRentedTool;

            return result;
        }

        private bool CarryingRentedTools(Farmer who)
        {
            
            bool result = false;

            IList<Item> inventory = who.Items;
            List<Tool> tools = inventory
                .Where(tool => tool is Axe || tool is Pickaxe || tool is WateringCan || tool is Hoe || tool is Pan)
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
                    if (tools.Exists(item => item.Equals(tool) == false && item.GetType().IsInstanceOfType(tool) 
                            && item.UpgradeLevel <= tool.UpgradeLevel))
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
                i18n.Get("blacksmith.rentalReturn"),
                new Response[1]
                {
                    new Response("ACCEPT", i18n.Get("player.rentalReturn.accept")),
                },
                (Farmer whoInCallback, string whichAnswer) =>
                {
                    switch (whichAnswer)
                    {
                        case "ACCEPT":
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
            Tool? toolBeingUpgradedTo = GetToolBeingUpgraded(who);
            if(toolBeingUpgradedTo == null)
                return;

            Tool? basicTool = GetFreshTool(toolBeingUpgradedTo);
            if(basicTool == null)
                return;

            string? previousToolId = toolBeingUpgradedTo.GetToolData()?.ConventionalUpgradeFrom;
            if(previousToolId == null)
                return;
            Tool currentTool = (Tool)ItemRegistry.Create(previousToolId);
            if(currentTool == null)
                return;

            Monitor.Log($"*** currentTool == {currentTool.DisplayName}");

            int basicCost = GetToolRentalCost("BASIC");
            int currentCost = GetToolRentalCost("CURRENT");

            bool offerBasic = Config.AllowRentBasicLevelTool;
            bool offerCurrent = Config.AllowRentCurrentLevelTool;

            if(currentTool.UpgradeLevel == basicTool.UpgradeLevel && offerBasic && offerCurrent)
                offerCurrent = false;

            // setup blacksmith dialog based on current config
            string blacksmithDialog = i18n.Get("blacksmith.rentalOffer.base", 
                new { toolNameWithArticle = Lexicon.prependArticle(basicTool.DisplayName)});

            if((offerBasic == false || basicCost == 0)
                && (offerCurrent == false || currentCost == 0))
                blacksmithDialog += i18n.Get("blacksmith.rentalOffer.noFee");
            else if(offerBasic != offerCurrent
                || basicCost == currentCost)
                blacksmithDialog += i18n.Get("blacksmith.rentalOffer.sameFeeAllOptions", new { currency = currentCost});
            else
                blacksmithDialog += i18n.Get("blacksmith.rentalOffer.feeForCurrentLevelOnly",
                new
                {
                    basicToolName = basicTool.DisplayName,
                    currentToolName = currentTool.DisplayName,
                    currency = currentCost
                });

            List<Response> responses = new List<Response>();
            if(offerBasic)
            {
                Response responseToAdd = new Response("BASIC", i18n.Get("player.rentalOffer.basic"));
                responses.Add(responseToAdd);
            }
            if(offerCurrent)
            {
                Response responseToAdd = new Response("CURRENT", i18n.Get("player.rentalOffer.current"));
                responses.Add(responseToAdd);
            }
            if(responses.Count == 1)
                responses[0].responseText = i18n.Get("player.rentalOffer.accept");
            responses.Add(new Response("REJECT", i18n.Get("player.rentalOffer.reject")));

            Tool? toolToRent = null;
            who.currentLocation.createQuestionDialogue(
                blacksmithDialog,
                responses.ToArray(),
                (Farmer whoInCallback, string answer) =>
                    {
                        switch (answer)
                        {
                            case "BASIC":
                                toolToRent = basicTool;
                                break;
                            case "CURRENT":
                                toolToRent = currentTool;
                                break;
                            case "REJECT":
                                return;
                        }
                        RentTool(whoInCallback, answer, toolToRent);
                        return;
                    },
                BlacksmithNPC
            );
        }

        private void DisplaySuccessDialog()
        {
            Game1.DrawDialogue(new Dialogue(BlacksmithNPC, "blacksmith.howToReturn", i18n.Get("blacksmith.howToReturn")));
        }

        private Tool? GetFreshTool(Item? tool, int? panUpgradeLevel = null)
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
            else if (tool is Pan)
            {
                if(panUpgradeLevel != null)
                    return new Pan(panUpgradeLevel.Value);
                else
                    return new Pan();
            }
            else
            {
                Monitor.Log($"unsupported upgradable tool: {tool?.ToString()}");
                return null;
            }
        }

        private void RentTool(Farmer who, string toolLevel, Tool? toolToRent)
        {
            if(toolToRent == null)
                return;

            int toolCost = GetToolRentalCost(toolLevel);

            if(who.Money < toolCost)
            {
                Game1.drawObjectDialogue(i18n.Get("notify.insufficientFunds"));
            }
            else if(who.freeSpotsInInventory() <= 0)
            {
                Game1.drawObjectDialogue(i18n.Get("notify.noInventorySpace"));
            }
            else
            {   
                ShopMenu.chargePlayer(who, 0, toolCost);
                Item item = who.addItemToInventory(toolToRent);
                DisplaySuccessDialog();

                RentalTracker ??= new RentalTracking();

                RentalTracker.HasRentedTool = true;
                Helper.Data.WriteSaveData("RentalTracking", RentalTracker);
            }
        }

        private void ReturnRentals(Farmer who)
        {
            // recycle all rented tools
            IList<Item> inventory = who.Items;
            List<Tool> tools = inventory
                .Where(tool => tool is Axe || tool is Pickaxe || tool is WateringCan || tool is Hoe || tool is Pan)
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

                RentalTracker ??= new RentalTracking();

                RentalTracker.HasRentedTool = false;

                if(Player.Money >= RentalTracker.AccruedDebt)
                {
                    ShopMenu.chargePlayer(Player, 0, RentalTracker.AccruedDebt);
                    RentalTracker.AccruedDebt = 0;
                }
                
                Helper.Data.WriteSaveData("RentalTracking", RentalTracker);
            }
        }

        private int GetToolRentalCost(string toolLevel)
        {
            int result = Config.RentalFee;
            
            if(Config.ApplyFeeToBasicLevel == false && toolLevel == "BASIC")
                result = 0;

            return result;
        }
    }
}