print("load SrvMsg.lua Success!!!");
Base:dofile("./Localization/en_us/UI/Base/text.lua");
--显示的位置
local CHANNEL_MIDDLE = 0	--屏幕中间
local CHANNEL_PRESONAL = 1	--屏蔽右下角
local CHANNEL_MOVE = 2		--跑马灯

--句子的格式
local NOTE_1 = 0	--技能书
local NOTE_10 = 1	--战场
local NOTE_20 = 2	--跨级任务
local NOTE_30 = 3	--公会
local NOTE_40 = 4	--新手活动1
local NOTE_50 = 5	--新手活动2
local NOTE_60 = 6	--新手活动3
local NOTE_70 = 7	--公会保卫活动
local NOTE_80 = 8	--
local NOTE_90 = 9	--
local NOTE_100 = 10	--
local NOTE_110 = 11	--
local NOTE_120 = 12	--
local NOTE_130 = 13	--
local NOTE_140 = 14
local NOTE_150 = 15
local NOTE_41 = 16
local NOTE_160 = 18	--健康跑
local NOTE_170 = 17
local NOTE_42 = 19
local NOTE_43 = 20
local NOTE_44 = 21
local NOTE_45 = 22
local NOTE_46 = 23
local NOTE_47 = 24
local NOTE_48 = 25
local NOTE_49 = 26
local NOTE_51 = 27
local NOTE_180 = 28	--副本
local NOTE_190 = 29	--潘神的化身
local NOTE_59 = 30
local NOTE_191 = 40	--道具调用脚本消息提示


-- 技能书配置表
local skill_msg = 
{
        ["3875"]=SM_875,  
        ["5001"]=SM_001,
        ["5002"]=SM_002,
        ["5005"]=SM_003,
        ["5006"]=SM_004,
        ["5009"]=SM_005,
        ["5010"]=SM_006,
        ["5013"]=SM_007,
        ["5014"]=SM_008,
        ["5017"]=SM_009,
        ["5018"]=SM_010,
        ["5021"]=SM_011,
        ["5022"]=SM_012,
        ["5025"]=SM_013,
        ["5026"]=SM_014,
        ["5030"]=SM_015,
        ["5031"]=SM_016,
        ["5035"]=SM_017,
        ["5036"]=SM_018,
        ["5040"]=SM_019,
        ["5041"]=SM_020,
        ["5201"]=SM_021,
        ["5202"]=SM_022,
        ["5205"]=SM_023,
        ["5206"]=SM_024,
        ["5209"]=SM_025,
        ["5210"]=SM_026,
        ["5213"]=SM_027,
        ["5214"]=SM_028,
        ["5217"]=SM_029,
        ["5218"]=SM_030,
        ["5221"]=SM_031,
        ["5222"]=SM_032,
        ["5225"]=SM_033,
        ["5226"]=SM_034,
        ["5229"]=SM_035,
        ["5230"]=SM_036,
        ["5233"]=SM_037,
        ["5234"]=SM_038,
        ["5238"]=SM_039,
        ["5239"]=SM_040,
        ["5243"]=SM_041,
        ["5244"]=SM_042,
        ["5401"]=SM_043,
        ["5402"]=SM_044,
        ["5405"]=SM_045,
        ["5406"]=SM_046,
        ["5409"]=SM_047,
        ["5410"]=SM_048,
        ["5413"]=SM_049,
        ["5414"]=SM_050,
        ["5417"]=SM_051,
        ["5418"]=SM_052,
        ["5421"]=SM_053,
        ["5422"]=SM_054,
        ["5425"]=SM_055,
        ["5426"]=SM_056,
        ["5430"]=SM_057,
        ["5431"]=SM_058,
        ["5435"]=SM_059,
        ["5436"]=SM_060,
        ["5440"]=SM_061,
        ["5441"]=SM_062,
        ["5443"]=SM_063,
        ["5444"]=SM_064,
        ["5603"]=SM_065,
        ["5604"]=SM_066,
        ["5608"]=SM_067,
        ["5609"]=SM_068,
        ["5613"]=SM_069,
        ["5614"]=SM_070,
        ["5618"]=SM_071,
        ["5619"]=SM_072,
        ["5621"]=SM_073,
        ["5622"]=SM_074,
        ["5624"]=SM_075,
        ["5625"]=SM_076,
        ["5628"]=SM_077,
        ["5629"]=SM_078,
        ["5632"]=SM_079,
        ["5633"]=SM_080,
        ["5636"]=SM_081,
        ["5637"]=SM_082,
        ["5640"]=SM_083,
        ["5641"]=SM_084,
        ["5644"]=SM_085,
        ["5645"]=SM_086,
        ["5648"]=SM_087,
        ["5649"]=SM_088,
	["1000"]=SM_100,
	["1001"]=SM_101,
	["2000"]=SM_102,
	["2001"]=SM_103,
	["1003"]=SM_091,--YD雅典玩家
	["1004"]=SM_092--SBD
        
};

