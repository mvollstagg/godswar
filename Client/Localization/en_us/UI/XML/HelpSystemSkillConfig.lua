-- 2008.09.10 tavia
--帮组系统技能的配置------
--战士1-100
--斗士101-200
--祭祀201-300
--法师301-400
--配置表项最多：Descrip，ID，MaxLev，Power2，Power1，Values，Time，CoolingTime，MP，Level
--Power2，Power1，CoolingTime，MP的值从magic.ini中读取
--Values，Time的值从statues.ini中读取，有状态的情况下才读取
--其它项都是手动 配置

local HelpSystem_SkillCofig = {};

HelpSystem_SkillCofig[1] = {
Descrip=[[Light Chop：Chops the target with light, dealing physical damage caused by physical attack power plus physical damage.


                     Lesser   Middle  Greater   Major  
Base Damage:         220      350      530      900
Damage Bonus:        50%      60%      80%     100%
Cooldown:            10        10       10       10
Consume MP:          12       32       60       100
Available level:      1        15       30       45

]]

}

HelpSystem_SkillCofig[2] = {
Descrip=[[Whirlwind：Wield a sword to produce a whirlwind, dealing physical damage caused by physical attack power plus physical damage.

                     Lesser    Middle   Greater   Major
Base Damage:         300       520       750      1120
Damage Bonus:        100%      120%      160%     200%
Cooldown:             10        10        10       10
Consume MP:           50        72       120      220
Available level:      35        55        70       85

]]
}

HelpSystem_SkillCofig[3] = {
Descrip=[[Flash Chop:Chops the target with flash, dealing physical damage caused by physical attack power plus physical damage.

                     Lesser    Middle   Greater   Major 
Base Damage:         825       1200      1650     2100
Damage Bonus:        140%      170%      220%     300% 
Cooldown:             10        10        10        10
Consume MP:           90        150       240      380
Available level:      75        90        100      110

]]
}

HelpSystem_SkillCofig[4] = {
Descrip=
[[Sword Storm:Freezes the nearby air and then transforms the ice into sharp blades to attack nearby enemies, dealing physical damage caused by physical attack power plus physical damage.

                     Lesser    Middle   Greater   Major
Base Damage:         200       350       700     1200
Damage Bonus:        20%       30%       50%      80%
Cooldown:             16        16        16        16
Consume MP:           40        80       140      250
Available level:      10        30        50       70

]]
}

HelpSystem_SkillCofig[5] = {
Descrip=[[Steel Rain:Transforms warrior's rage into sharp blades to attack nearby enemy targets, dealing physical damage caused by physical attack power plus physical damage.

                     Lesser    Middle   Greater   Major
Base Damage:         600       900       1200     2000
Damage Bonus:        40%        70%       100%     150%
Cooldown:             16         16        16       16
Consume MP:          120        220       350      600
Available level:     55         80         97       106

]]
}

HelpSystem_SkillCofig[6] = {
Descrip=[[Blood Claw:Attacks the target with blood claw for physical damage. Chance to hit causing for sec per sec losing health.
														
					              Lesser   Middle  Greater  Major
Base Damage:         100      160     280     400
Damage Bonus:        20%      20%     40%     50%
Damage every 2 sec:  24       40      90      140
Consume MP:           8        15      30       60
Available level:      5        25      45       65
]]
}

HelpSystem_SkillCofig[7] = {
Descrip=[[Rage Claw:Attacks the target with rage claw for physical damage caused by physical attack power plus physical damage. Chance to hit causing for sec per sec losing health.
														
				                Lesser    Middle  Greater   Major
Base Damage:         150       220      300      400
Damage Bonus:        50%       50%      80%      100%
Damage every 2 sec:  60        100      180      250
Consume MP:           25        50       75       100
Available level:      50        70       90       109

]]
}

HelpSystem_SkillCofig[8] = {
Descrip=[[Stun：Stuns the target. Chance to hit causing for sec. During the duration, the target cannot perform any action.

                  Lesser  Middle  Greater   Major  Super
Duration s:        3       3        3         3       3
Cooldown:          30      26       23       20      18
Consume MP:        25      60      110      180     250
Available level:   14      40      66       85      103

]]
}

