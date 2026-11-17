-- 2008.09.10 tavia
--帮组系统的配置------

local HelpSystem_Cofig = {};
--新手帮助
HelpSystem_Cofig[0] = {
 static_text=[[
|cffFFBBFFMap|cffffffff：Gods have endowed everybody in Gods War Online with a strong prescient ability. Hit |cffD5912BM|cffffffff to open the world map, and adjust the settings at the top of the window and to get corresponding information from the world map and minimap. It can be invaluable when travelling!	

|cffFFBBFFQuest|cffffffff：Do you see the NPCs with a |cffD5912B'?'|cffffffff above their heads? Accept quests from them. When you complete a certain quest, the corresponding NPC will be displayed with a|cffD5912B'!'|cffffffff. Press the |cffD5912B'Q'|cffffffff key to check your quests and search those you are eligible for but haven't accepted.

|cffFFBBFFCharacter|cffffffff：Hit |cffD5912BC|cffffffff to look into your character information. 

|cffFFBBFFParty|cffffffff：Click on a target and then click the |cffD5912B'operation'|cffffffff" button under his name in the upper center of the screen to invite him to team up with you.

|cffFFBBFFBackpack|cffffffff：Press the |cffD5912BB|cffffffff key to open your backpack.

|cffFFBBFFSkill|cffffffff：You can learn a new skill every time you reach a certain level. Purchase a book from the |cffD5912B'Skill Merchant'|cffffffff and right click it to learn the skill. Whereas, only |cffD5912B'Junior Books'|cffffffff are available at the Skill Merchant's, more adanced books can be obtained by killing monsters or from other players. Hit |cffD5912BS|cffffffff to display all the skills that you have learnt and drag the skill icon to the |cffD5912B'Action Bar'|cffffffff at the bottom of screen. After that you can cast skills just by pressing the relevant keys.

|cffFFBBFFSocial|cffffffff：Hit |cffD5912BF|cffffffff to bring up your social panel. You can add, remove, party and whisper to players. Even ignore them if you'd like. 

|cffFFBBFFCombat|cffffffff：Equip your weapon and left click on a target to attack.

|cffFFBBFFChat|cffffffff：Chat with players through different channels. Click players' names, add them to friends, send team invites or guild invites.
]]
}
--人物属性
HelpSystem_Cofig[1] = {
 static_text=[[
Open Char Info Panel Shortcut  |cffD5912B C |cffffff   
Oh, my god, I don't understand all these attributes.
"Let me explain them to you! It's really not so hard." (God's voice)
  |cffFFBBFFFaith|cffffffff：The god who your faction worships.
  |cffFFBBFFClass|cffffffff：Chosen when creating your character.
  |cffFFBBFFReputation|cffffffff：Your reputation in your faction.
  |cffFFBBFFGuild|cffffffff：You can only communicate with Gods through a guild.
  |cffFFBBFFPosition|cffffffff：You status in a guild.
  |cffFFBBFFContribution|cffffffff：The more contribution you have, the more favor from the Gods which means more benefits
  |cffFFBBFFHP|cffffffff：The character's max health points.
  |cffFFBBFFMP|cffffffff：The character's max mana points.
  |cffFFBBFFATK|cffffffff：a basic attribute of Warriors and Champions.
  |cffFFBBFFDEF|cffffffff：Avoid damage from attacks.
  |cffFFBBFFHealing|cffffffff：Bonuses from healing other players. A strong attribute of priests.
  |cffFFBBFFHit|cffffffff：Hit rate for attack.
  |cffFFBBFFDodge|cffffffff：Dodge attacks.
  |cffFFBBFFCrit|cffffffff：Causes deadly damage.
  |cffFFBBFFResilience|cffffffff：Defends against deadly damage.
  |cffFFBBFFDmg|cffffffff：The higher the level of attack, the more damage will be caused.
  |cffFFBBFFMD|cffffffff：The higher the level of magic attack, the more magic damage will be caused.
  |cffFFBBFFDmg Absorption|cffffffff：Absorbs both melee damage and magic damage.

 God: I told you so much, do you understand? If not, take another look.
]]
}
--任务
HelpSystem_Cofig[2] = {
 static_text=[[
   |cffFFBBFFMain Quest|cffffffff：Each person has a primary story. Come and get into the Gods War storyline, it can lead you to the most mysterious places in Ancient Greece. Furthermore, big rewards will be there waiting too.

   |cffFFBBFFRepeated Quest|cffffffff：This kind of quest can be done repeatedly so it can help players level up quickly.

   |cffFFBBFFFaction Quest|cffffffff：A faction is a big family, your people, even strangers from the same faction can do faction quests together. However, these kind of quests are usually diffcult, but the rewards are comparable to the difficulty.

   |cffFFBBFFDaily Quest|cffffffff：These quests deliver better rewards, but they are time limited. Also, the difficulty will increase as your level goes up.

   |cffFFBBFFGuild Quest|cffffffff：More and more resources need to be collected when Guild Quests are finished. It's crucial for the Guild's prosperity. To be a powerful Guild, all members should contribute to it. 

   |cffFFBBFFScroll Quest|cffffffff：Get these quests by killing monsters, and earn big rewards by doing them.
   
   When you reach level 10, you will be qualified to claim top gear and items from the 'Gear Giver' and 'Gift Envoy' in the suburbs every 5 levels until Level 40.

]]
}
--装备
HelpSystem_Cofig[3] = {
 static_text=[[Turn out the pockets… hm… Wow, there is a broken sheepskin, let me see…  
|cffFFBBFFWeapon|cffffffff：One-Handed Sword (Warrior), Two-Handed Spear (Champion), One-Handed Wand (Priest), Two-Handed Wand (Mage)
|cffFFBBFFHead|cffffffff：Helm (Melee), Crown (Melee and Mana), Cap (Mana)
|cffFFBBFFBody|cffffffff：Armor (Melee Defense), Clothes (Mana Defense)
|cffFFBBFFGloves|cffffffff：Increases shot rate.
|cffFFBBFFSleeves, Leggings|cffffffff：Increases both melee and mana defense.
|cffFFBBFFBoots|cffffffff：Increases dodge rate.  ^o^
|cffFFBBFFNecklace|cffffffff：Increases damage absorption. ～
|cffFFBBFFRing|cffffffff：Different attributes will be given to different levels. Equip as you need.
|cffFFBBFFShield|cffffffff：Exclusive for warriors and priests. It is useful for training, as it can increase defense.
Ah? The gear has different qualities? Let me search the pocket again……
|cffD5912BQuality|cffffffff and |cffD5912BGrade|cffffffff measure how good the gear is. The Gear is better with higher |cffFFBBFFQuality|cffffffff and |cffFFBBFFGrade|cffffffff, possibly even giving off a magic light, as well.
|cffFFBBFFQuality|cffffffff：Common,|cff00CCFFEnhanced|cffffffff,|cff2BD591Delicate|cffffffff,|cff08D631Good|cffffffff,|cffFF9900Superior|cffffffff,|cffFFFF00Classic|cffffffff,|cffFF88E7Eternal|cffffffff,|cffDD00B1Epic|cffffffff.
|cffFFBBFFStar|cffffffff：Grade 1, Grade 2,Grade 3……|cffD5912B'Surprise'|cffffffffgrades（The gear has been upgraded to such a high level, even the Gods are surprised. So it is called the |cffD5912B"Surprise"|cffffffff level）

]]
}
--交际
HelpSystem_Cofig[4] = {
 static_text=[[
    Communication is significant. To make friends, defeat |cffD5912B'BOSS'|cffffffffes, fight with  players from other factions or go adventuring in other factions territories requires lots of communication. Also players can whisper to each other or team up to do a quest.

   What's the |cffD5912B'Blacklist'|cffffffff? People who are placed into your Blacklist will never disturb you again, all their words will be blocked. WOW, the world is at peace. 

]]
}
--公会
HelpSystem_Cofig[5] = {
 static_text=[[
The guilds in GodsWar Online offer a place where players can communicate with each other and also worship Gods to improve their abilities.

|cffFFBBFFGuild Building|cffffffff: Guild officials can build various guild buildings to improve the level of the guilds, increase the max number of guild members and provide altars. Guild buildings are the basis of a guild.
 
|cffFFBBFFMake offerings to Gods:|cffffffff: Players can use contribution points to make offerings to the Gods. The more offerings are made, the more bonuses players will gain, also, more offerings will be consumed per hour. The God Altar will only endow bonuses to players who have made offering to Gods.
 
|cffFFBBFFGuild Quest|cffffffff: Guild officials can issue guild quests every day. Guild members may gain Guild Gold, Guild Silver and Guild Contribution points by completing these quests or by donating Silver and Gold.
 
|cffFFBBFFGuild Welfare|cffffffff: Find the “Devoted Acolyte” in the main city and claim offerings 10 times a week. The record will be reverted back to 0 on every Saturday.

]]
}
HelpSystem_Cofig[6] = {
 static_text=[[
1. Click |cffD5912B'Set'|cffffffff to open the |cffD5912B'GodsWar Guardian Elf 1.0'|cffffffff interface and then choose some options.
 
2.Drag general skills to the Shortcut below, and remember to tick the |cffD5912B'Auto-cast'|cffffffffsetting. The character will then cast skills in sequence. If a skill is cooling, the next skill will be cast.

3. Tick |cffD5912B'Auto-attack'|cffffffff to make the character attack automatically.

4.Tick auto-use Healing Potion or Mana Potion in |cffD5912B'Healing settings'|cffffffff. Input number 1-9 to allow the character to use potions when HP or MP fall below |cffD5912B10%-90%|cffffffff (can be adjusted).

5. Tick |cffD5912B'Healing Potion used up'|cffffffff or |cffD5912B'Mana Potion used up'|cffffffff under |cffD5912B'Auto-back to city settings'|cffffffff and drag a portal skill or portal scroll into the frame below. so when the character runs out of potions, it will return to the main city automatically.

6. Click |cffD5912B'Save'|cffffffff to save your settings and then click |cffD5912B'Start'|cffffffff, after that the character will enter |cffD5912B'AFK'|cffffffff mode. You can also press |cffD5912B'F12'|cffffffff or click |cffD5912B'AFK'|cffffffff on the interface. If you want to change anything, click |cffD5912B'Set'|cffffffff again.

]]
}
--推荐人
HelpSystem_Cofig[7] = {
 static_text=[[
The Recommendation System aims to encourage players to invite more friends into GodsWar Online and gives players rewards for doing so.

|cffFFBBFFObtain Recommender ID|cffffffff: click the "Buzz" button on the Stats panel to obtain a Recommender ID.
 
|cffFFBBFFUse of Recommender ID|cffffffff: Input your recommender ID when your friend creates their character, you and your friend must be on the same server.
 
|cffFFBBFFRewards|cffffffff: When your friend spends money in game, you will receive 5% of the total amount spent, but this will not be deductued from your friend's Gold
 
]]
}
--星座系统
HelpSystem_Cofig[8] = {
 static_text=[[
Everyone has a zodiac to watch over them, heroes included. What can your zodiac do for you?

|cffFFBBFFAccumulated EXP, Talent EXP|cffffffff: A good amount of EXP is often gathered by killing monsters, from quests and from EXP stones. The accumulated EXP serves as a bonus that increases as the Zodiac advances, resulting in a greater effect.

|cffFFBBFFSkill Training|cffffffff: Zodiac can train heroes and improve their skills.
Attack skill training and improvement are ways to increase attack power. Some skills will do a fixed amount of damage while some can elevate the amount of damage by a good percentage. As attack power increases, the amount of MP consumed in the process will increase as well.
Defensive skill training and improvement are ways to decrease the enemies' attack power, and that results in less damage. Some skills will allow a fixed amount of damage done while some can cut the damage done by a good percentage.

|cffFFBBFFActivating Skill Slots|cffffffff: Skill slots can be activated using E-gold. The more you activate, the more skills you can learn.

|cffFFBBFFUpgrading Skill Slots|cffffffff: Skill slots can be upgraded using zodiac energy and talent points.

]]
}

function Get_HelpSystem_Cofig(level)
	return HelpSystem_Cofig[level];
end




