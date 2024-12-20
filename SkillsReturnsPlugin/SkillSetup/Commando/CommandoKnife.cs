﻿using EntityStates;
using RoR2;
using R2API;
using RoR2.Skills;
using SkillsReturns.SkillStates.Commando;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using SkillsReturns.SharedHooks;

namespace SkillsReturns.SkillSetup.Commando
{
    public class CommandoKnife : SkillBase<CommandoKnife>
    {
        public override string SkillName => "Commando - Combat Knife";

        public override string SkillLangTokenName => "COMMANDO_SECONDARY_SKILLSRETURNS_SLASHKNIFE_NAME";

        public override string SkillLangTokenDesc => "COMMANDO_SECONDARY_SKILLSRETURNS_SLASHKNIFE_DESCRIPTION";

        public override SkillFamily SkillFamily => Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Commando/CommandoBodySecondaryFamily.asset").WaitForCompletion();

        public static DamageAPI.ModdedDamageType knifeDamageType;
        public static BuffDef knifeDebuff;
        
        protected override void CreateAssets()
        {
            if (knifeDebuff) return;
            knifeDamageType = DamageAPI.ReserveDamageType();
            knifeDebuff = SkillsReturns.Utilities.CreateBuffDef("CommandoKnifeDebuff", false, false, true, new Color32(81, 0, 0, 255), Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Bandit2/texBuffSuperBleedingIcon.tif").WaitForCompletion());
           
        }

        protected override void Hooks()
        {
            GlobalEventManager.onServerDamageDealt += ApplyKnifeDebuff;
            On.RoR2.HealthComponent.TakeDamage += ChangeDamageColor;
            SharedHooks.ModifyFinalDamage.ModifyFinalDamageActions += AmplifyDamage;
        }

        private void AmplifyDamage(ModifyFinalDamage.DamageMult damageMult, DamageInfo damageInfo, HealthComponent victim, CharacterBody victimBody)
        {
            if (victimBody.HasBuff(knifeDebuff)) damageMult.value += 0.5f;
        }

        private void ApplyKnifeDebuff(DamageReport report)
        {
            if (!report.victimBody) return;
            if (report.damageInfo.rejected) return;
            if (!report.damageInfo.HasModdedDamageType(knifeDamageType)) return;

            report.victimBody.AddTimedBuff(knifeDebuff, 5f);
        }

        private void ChangeDamageColor(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            //Always use a NetworkServer.active check when doing stuff with TakeDamage
            if (NetworkServer.active && self.body && self.body.HasBuff(knifeDebuff) && damageInfo.damageColorIndex == DamageColorIndex.Default)
            {
                damageInfo.damageColorIndex = DamageColorIndex.SuperBleed;
            }

            //ALWAYS remember to call orig so that the original function can run.
            orig(self, damageInfo);
        }

        protected override void CreateSkillDef()
        {
            base.CreateSkillDef();
            skillDef.activationState = new SerializableEntityStateType(typeof(SlashKnife));
            skillDef.activationStateMachineName = "Weapon";
            skillDef.baseMaxStock = 1;
            skillDef.baseRechargeInterval = 3f;
            skillDef.beginSkillCooldownOnSkillEnd = false;
            skillDef.canceledFromSprinting = false;
            skillDef.cancelSprintingOnActivation = true;
            skillDef.fullRestockOnAssign = true;
            skillDef.interruptPriority = InterruptPriority.Skill;
            skillDef.isCombatSkill = true;
            skillDef.mustKeyPress = false;
            skillDef.rechargeStock = 1;
            skillDef.requiredStock = 1;
            skillDef.stockToConsume = 1;
            skillDef.icon = Assets.mainAssetBundle.LoadAsset<Sprite>("CombatKnifeIcon");
            skillDef.keywordTokens = new string[] { "KEYWORD_STUNNING" };

            LanguageAPI.Add(SkillLangTokenName, "Combat Knife");
            LanguageAPI.Add(SkillLangTokenDesc, "Slash enemies for <style=cIsDamage>360% damage</style>, wounding and <style=cIsDamage>stunning</style> enemies. Wounded enemies take <style=cIsDamage>50% more damage</style>.");
        }

        protected override void RegisterStates()
        {
            ContentAddition.AddEntityState(typeof(SlashKnife), out bool wasAdded);
        }
    }
}