--战场
local Battlefield_msg = 
{

	["100"]=SM_089,
	["1001"]=SM_091,--YD雅典玩家
	["1002"]=SM_092,--SBD
	["1003"]=SM_093,
	["1004"]=SM_094

};

--公会保卫活动
local Association_msg = 
{

	["1000"]=SM_102,
	["1001"]=SM_103,
	["1002"]=SM_104,
	["1003"]=SM_105,
	["1004"]=SM_106,
	["1005"]=SM_107,
	["1006"]=SM_108,
	["1007"]=SM_109,
	["1008"]=SM_110,
	["1009"]=SM_111
};

--公会保卫活动2杀怪公告积分
local Association_msg2 = 
{

	["1"] = SM_121,
	["2"] = SM_122,
	["3"] = SM_123,
	["4"] = SM_124,
	["5"] = SM_125,
	["11"] = SM_127,
	["12"] = SM_128,
	["13"] = SM_129,
	["14"] = SM_130,
	["15"] = SM_131,
	["32"] = SM_132,
	["33"] = SM_133,
	["34"] = SM_134,
	["35"] = SM_135,
	["36"] = SM_136,
	["37"] = SM_137,
	["38"] = SM_138
};

--公会保卫活动3boss
local Association_msg3 = 
{
	["1"]=SM_126

};

--公会活动
local Union_msg = 
{

	["1"]=SM_201,--YD
	["2"]=SM_202,--SBD
	["3"]=SM_203

};

--新手活动
local New_msg = 
{

	["101"]=SM_2000,
	["102"]=SM_2001,
	["103"]=SM_2002,
        ["0"]=SM_2003,
        ["9"]=SM_2004,
        ["49"]=SM_2005,
        ["99"]=SM_2006,
        ["199"]=SM_2007,
        ["299"]=SM_2008,
        ["399"]=SM_2009,
        ["499"]=SM_2010,
        ["599"]=SM_2011,
        ["699"]=SM_2012,
	["799"]=SM_2013,
	["899"]=SM_2014,
	["999"]=SM_2015,
	["1099"]=SM_2016,
	["1199"]=SM_2017


};


--新手活动1
local Newone_msg = 
{

	["104"]=SM_2018

};


--新手活动2
local Newtwo_msg = 
{

	["105"]=SM_2019,
	["106"]=SM_2020,
        ["0"]=SM_2003,
        ["9"]=SM_2004,
        ["49"]=SM_2005,
        ["99"]=SM_2006,
        ["199"]=SM_2007,
        ["299"]=SM_2008,
        ["399"]=SM_2009,
        ["499"]=SM_2010,
        ["599"]=SM_2011,
        ["699"]=SM_2012,
	["799"]=SM_2013,
	["899"]=SM_2014,
	["999"]=SM_2015,
	["1099"]=SM_2016,
	["1199"]=SM_2017

};

--健康跑
local Health_msg =
{
	["1"] = SM_001,
	["2"] = SM_002,
	["3"] = SM_003,
	["4"] = SM_004,
	["5"] = SM_005,
	["6"] = SM_006,
	["7"] = SM_007,
	["8"] = SM_008,
	["9"] = SM_009,
	["10"] = SM_010,
	["100"] = SM_312,
	["1000"] = SM_300,
	["1001"] = SM_301,
	["1002"] = SM_302,
	["1003"] = SM_303,
	["1004"] = SM_304,
	["1005"] = SM_305,
	["1006"] = SM_306,
	["1007"] = SM_307,
	["1008"] = SM_308,
	["1009"] = SM_309,
	["1010"] = SM_310,
	["1011"] = SM_311,
	["1012"] = SM_312,
	["1013"] = SM_313,
	["1014"] = SM_314,
	["1015"] = SM_315,
	["1016"] = SM_316,
	["1017"] = SM_317,
	["1018"] = SM_318,
	["1019"] = SM_319

}
--战场奖励
local Tige_msg = 
{
	["3102"]=SM_3102,
	["3103"]=SM_3103

};
--战场奖励
local Tiges_msg = 
{
	["1000"]=SM_3111,
	["1001"]=SM_3112,
	["1002"]=SM_3113,
	["1003"]=SM_3114,
	["2001"]=SM_3115,
	["2002"]=SM_3116,
	["4004"]=SM_4004,
	["4005"]=SM_4005,
	["4006"]=SM_4006,
	["4007"]=SM_4007,
	["4008"]=SM_4008,
	["4009"]=SM_4009,
	["4010"]=SM_4010,
	["5001"]=SM_3115,
	["5002"]=SM_5002,
	["5003"]=SM_5003,

};
--1
local Newthree_msg = 
{

	["107"]=SM_2021

};


