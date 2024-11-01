using Life;
using Life.BizSystem;
using Life.Network;
using Life.UI;
using Mirror;
using ModKit.Helper;
using ModKit.Interfaces;
using ModKit.Internal;
using ModKit.ORM;
using MODRP_BizBill.Classes;
using MODRP_BizBill.Functions;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using _menu = AAMenu.Menu;

namespace MODRP_BizBill.Main
{

    class Main : ModKit.ModKit
    {
        public BillPanel BillPanel = new BillPanel();
        
        public static string ConfigDirectoryPath;
        public static string ConfigBizBillPath;
        public static BizBillConfig _BizBillConfig;

        public Main(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Loicsmith");

            BillPanel.Context = this;
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");

            InitAAmenu();

            InitConfig();
            _BizBillConfig = LoadConfigFile(ConfigBizBillPath);

            Orm.RegisterTable<OrmManager.BizBill_LogsBill>();
        }


        private void InitConfig()
        {
            try
            {
                ConfigDirectoryPath = DirectoryPath + "/BizBill";
                ConfigBizBillPath = Path.Combine(ConfigDirectoryPath, "BizBillConfig.json");

                if (!Directory.Exists(ConfigDirectoryPath)) Directory.CreateDirectory(ConfigDirectoryPath);
                if (!File.Exists(ConfigBizBillPath)) InitBizBillConfig();
            }
            catch (IOException ex)
            {
                Logger.LogError("InitDirectory", ex.Message);
            }
        }

        private void InitBizBillConfig()
        {
            BizBillConfig BizBillConfig = new BizBillConfig();
            string json = JsonConvert.SerializeObject(BizBillConfig, Formatting.Indented);
            File.WriteAllText(ConfigBizBillPath, json);
        }

        private BizBillConfig LoadConfigFile(string path)
        {
            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);
                BizBillConfig BizBillConfig = JsonConvert.DeserializeObject<BizBillConfig>(jsonContent);

                return BizBillConfig;
            }
            else return null;
        }

        private void SaveConfig(string path)
        {
            string json = JsonConvert.SerializeObject(_BizBillConfig, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public void ConfigEditor(Player player)
        {
            Panel panel = PanelHelper.Create("BizBill | Config JSON", UIPanel.PanelType.TabPrice, player, () => ConfigEditor(player));

            panel.AddTabLine($"{TextFormattingHelper.Color("CityHallId : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_BizBillConfig.CityHallId}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "CityHallId");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("BankId : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_BizBillConfig.CityHallId}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "BankId");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("TaxPercentage : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_BizBillConfig.TaxPercentage}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "TaxPercentage");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("BankPercentage : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_BizBillConfig.TaxPercentage}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "BankPercentage");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("Appliquer la configuration", TextFormattingHelper.Colors.Success)}", _ =>
            {
                SaveConfig(ConfigBizBillPath);
                panel.Refresh();
            });

            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.AddButton("Retour", _ => AAMenu.AAMenu.menu.AdminPluginPanel(player));
            panel.CloseButton();
            panel.Display();
        }

        public void EditLineInConfig(Player player, string Param)
        {
            Panel panel = PanelHelper.Create("BizBill | Edit JSON", UIPanel.PanelType.Input, player, () => EditLineInConfig(player, Param));
            panel.TextLines.Add($"Modification de la valeur de : \"{Param}\"");
            panel.SetInputPlaceholder("Veuillez saisir une valeur");
            panel.AddButton("Valider", (ui) =>
            {
                string input = ui.inputText;

                switch (Param)
                {
                    case "CityHallId":
                        // int
                        if (int.TryParse(input, out int valueCity))
                        {
                            _BizBillConfig.CityHallId = valueCity;
                        }
                        else
                        {
                            player.Notify("BizBill", "Veuillez saisir un nombre entier.", NotificationManager.Type.Error);
                        }
                        break;
                    case "BankId":
                        // int
                        if (int.TryParse(input, out int valueBank))
                        {
                            _BizBillConfig.BankId = valueBank;
                        }
                        else
                        {
                            player.Notify("BizBill", "Veuillez saisir un nombre entier.", NotificationManager.Type.Error);
                        }
                        break;
                    case "TaxPercentage":
                        // float
                        if (float.TryParse(input, out float valueTax))
                        {
                            _BizBillConfig.TaxPercentage = valueTax;
                        }
                        else
                        {
                            player.Notify("BizBill", "Veuillez saisir un nombre valide.", NotificationManager.Type.Error);
                        }
                        break;
                    case "BankPercentage":
                        // float
                        if (float.TryParse(input, out float valueBankTax))
                        {
                            _BizBillConfig.BankPercentage = valueBankTax;
                        }
                        else
                        {
                            player.Notify("BizBill", "Veuillez saisir un nombre valide.", NotificationManager.Type.Error);
                        }
                        break;

                }
                panel.Previous();
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void InitAAmenu()
        {
            _menu.AddAdminPluginTabLine(PluginInformations, 0, "BizBill", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                ConfigEditor(player);
            });

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Bus }, null, "Faire une facture", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                BillPanel.MainPanel(player);
            });

           
        }
    }
}
