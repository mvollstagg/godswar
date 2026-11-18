local Constellation_Cofig = {};
local Constellationlev_Cofig = {};
local Constellationlevup_Cofig = {};
local Stoneslev_Cofig = {};
local Stoneslevup_Cofig = {};

Constellation_Cofig[0] = {
 static_text=[[ Aries: March 21st-April 20th,  Patron Planet: Mars,  Patron God: Ares.

]]
}

Constellation_Cofig[1] = {
 static_text=[[ Taurus: April 21st-May 20th,  Patron Planet: Jupiter,  Patron God: Zeus.

]]
}

Constellation_Cofig[2] = {
 static_text=[[ Gemini: May 21st-June 21st,  Patron Planet: Moon,  Patron God: Dionysus. 

]]
}

Constellation_Cofig[3] = {
 static_text=[[ Cancer: June 22nd-July 22nd,  Patron Planet: Pluto,  Patron God: Hades.

]]
}

Constellation_Cofig[4] = {
 static_text=[[Leo: July 23rd-August 23rd,  Patron Planet: Sun,  Patron God: Apollo.

]]
}

Constellation_Cofig[5] = {
 static_text=[[Virgo: August 23rd-September 22nd,  Patron Planet: Venus,  Patron God: Demeter.

]]
}

Constellation_Cofig[6] = {
 static_text=[[Libra: September 23rd-Octorber 23rd,  Patron Planet: Uranus,  Patron God: Athena.

]]
}

Constellation_Cofig[7] = {
 static_text=[[Scorpio: Octorber 23rd-November 22nd,  Patron Planet: Earth,  Patron God: Artemis.

]]
}

Constellation_Cofig[8] = {
 static_text=[[Sagittarius: November 22nd-December 21st,  Patron Planet: Mars,  Patron God: Artemis.

]]
}

Constellation_Cofig[9] = {
 static_text=[[Capricorn: December 22nd-January 19th,  Patron Planet: Mercury,  Patron God: Demeter.

]]
}

Constellation_Cofig[10] = {
 static_text=[[Aquarius: January 20th-February 19th,  Patron Planet: Neptune,  Patron God: Poseidon.

]]
}

Constellation_Cofig[11] = {
 static_text=[[Pisces: February 19th-March 20th,  Patron Planet: Neptune,  Patron God: Aphrodite.

]]
}

Constellationlev_Cofig[0] = {

Constellationlev_Level1="1/10",
Player_Level1="10",


Constellationlev_Level2="2/10",
Player_Level2="25",


Constellationlev_Level3="3/10",
Player_Level3="40",


Constellationlev_Level4="4/10",
Player_Level4="60",


Constellationlev_Level5="5/10",
Player_Level5="82",


Constellationlev_Level6="6/10",
Player_Level6="94",

Constellationlev_Level7="7/10",
Player_Level7="103",


Constellationlev_Level8="8/10",
Player_Level8="108",


Constellationlev_Level9="9/10",
Player_Level9="113",


Constellationlev_Level10="10/10",



}

Constellationlevup_Cofig[0] = {

Constellationlevup_Level1=500,
MaxPower1=1000,

Constellationlevup_Level2=2000,
MaxPower2=3000,


Constellationlevup_Level3=4000,
MaxPower3=8000,


Constellationlevup_Level4=8000,
MaxPower4=20000,


Constellationlevup_Level5=20000,
MaxPower5=28000,


Constellationlevup_Level6=30000,
MaxPower6=42000,

Constellationlevup_Level7=44000,
MaxPower7=60000,


Constellationlevup_Level8=65000,
MaxPower8=80000,


Constellationlevup_Level9=85000,
MaxPower9=100000,

MaxPower10=125000,

}

Stoneslev_Cofig[0] = {


Speciality_Level1="1%",
Experience_Level1="1%",
sk_max1="2000",
exp_max1="3000",


Speciality_Level2="2%",
Experience_Level2="2%",
sk_max2="4000",
exp_max2="6000",


Speciality_Level3="3%",
Experience_Level3="3%",
sk_max3="8000",
exp_max3="20000",


Speciality_Level4="4%",
Experience_Level4="4%",
sk_max4="15000",
exp_max4="80000",


Speciality_Level5="5%",
Experience_Level5="5%",
sk_max5="20000",
exp_max5="240000",


Speciality_Level6="6%",
Experience_Level6="6%",
sk_max6="30000",
exp_max6="420000",


Speciality_Level7="7%",
Experience_Level7="7%",
sk_max7="40000",
exp_max7="820000",


Speciality_Level8="8%",
Experience_Level8="8%",
sk_max8="50000",
exp_max8="1600000",


Speciality_Level9="9%",
Experience_Level9="9%",
sk_max9="60000",
exp_max9="2400000",


Speciality_Level10="10%",
Experience_Level10="10%",
sk_max10="70000",
exp_max10="3800000",

}

Stoneslevup_Cofig[0] = {

Power_Level1=200,
Gold_Level1=70,

Power_Level2=500,
Gold_Level2=230,

Power_Level3=2000,
Gold_Level3=830,

Power_Level4=6000,
Gold_Level4=1380,

Power_Level5=12000,
Gold_Level5=2060,

Power_Level6=20000,
Gold_Level6=2950,

Power_Level7=32000,
Gold_Level7=4130,

Power_Level8=50000,
Gold_Level8=6880,

Power_Level9=75000,
Gold_Level9=9180,

Power_Level10=100000,
Gold_Level10=12850,

}

function Get_Constellation_Cofig(level)
	return Constellation_Cofig[level];
end



function Get_Constellationlev_Cofig()
	return Constellationlev_Cofig[0];
end

function Get_Constellationlevup_Cofig()
	return Constellationlevup_Cofig[0];
end

function Get_Stoneslev_Cofig(Num)
	return Stoneslev_Cofig[Num];
end

function Get_Stoneslevup_Cofig(Num)
	return Stoneslevup_Cofig[Num];
end