--2
local Newfour_msg = 
{

	["108"]=SM_2022

};


--3
local Newfive_msg = 
{

	["109"]=SM_2023

};

--4
local Newsix_msg = 
{

	["110"]=SM_2024

};


--5
local Newseven_msg = 
{

	["111"]=SM_2025

};


--6
local Neweight_msg = 
{

	["112"]=SM_2026

};


--7
local Newnight_msg = 
{

	["113"]=SM_2027

};

--zeus
local Zeus_msg = 
{

	["1230"]=ZEUS_1230,
	["1231"]=ZEUS_1231,
	["1232"]=ZEUS_1232,
	["1233"]=ZEUS_1233,
	["1234"]=ZEUS_1234,
	["1235"]=ZEUS_1235,
	["1236"]=ZEUS_1236

};

--副本
local FuBen_msg = 
{

	["1"]=FB_0001,
	["2"]=FB_0002,
	["3"]=FB_0003,
	["4"]=FB_0004,
	["5"]=FB_0005,
        ["6"]=FB_0006
};
--道具调用技能
local DaoJu_msg = 
{

	["50"]=DJ_0001,
	["51"]=DJ_0002,
	["52"]=DJ_0003,
	["53"]=DJ_0004,
	["54"]=DJ_0005

};

-- 潘神的化身
local pan_msg = 
{

        ["3820"]=XD_001,
        ["3821"]=XD_002,
        ["3822"]=XD_003,
        ["3823"]=XD_004,
        ["3824"]=XD_005,
        ["3825"]=XD_006,
        ["4262"]=XD_007,
        ["4263"]=XD_008,
        ["4264"]=XD_009,
        ["4265"]=XD_010,
        ["4222"]=XD_011,
        ["4212"]=XD_012,
        ["4202"]=XD_013,
        ["4252"]=XD_014,
        ["9961"]=XD_015,
        ["4182"]=XD_016,
        ["4062"]=XD_017,
        ["4061"]=XD_018,
        ["10106"]=XD_019,
	["4109"]=XD_020,
	["9991"]=XD_021,
	["1"]=XD_067,		--YD雅典玩家
	["0"]=XD_068,		--SBD
	["1000"]=XD_069
        
};