HelpSystem_SkillCofig[9] = {         
Descrip=[[Expose Armor:Casts on nearby enemy targets for sec, reducing both their physical defense and magical defense.

					            Lesser   Middle  Greater  Major Super
Consume MP:         50　　  80　　  110 　    150 　 200
Reduce defense:     30　    70　    120 　   200 　 300
Reduce M.Defense:   15　　  35　　   60　　   100 　 150
Available level:    20　    42　　  64　　    86　　108 
 
]]
}

HelpSystem_SkillCofig[10] = {       
Descrip=[[Holy Ward: Conjures a holy ward to reduce physical and magical damage by a certain percentage within a certain amount of time. The cooldown lasts 10 seconds.

				             Lesser   Middle  Greater  Major  Super
Consume MP: 	     35       45 	    60 	     90 	    120
Reduce damage:    10%      13%     16%      20%     25%
Reduce M.damage:   0%       0%      5%      10%     15%
Duration s:        120      120     150      180     240
Available level:   10       34      58       82     106 

]]
}

HelpSystem_SkillCofig[101] = {
Descrip=
[[Jab：Jabs the target, dealing physical damage caused by physical attack power plus physical damage.

				                 Lesser  Middle Greater Major
Base Damage:          150     250     350     500
Damage Bonus:         20%     40%    60%     100%
Cooldown:              6       6       6       6
Consume MP:            10     26      48      75
Available level:       1       15      30      45

]]
}

HelpSystem_SkillCofig[102] = {
Descrip=[[Holy Jab：Jabs the target heavily, dealing physical damage caused by physical attack power plus physical damage.

				                 Lesser  Middle  Greater  Major
Base Damage:          200     320     550      800
Damage Bonus:         50%     80%     110%     140%
Cooldown:              6       6       6        6
Consume MP:           40      60      90       135
Available level:      35      55      70       85

]]
}

HelpSystem_SkillCofig[103] = {
Descrip=[[Stab：Stabs the target, dealing physical damage caused by physical attack power plus physical damage.

				                Lesser   Middle  Greater  Major
Base Damage:         450      700     900      1200
Damage Bonus:        100%     120%    160%     220%
Cooldown:             6        6        6        6
Consume MP:          75       100      140     200
Available level:     75       90       100      110

]]
}

HelpSystem_SkillCofig[104] = {
Descrip=[[Rage Spear：Transfers rage to spear head to attack the target, dealing physical damage caused by physical attack power plus physical damage.

				               Lesser    Middle   Greater  Major
Base Damage:         350      600      1000     1600
Damage Bonus:        100%     120%     160%     220%
Cooldown:             25       25       25        25
Consume MP:           50       110      180      300
Available level:      10       30       50        70

]]
}

HelpSystem_SkillCofig[105] = {
Descrip=[[Spear Hit：Attacks the target extremely fast using a spear, dealing physical damage caused by physical attack power plus  physical damage.

				                Lesser   Middle  Greater   Major
Base Damage:         700      1000     1800     3200
Damage Bonus:        140%     180%     250%     340%
Cooldown:             25       25        25       25
Consume MP:           140     220       350      500
Available level:      55       80        97      106

]]
}

HelpSystem_SkillCofig[106] = {
Descrip=[[Ruined Strike：Swing the spear to attack all nearby enemy targets, dealing physical damage caused by physical attack power plus  physical damage.

				                Lesser  Middle  Greater   Major
Base Damage:         120      200     350      600
Damage Bonus:        40%      50%     70%      90%
Cooldown:             18       18       18       18 
Consume MP: 	        20       45       75       120
Available level:      5        25       45       65

]]
}

HelpSystem_SkillCofig[107] = {
Descrip=[[Chip Blast：Blasts all nearby enemy targets with chips, dealing physical damage caused by physical attack power plus physical damage.

				                Lesser    Middle  Greater  Major
Base Damage:         300       420      600      900
Damage Bonus:        60%       70%     100%     130%
Cooldown:             16        16        16       16
Consume MP:           60        90       140      220
Available level:      53        75        95       109

]]
}

