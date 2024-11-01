using Life;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Utils;
using System;

namespace MODRP_BizBill.Functions
{
    internal class BillPanel
    {
        public ModKit.ModKit Context { get; set; }

        public void BizBillPanel(Player player)
        {
            if (player.GetClosestPlayer() != null)
            {
                Panel panel = Context.PanelHelper.Create("Facture", UIPanel.PanelType.Input, player, () => BizBillPanel(player));
                panel.TextLines.Add("Facture adressée à : " + player.GetClosestPlayer().GetFullName());
                panel.TextLines.Add("Veuillez saisir le montant de la facture.");
                panel.SetInputPlaceholder("Montant en €..");

                panel.AddButton("Valider", (ui) =>
                {
                    string StringPrice = ui.inputText;

                    if (float.TryParse(StringPrice, out float Price))
                    {
                        if (Price > 0)
                        {
                            player.Notify("Succès", $"La facture d'un montant de {Price}€ vient d'être envoyé à {player.GetClosestPlayer().GetFullName()}", NotificationManager.Type.Success);
                            BizBill_ReceiveBill(player, player.GetClosestPlayer(), Price);
                            player.ClosePanel(ui);
                        } else
                        {
                            player.Notify("Erreur", "Vous ne pouvez pas saisir une valeur négative !", NotificationManager.Type.Error);
                        }
                    }
                    else
                    {
                        player.Notify("Erreur", "Vous devez saisir un montant valide !", NotificationManager.Type.Error);
                    }
                });

                panel.AddButton("Retour", ui => AAMenu.AAMenu.menu.BizPanel(player));
                panel.CloseButton();
                panel.Display();
            }
            else
            {
                player.Notify("Erreur", "Vous ne pouvez pas faire une facture si il n'y a personne à proximiter !", NotificationManager.Type.Error);
            }
        }

        public void BizBill_ReceiveBill(Player player, Player SecondPlayer, float Price)
        {
            Panel panel = Context.PanelHelper.Create("Facture", UIPanel.PanelType.Text, SecondPlayer, () => BizBill_ReceiveBill(player, SecondPlayer, Price));
            panel.TextLines.Add("Facture adressée à : " + SecondPlayer.GetFullName());
            panel.TextLines.Add($"Montant de : {Price}€");

            panel.AddButton("Signer", (ui) =>
            {
                if (SecondPlayer.character.Bank >= Price)
                {
                    SecondPlayer.ClosePanel(ui);
                    BizPanel_AcceptBill(player, player.GetClosestPlayer(), Price);
                }
                else
                {
                    SecondPlayer.ClosePanel(ui);
                    player.Notify("Facture déchirée, fonds bancaire insufissant", $"{SecondPlayer.GetFullName()} vient de déchirer la facture d'un montant de {Price}€", NotificationManager.Type.Success);
                    SecondPlayer.Notify("Facture déchirée fonds bancaire insufissant", $"Vous venez de déchirer la facture d'un montant de {Price}€", NotificationManager.Type.Success);

                }
            });

            panel.AddButton("Déchirer", (ui) =>
            {
                SecondPlayer.ClosePanel(ui);
                BizPanel_RefuseBill(player, player.GetClosestPlayer(), Price);
            });


            panel.CloseButton();
            panel.Display();
        }

        public async void BizPanel_AcceptBill(Player player, Player SecondPlayer, float Price)
        {
            OrmManager.BizBill_LogsBill instance = new OrmManager.BizBill_LogsBill { CustomerName = SecondPlayer.GetFullName(), EmployeeName = player.GetFullName(), Date = 0, Price = Price };
            var result = await instance.Save();

            if (result)
            {
                player.Notify("Facture acceptée", $"{SecondPlayer.GetFullName()} vient de signer la facture d'un montant de {Price}€", NotificationManager.Type.Success);
                SecondPlayer.Notify("Facture acceptée", $"Vous venez de signer la facture d'un montant de {Price}€", NotificationManager.Type.Success);

                float taxPercentage = Main.Main._BizBillConfig.TaxPercentage;
                float BankPercentage = Main.Main._BizBillConfig.BankPercentage;

                float cityHallMoney = Price * (taxPercentage / 100f);

                float BankMoney = Price * (BankPercentage / 100f); ;

                float BizMoney = Price - cityHallMoney - BankMoney;

                if (Nova.biz.FetchBiz(Main.Main._BizBillConfig.CityHallId) != null)
                {
                    Nova.biz.FetchBiz(Main.Main._BizBillConfig.CityHallId).Bank += Math.Round(cityHallMoney, 2);
                    Nova.biz.FetchBiz(Main.Main._BizBillConfig.CityHallId).Save();
                }

                if (Nova.biz.FetchBiz(Main.Main._BizBillConfig.BankId) != null)
                {
                    Nova.biz.FetchBiz(Main.Main._BizBillConfig.BankId).Bank += Math.Round(BankMoney, 2);
                    Nova.biz.FetchBiz(Main.Main._BizBillConfig.BankId).Save();
                }

                player.biz.Bank += Math.Round(BizMoney, 2);
                player.biz.Save();

                SecondPlayer.AddBankMoney(-Price);
            }
            else
            {
                player.Notify("Erreur", "Une erreur inattendu vient de survenir, veuillez réessayer.", NotificationManager.Type.Error);
                SecondPlayer.Notify("Erreur", "Une erreur inattendu vient de survenir, veuillez réessayer.", NotificationManager.Type.Error);
            }
        }

        public void BizPanel_RefuseBill(Player player, Player SecondPlayer, float Price)
        {
            player.Notify("Facture déchirée", $"{SecondPlayer.GetFullName()} vient de déchirer la facture d'un montant de {Price}€", NotificationManager.Type.Success);
            SecondPlayer.Notify("Facture déchirée", $"Vous venez de déchirer la facture d'un montant de {Price}€", NotificationManager.Type.Success);
        }

        public async void ComptaPanel(Player player)
        {
            bool canManageTheBank = await PermissionUtils.PlayerCanManageTheBank(player);
            if (canManageTheBank)
            {
                Panel panel = Context.PanelHelper.Create($"Comptabilité de {player.biz.BizName}", UIPanel.PanelType.TabPrice, player, () => ComptaPanel(player));

                var data = await OrmManager.BizBill_LogsBill.QueryAll();

                foreach (OrmManager.BizBill_LogsBill ComptaData in data)
                {
                    panel.AddTabLine($"De {ComptaData.EmployeeName} à {ComptaData.CustomerName}", $"Montant : {ComptaData.Price}€", ItemUtils.GetIconIdByItemId(1112), _ => { });
                }

                panel.AddButton("Retour", ui => AAMenu.AAMenu.menu.BizPanel(player));
                panel.CloseButton();
                panel.Display();
            }
            else
            {
                player.Notify("Erreur", "Vous n'avez pas la permission de gérer la banque de votre entreprise !", NotificationManager.Type.Error);
            }
        }


    }
}