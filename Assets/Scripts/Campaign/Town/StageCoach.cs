﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StageCoach : Building
{
    string[] firstHeroClasses = new string[2] { "plague_doctor","vestal" };

    public int BaseRecruitSlots { get; set; }
    public int RecruitSlots { get; set; }

    public int BaseRosterSlots { get; set; }
    public int RosterSlots { get; set; }

    public int CurrentRecruitMaxLevel { get; set; }

    public List<SlotUpgrade> RecruitSlotUpgrades { get; set; }
    public List<SlotUpgrade> RosterSlotUpgrades { get; set; }
    public List<RecruitUpgrade> RecruitExperienceUpgrades { get; set; }

    public List<Hero> Heroes { get; set; }

    public StageCoach()
    {
        RecruitSlotUpgrades = new List<SlotUpgrade>();
        RosterSlotUpgrades = new List<SlotUpgrade>();
        RecruitExperienceUpgrades = new List<RecruitUpgrade>();
        Heroes = new List<Hero>();
    }

    void GeneratePurchaseInfo(Hero hero, Estate estate)
    {
        estate.HeroPurchases.Add(hero.RosterId, new Dictionary<string, UpgradePurchases>());

        foreach (var tree in DarkestDungeonManager.Data.UpgradeTrees.Values.ToList().FindAll(item => item.Id.StartsWith(hero.HeroClass.StringId)))
            estate.HeroPurchases[hero.RosterId].Add(tree.Id, new UpgradePurchases(tree.Id));
        foreach (var skill in hero.HeroClass.CampingSkills)
            estate.HeroPurchases[hero.RosterId].Add(skill.Id, new UpgradePurchases(skill.Id));

        if(hero.Weapon.UpgradeLevel > 1)
        {
            var weaponPurchases = estate.HeroPurchases[hero.RosterId][hero.ClassStringId + ".weapon"];

            for(int i = 0; i < hero.Weapon.UpgradeLevel - 1; i++)
                weaponPurchases.PurchasedUpgrades.Add(i.ToString());
        }
        if (hero.Armor.UpgradeLevel > 1)
        {
            var armorPurchases = estate.HeroPurchases[hero.RosterId][hero.ClassStringId + ".armour"];

            for (int i = 0; i < hero.Weapon.UpgradeLevel - 1; i++)
                armorPurchases.PurchasedUpgrades.Add(i.ToString());
        }
        
        for (int i = 0; i < hero.CurrentCombatSkills.Length; i++)
        {
            if (hero.CurrentCombatSkills[i] != null)
            {
                string treeName = hero.ClassStringId + "." + hero.CurrentCombatSkills[i].Id;
                var skillTree = DarkestDungeonManager.Data.UpgradeTrees[treeName];

                estate.HeroPurchases[hero.RosterId][treeName].PurchasedUpgrades.Add(skillTree.Upgrades[0].Code);
                if (hero.Resolve.Level > 0)
                {
                    for(int j = 1; j < skillTree.Upgrades.Count; j++)
                    {
                        if((skillTree.Upgrades[j] as HeroUpgrade).PrerequisiteResolveLevel <= hero.Resolve.Level)
                            estate.HeroPurchases[hero.RosterId][treeName].PurchasedUpgrades.Add(skillTree.Upgrades[j].Code);
                    }
                }
            }
        }
        estate.ReskillCombatHero(hero);
        for (int i = 0; i < hero.CurrentCampingSkills.Length; i++)
            if (hero.CurrentCampingSkills[i] != null)
                estate.HeroPurchases[hero.RosterId][hero.CurrentCampingSkills[i].Id].PurchasedUpgrades.Add("0");
    }

    public void Reset()
    {
        RecruitSlots = BaseRecruitSlots;
        RosterSlots = BaseRosterSlots;
        CurrentRecruitMaxLevel = 0;
    }

    public void RestockHeroes(List<int> rosterIds, Estate estate)
    {
        var heroClasses = DarkestDungeonManager.Data.HeroClasses.Keys.ToList();

        if(Heroes != null && Heroes.Count > 0)
        {
            for(int i = 0; i < Heroes.Count; i++)
            {
                if (rosterIds.Contains(Heroes[i].RosterId))
                {
                    Debug.LogError("Same id returned while restocking heroes.");
                }
                else
                    rosterIds.Add(Heroes[i].RosterId);

                estate.HeroPurchases.Remove(Heroes[i].RosterId);
            }
        }
        Heroes = new List<Hero>();
        if(DarkestDungeonManager.RaidManager.Quest != null && DarkestDungeonManager.RaidManager.Quest.Goal.Id == "tutorial_final_room")
        {
            for (int i = 0; i < firstHeroClasses.Length; i++)
            {
                int id = rosterIds[Random.Range(0, rosterIds.Count)];
                var newHero = new Hero(id, firstHeroClasses[i],
                    LocalizationManager.GetString("hero_name_" + Random.Range(0, 556).ToString()));
                Heroes.Add(newHero);
                rosterIds.Remove(id);
                GeneratePurchaseInfo(newHero, estate);
            }
        }
        else
        {
            for (int i = 0; i < RecruitSlots; i++)
            {
                RecruitUpgrade experienceUpgrade = null;

                if(CurrentRecruitMaxLevel > 0)
                {
                    for(int j = 0; j <= RecruitExperienceUpgrades.Count - 1; j++)
                    {
                        if (RecruitExperienceUpgrades[j].Level <= CurrentRecruitMaxLevel && RandomSolver.CheckSuccess(RecruitExperienceUpgrades[j].Chance))
                        {
                            experienceUpgrade = RecruitExperienceUpgrades[j];
                            break;
                        }
                    }
                }
                int id = rosterIds[Random.Range(0, rosterIds.Count)];
                string heroClass = heroClasses[Random.Range(0, DarkestDungeonManager.Data.HeroClasses.Count)];
                string heroName = LocalizationManager.GetString("hero_name_" + Random.Range(0, 556).ToString());
                var newHero = experienceUpgrade == null ? new Hero(id, heroClass, heroName) : new Hero(id, heroClass, heroName, experienceUpgrade);
                Heroes.Add(newHero);
                rosterIds.Remove(id);
                GeneratePurchaseInfo(newHero, estate);
            }
        }
        int abominations = DarkestDungeonManager.Campaign.Heroes.FindAll(hero => hero.Class == "abomination").Count + Heroes.FindAll(hero => hero.Class == "abomination").Count;
        int additionalHeroes = 4 - DarkestDungeonManager.Campaign.Heroes.Count - Heroes.Count + abominations;
        if(abominations > 3)
            return;
        for(int i = 0; i < additionalHeroes; i++)
        {
            RecruitUpgrade experienceUpgrade = null;

            if (CurrentRecruitMaxLevel > 0)
            {
                for (int j = 0; j <= RecruitExperienceUpgrades.Count - 1; j++)
                {
                    if (RecruitExperienceUpgrades[j].Level <= CurrentRecruitMaxLevel && RandomSolver.CheckSuccess(RecruitExperienceUpgrades[j].Chance))
                    {
                        experienceUpgrade = RecruitExperienceUpgrades[j];
                        break;
                    }
                }
            }
            int id = rosterIds[Random.Range(0, rosterIds.Count)];
            string heroClass = "abomination";
            while (heroClass == "abomination")
                heroClass = heroClasses[Random.Range(0, DarkestDungeonManager.Data.HeroClasses.Count)];

            string heroName = LocalizationManager.GetString("hero_name_" + Random.Range(0, 556).ToString());
            var newHero = experienceUpgrade == null ? new Hero(id, heroClass, heroName) : new Hero(id, heroClass, heroName, experienceUpgrade);
            Heroes.Add(newHero);
            rosterIds.Remove(id);
            GeneratePurchaseInfo(newHero, estate);
        }
    }

    public void InitializeBuilding(Dictionary<string, UpgradePurchases> purchases)
    {
        Reset();

        for (int i = RosterSlotUpgrades.Count - 1; i >= 0; i--)
        {
            if (purchases[RosterSlotUpgrades[i].TreeId].PurchasedUpgrades.Contains(RosterSlotUpgrades[i].UpgradeCode))
            {
                RosterSlots = RosterSlotUpgrades[i].NumberOfSlots;
                break;
            }
        }

        for (int i = RecruitSlotUpgrades.Count - 1; i >= 0; i--)
        {
            if (purchases[RecruitSlotUpgrades[i].TreeId].PurchasedUpgrades.Contains(RecruitSlotUpgrades[i].UpgradeCode))
            {
                RecruitSlots = RecruitSlotUpgrades[i].NumberOfSlots;
                break;
            }
        }

        for(int i = RecruitExperienceUpgrades.Count - 1; i>= 0; i--)
        {
            if (purchases[RecruitExperienceUpgrades[i].TreeId].PurchasedUpgrades.Contains(RecruitExperienceUpgrades[i].UpgradeCode))
            {
                CurrentRecruitMaxLevel = RecruitExperienceUpgrades[i].Level;
                break;
            }
        }
    }

    public void UpdateBuilding(Dictionary<string, UpgradePurchases> purchases)
    {
        Reset();

        for (int i = RosterSlotUpgrades.Count - 1; i >= 0; i--)
        {
            if (purchases[RosterSlotUpgrades[i].TreeId].PurchasedUpgrades.Contains(RosterSlotUpgrades[i].UpgradeCode))
            {
                RosterSlots = RosterSlotUpgrades[i].NumberOfSlots;
                break;
            }
        }

        for (int i = RecruitSlotUpgrades.Count - 1; i >= 0; i--)
        {
            if (purchases[RecruitSlotUpgrades[i].TreeId].PurchasedUpgrades.Contains(RecruitSlotUpgrades[i].UpgradeCode))
            {
                RecruitSlots = RecruitSlotUpgrades[i].NumberOfSlots;
                break;
            }
        }

        for (int i = RecruitExperienceUpgrades.Count - 1; i >= 0; i--)
        {
            if (purchases[RecruitExperienceUpgrades[i].TreeId].PurchasedUpgrades.Contains(RecruitExperienceUpgrades[i].UpgradeCode))
            {
                CurrentRecruitMaxLevel = RecruitExperienceUpgrades[i].Level;
                break;
            }
        }
    }

    public ITownUpgrade GetUpgradeByCode(string treeId, string code)
    {
        ITownUpgrade upgrade = RosterSlotUpgrades.Find(item => item.UpgradeCode == code && item.TreeId == treeId);
        if (upgrade == null)
            upgrade = RecruitSlotUpgrades.Find(item => item.UpgradeCode == code && item.TreeId == treeId);
        if (upgrade == null)
            upgrade = RecruitExperienceUpgrades.Find(item => item.UpgradeCode == code && item.TreeId == treeId);
        return upgrade;
    }
}