HelpSystem_SkillCofig[108] = {       
Descrip=[[Spear Blast：|Blasts all nearby enemy targets with spears, dealing physical damage caused by physical attack power plus physical damage. Chance to hit causing for sec, increasing the target's physical damage taken by 10%.
								         Lesser    Middle   Greater    Major
Base Damage:　　    200 　　  400 　　  800 　　  1500
Damage Bonus:　　   70%　     80%       100%　    140%
Increase damage:    10%　　   10%　     15%　　   15% 
Duration s:          12　　    20　　    12　　    20
Consume MP:          60　　   100　　   200　　   350
Available level:     15　　    30　　    45　　    60

]]
}

HelpSystem_SkillCofig[109] = {
Descrip=[[Meteor Blast：Blasts all nearby enemy targets with meteors, dealing physical damage caused by physical attack power plus physical damage. Chance to hit causing for sec to increase its physical damage taken by 10%.
								     
									          Lesser    Middle   Greater   Major
Base Damage:          300      600      1200      1800
Damage Bonus:         120%     130%     150%      180%
Increase damage:      10%      10%      15%        15% 
Duration s:            12       20       12         20
Consume MP:           180      270      450        750
Available level:       50       70       90        105

]]
}

HelpSystem_SkillCofig[110] = {     
Descrip=[[Sacred Zeal：Enters sacred zeal stance for sec, increasing hit and critical chance.
								     
								       Lesser   Middle  Greater   Major  Super
Consume MP:        50      90      130      200     300
Duration s:        120     120      150      180    240 
Increase hit:      10      20       30       45      60
Increase crit:     4       8        12       18      24
Available level:   10      34       58       82      106

]]
}

HelpSystem_SkillCofig[111] = {       
Descrip=[[Freeze：Freezes the target suddenly. Chance to hit causing for sec. During the duration, the target cannot move.
								
								       Lesser  Middle  Greater   Major   Super
Consume MP:        50      80      120      180     250
Cooldown:          60      55      50       45      40
Available level:   14      40      66       88      103

]]
}

HelpSystem_SkillCofig[301] = {
Descrip=[[Fireball：Hurls a fiery ball to the target, dealing magical damage caused by magical attack power plus magical damage.

                    Lesser   Middle   Greater  Major
Base Damage:       70       120      200      280
Damage Bonus:      70%      70%      80%      90%
Consume MP:         8        12       20       30
Available level:    1        15        30       45

]]
}

HelpSystem_SkillCofig[302] = {
Descrip=[[Lightball：Hurls a lightball to the target, dealing magical damage caused by magical attack power plus magical damage.
                     
                  Lesser   Middle   Greater  Major 
Base Damage:     170      220      320      500  
Damage Bonus:    80%      80%      100%     110%
Cooldown:         1         1        1        1
Consume MP:       15       25       40       60
Available level:  35       55       70       85

]]
}

HelpSystem_SkillCofig[303] = {
Descrip=[[Lightblade：Hurls a lightblade to the target, dealing magical damage caused by magical attack power plus magical damage.

                    Lesser   Middle  Greater  Major 
Base Damage:       300      400     550      700
Damage Bonus:      100%     100%    110%     120%
Cooldown:           1        1        1        1
Consume MP:         30       50      75       100
Available level:    75       90      100      110

]]
}

HelpSystem_SkillCofig[304] = {
Descrip=[[Thunder：Call down thunder to attack the target, dealing magical damage caused by magical attack power plus magical damage.

                   Lesser  Middle   Greater  Major
Base Damage:      300      500     900      1400 
Damage Bonus:     100%     140%    180%     250%
Cooldown:          22       22      22       22
Consume MP:        60       100     180      320
Available level:   10       30       50       70

]]
}