function Broadcastmsg(notetype,channel,name,note)

	strText = "";
	if notetype == NOTE_1  then--技能书
		msg_table = Split(note,"#");
		strText ="|cff76EE00"..skill_msg[msg_table[0]]..name..skill_msg[msg_table[1]]..skill_msg[msg_table[2]].."|cFFFFFFFF";
        elseif notetype == NOTE_10  then--战场
		msg_table = Split(note,"#");
		strText = Battlefield_msg[msg_table[0]] .. name .. Battlefield_msg[msg_table[1]];

        elseif notetype == NOTE_20  then----跨级任务
		rank = note+10;

		if rank == 130 then 
			strText = "\""..name.."\""..SM_L0_04;	
		else 
			if rank > 120 then
				rank = 120;
			end
			strText = "\""..name.."\""..SM_L0_01 .. " "..note.." "..SM_L0_02.." "..rank.." "..SM_L0_03;		
		end
        elseif notetype == NOTE_30  then--公会活动
		msg_table = Split(note,"#");
		strText =  Union_msg[msg_table[0]] .. name .. Union_msg[msg_table[1]];

        elseif notetype == NOTE_40  then--新手活动
		msg_table = Split(note,"#");
		strText = New_msg[msg_table[0]] .. name .. New_msg[msg_table[1]] .. New_msg[msg_table[2]] .. New_msg[msg_table[3]];

        elseif notetype == NOTE_50  then--新手活动1
		msg_table = Split(note,"#");
		strText = Newone_msg[msg_table[0]];

        elseif notetype == NOTE_60  then--新手活动2
		msg_table = Split(note,"#");
		strText = Newtwo_msg[msg_table[0]] .. Newtwo_msg[msg_table[1]] .. Newtwo_msg[msg_table[2]];
                
	elseif notetype == NOTE_70  then--新手活动2
		msg_table = Split(note,"#");
		strText = Association_msg[msg_table[0]] .. name .. Association_msg[msg_table[1]];

        elseif notetype == NOTE_80  then--1
		msg_table = Split(note,"#");
		strText = Newthree_msg[msg_table[0]];

        elseif notetype == NOTE_90  then--2
		msg_table = Split(note,"#");
		strText = Newfour_msg[msg_table[0]];

        elseif notetype == NOTE_100  then--3
		msg_table = Split(note,"#");
		strText = Newfive_msg[msg_table[0]];

        elseif notetype == NOTE_110  then--4
		msg_table = Split(note,"#");
		strText = Newsix_msg[msg_table[0]];

        elseif notetype == NOTE_120  then--5
		msg_table = Split(note,"#");
		strText = Newseven_msg[msg_table[0]];

        elseif notetype == NOTE_130  then--6
		msg_table = Split(note,"#");
		strText = Neweight_msg[msg_table[0]];
	
	elseif notetype == NOTE_140  then--公会活动2
		msg_table = Split(note,"#");
		strText = name .. Association_msg2[msg_table[0]] .. name .. SM_132 .. msg_table[1] .. SM_133 .. msg_table[2];

	elseif notetype == NOTE_150  then--公会活动3
		msg_table = Split(note,"#");
		strText = Association_msg2[msg_table[0]] .. Association_msg2[msg_table[1]];

        elseif notetype == NOTE_41  then--7
		msg_table = Split(note,"#");
		strText = Newnight_msg[msg_table[0]];

	elseif notetype == NOTE_160  then--健康跑
		msg_table = Split(note,"#");
		strText =SM_300 .. name .. SM_310 ..msg_table[0] .. SM_314;

	elseif notetype == NOTE_170  then
		msg_table = Split(note,"#");
		strText =Health_msg[msg_table[2]] .. name .. SM_306 .. msg_table[3] .. Health_msg[msg_table[1]];

        elseif notetype == NOTE_42  then
		msg_table = Split(note,"#");
		strText = name .. SM_310 .. msg_table[1] .. SM_303 .. msg_table[0] .. SM_304;

        elseif notetype == NOTE_43  then
		msg_table = Split(note,"#");
		strText = Health_msg[msg_table[0]];

        elseif notetype == NOTE_44  then
		msg_table = Split(note,"#");
		strText = Association_msg2[msg_table[0]] .. SM_132 .. msg_table[1] .. SM_132 .. msg_table[2];

        elseif notetype == NOTE_45  then
		msg_table = Split(note,"#");
		strText = Association_msg2[msg_table[0]] .. Association_msg2[msg_table[1]];
        elseif notetype == NOTE_46  then
		msg_table = Split(note,"#");
		strText = name .. NF_L0_Z1519;
        elseif notetype == NOTE_47  then
		msg_table = Split(note,"#");
		strText = name .. NF_L0_Z1520 .. Zeus_msg[msg_table[0]];
        elseif notetype == NOTE_48  then
		msg_table = Split(note,"#");
		strText = name..Tige_msg[msg_table[0]];
        elseif notetype == NOTE_49  then
		msg_table = Split(note,"#");
		strText = Tiges_msg[msg_table[0]]..name..Tiges_msg[msg_table[1]];
        elseif notetype == NOTE_59  then
		msg_table = Split(note,"#");
		strText = Tiges_msg[msg_table[0]]..name..Tiges_msg[msg_table[1]]..Tiges_msg[msg_table[2]];
        elseif notetype == NOTE_51  then--战场
		msg_table = Split(note,"#");
		strText = Battlefield_msg[msg_table[0]];
        elseif notetype == NOTE_180  then
		msg_table = Split(note,"#");
		strText = name .. FuBen_msg[msg_table[0]];
        elseif notetype == NOTE_190  then
		msg_table = Split(note,"#");
		strText = pan_msg[msg_table[0]] .. name .. pan_msg[msg_table[1]] .. pan_msg[msg_table[2]];
        elseif notetype == NOTE_180  then
		msg_table = Split(note,"#");
		strText = name .. FuBen_msg[msg_table[0]];
        elseif notetype == NOTE_191  then
		msg_table = Split(note,"#");
		strText = DaoJu_msg[msg_table[0]];

	end;

	
	if channel == CHANNEL_MIDDLE  then
		GameAPI:AddProclaimMessage_UTF8(strText,1);
        elseif channel == CHANNEL_PRESONAL  then
		GameAPI:AddPersonalMessage_UTF8(strText,6,1);
        elseif channel == CHANNEL_MOVE  then
       		DynamicText_AddMsg(strText);
    	end;

end

function Split(szFullString, szSeparator)
	local nFindStartIndex = 1;
	local nSplitIndex = 0;
	local nSplitArray = {};
	while true do
   		local nFindLastIndex = string.find(szFullString, szSeparator, nFindStartIndex);
   		if not nFindLastIndex then
    			nSplitArray[nSplitIndex] = string.sub(szFullString, nFindStartIndex, string.len(szFullString));
    			break;
   		end;
   		nSplitArray[nSplitIndex] = string.sub(szFullString, nFindStartIndex, nFindLastIndex - 1);
   		nFindStartIndex = nFindLastIndex + string.len(szSeparator);
   		nSplitIndex = nSplitIndex + 1;
	end;
	return nSplitArray;
end