HelpSystem_SkillCofig[305] = {
Descrip=[[Fire Lotus：Use essence of fire to blast the target like a fire lotus, dealing magical damage caused by magical attack power plus magical damage.

                  Lesser   Middle  Greater  Major
Base Damage:     600      900     1500     2800
Damage Bonus:    160%     220%    280%     350%
Cooldown:         22       22      22       22
Consume MP:       140      260     450      700
Available level:  55       80       97      108

]]
}

HelpSystem_SkillCofig[306] = {
Descrip=[[Star Shower：Call down meteors to pelt the target area, dealing magical damage caused by magical attack power plus magical damage.

                 Lesser   Middle   Greater  Major
Base Damage:     150     280      500      800
Damage Bonus:    50%     80%      120%     160%
Cooldown:         18      18        18       18
Consume MP:       70      150      300      500
Available level:  5       25        45       65

]]
}

HelpSystem_SkillCofig[307] = {
Descrip=[[Thundercloud：Call down thunderclouds to pelt the target area, dealing magical damage caused by magical attack power plus magical damage.

                     Lesser   Middle  Greater  Major
Added damage:       400      550     800      1200
Damage Bonus:       100%     140%    200%     250%
Cooldown:           18        18      18       18
Consume MP:         220      400     750      1100
Available level:    50        75      95       109

]]
}

HelpSystem_SkillCofig[308] = {
Descrip=[[Flame Blast：Use essence of fire to pelt the target area, per sec, dealing magical damage caused by magical attack power plus  magical damage for 5 sec.

                   Lesser  Middle Greater  Major  Super
Base Damage:      24      52     86       124     180
Damage Bonus:     20%     20%    40%      40%     50%
Cooldown:          2        2      2        2       2
Consume MP:        20      45     75       120     180
Available level:   10      34     58       82      106

]]
}

HelpSystem_SkillCofig[309] = {
Descrip=[[Fire Blast: Blasts all enemies within range. Deals magical damage. 

                   Lesser  Middle  Greater Major   Super
Base Damage:      600     1000    1600    2500    3500
Damage Bonus:     120%    160%    220%    300%    400%
Cooldown:          180     180     180     180     180
Consume MP:        400     600     900    1500    2000
Available level:   30       52      74     96      106

]]
}

HelpSystem_SkillCofig[310] = {     
Descrip=[[Five-star Form：Uses some of the Gods power for sec to reduce magical and physical damage.

							            Lesser  Middle Greater Major Super
Reduce damage（%）:  8      10      12	     15	   20
Reduce M.damage（%）:10     15      20       27    35
Duration s:          120     120    150      180   240
Consume MP:          120    170     250      350   500
Available level:     10      34     58       82    102

]]
}

HelpSystem_SkillCofig[311] = {
Descrip=[[Silence：Silences the target for a time. Chance to hit causing for sec. The target cannot cast spells for the duration of the effect.

                    Lesser   Middle  Greater  
Duration s:         10       10      10
Cooldown:           50       42      38
Consume MP:         100      180     250
Available level:    26       72      104

]]
}

HelpSystem_SkillCofig[201] = {
Descrip=[[Heal：Heals the friendly target for.

                 Lesser  Middle  Greater Major   Super
Recovery:        500     900     1600    2500    4000
Cooldown:        8       8       8        8       8
Consume MP:      60      100     180     350     600
Available leve:  6       34      62      88      104

]]
}

HelpSystem_SkillCofig[202] = {
Descrip=[[Area Heal：Heals all nearby allies for.

                 Lesser  Middle  Greater Major  Super
Recovery:        300     500     800     1400   2200
Cooldown:        18      18      18      18      18
Consume MP:      120     200    380     750     1400
Available level: 10      38      66      92      100

]]
}

HelpSystem_SkillCofig[203] = {
Descrip=[[Gaia Care:	Uses some of the Gods power for sec to increase the chance to dodge and avoid painful damage.
								      
							        Lesser  Middle  Greater  Major  Super
Duration s:      5       5        7        8      10
Cooldown:        150     120     120      90     90
Consume MP:      100     150     220      300    400
Available level: 14      40      66       85     102

]]
}

HelpSystem_SkillCofig[204] = {     
Descrip=[[Mana Shield：Use essence of air to form a mana shield. For sec increases both physical defense and magical defense.
								       
									         Lesser Middle  Greater Major  Super
Increase defense:   14     38      70      125     200
Increase M.defense: 6      18      30       60      95
Duration s:         120    120     150      180     240
Consume MP:         100    150     210      300     450
Available level:    10     34      58       82      106 
]]
}

HelpSystem_SkillCofig[205] = {
Descrip=[[Shackle: Shackles the target using the power of wind. The target cannot perform any action for the duration of the effect, but the damage done will only be 25% the norm. As the skill level improves, the success rate upon performance increases. 

                      Lesser     Middle     Greater    
Duration:             10         10         10
Cooldown:             50         42         38
Consume MP:           100        160        240
Available level:      26         72         106 
]]
}

HelpSystem_SkillCofig[206] = {
Descrip=[[Ice Shot：Hurls an ice arrow at the enemy, dealing magical damage caused by magical attack power plus magical damage.

                    Lesser   Middle   Greater   Major   
Base Damage:       100      250       420       750
Damage Bonus:      50%      50%       50%       50% 
Consume MP:        10       18        30        48
Available level:   1        20        40        60

]]
}

HelpSystem_SkillCofig[207] = {
Descrip=[[Windblade：Hurls a windblade at the enemy, dealing magical damage caused by magical attack power plus magical damage.
                   Lesser   Middle    Greater   Major 
Base Damage:      250      500       800       1200
Damage Bonus:     70%      70%       70%       70%
Cooldown:          1         1         1         1
Consume MP:       36        50        72        100
Available level:  60        80        95        110

]]
}

HelpSystem_SkillCofig[208] = {
Descrip=[[Ice Cone：Binds water vapor into ice cones to attack the enemy, dealing magical damage caused by magical attack power plus magical damage.

                      Lesser  Middle   Greater  Major   
Base Damage:         360      500     700      1000
Damage Bonus:        50%      80%     110%     150%
Cooldown:             12       12       12       12
Consume MP:           50       90      140      210
Available level:      8        22      36        50

]]
}

HelpSystem_SkillCofig[209] = {
Descrip=[[Dripstone：Call up innumerous dripstones from underground to attack the enemy, dealing magical damage caused by magical attack power plus magical damage.

                     Lesser   Middle   Greater  Major
Base Damage:        500      750      1000     1500  
Damage Bonus:       120%     140%     180%     220%
Cooldown:           12        12        12       12
Consume MP:         110       180      270      420
Available level:    37        57       72       87

]]
}

HelpSystem_SkillCofig[210] = {
Descrip=[[Holy Light：Use essence of light to blast the enemy, dealing magical damage caused by magical attack power plus magical damage.

                    Lesser   Middle   Greater  Major 
Base Damage:       650      750      900      1200 
Damage Bonus:      200%     250%     300%     340%
Cooldown:          12        12       12        12
Consume MP:        250      340      440      650 
Available level:   75        90       97      108

]]
}

HelpSystem_SkillCofig[211] = {
Descrip=[[Stalagmite：Call up innumerous stalagmites from underground to attack all nearby enemies, dealing magical damage caused by magical attack power plus magical damage.

                       Lesser   Middle   Greater  Major 
Base Damage:          180      350      600      1000
Damage Bonus:         50% 	    80%      120%     150%
Cooldown:              24       24       24       24
Consume MP:            80       170      340      550
Available level:       5         25       45       65
 
]]
}

HelpSystem_SkillCofig[212] = {
Descrip=[[Ternary Curse:Gathers the essence of water, fire and light to attack all nearby enemies, dealing magical damage caused by magical attack power plus magical damage.


                     Lesser   Middle   Greater  Major
Base Damage:        450      800      1200     2000
Damage Bonus:       100%     130%     170%     200%
Cooldown:            24       24       24       24
Consume MP:          250      450      800      1200
Available level:     55        75       95       109

]]
}

function Get_HelpSystem_SkillCofig(level)
	return HelpSystem_SkillCofig[level];
end




