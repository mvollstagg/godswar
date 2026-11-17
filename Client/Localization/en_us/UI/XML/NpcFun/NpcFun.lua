
--定义拓展的NPC功能类型
--NPC_FLAG_SYS
NPC_FLAG_SYS_NONE    = 0
NPC_FLAG_SYS_TRANMIT = 1  --传送
NPC_FLAG_SYS_CREDIT_EXCHANGE = 2 --声望
NPC_FLAG_SYS_WAR   = 3 --战场活动
NPC_FLAG_SYS_BREAK  = 4  --装备分解
NPC_FLAG_SYS_ALTAR = 5  --祭坛
NPC_FLAG_GUILDQUEST = 6  --公会任务
NPC_FLAG_ACTIVITY = 7   --物品兑换
NPC_FLAG_SYS_UNIONTIME = 8--公会福利
NPC_FLAG_SYS_REPETITION = 9---副本
NPC_FLAG_SYS_PAN = 10----潘神
NPC_FLAG_SYS_MESSENGER = 11----潘神使者
NPC_FLAG_SYS_DESIDENTIFY = 12 --称谓鉴定
NPC_FLAG_SYS_DESAWARD = 13	--称谓奖励
NPC_FLAG_SYS_AWARD = 14----公会奖励
NPC_FLAG_SYS_SIGNACT = 15 ----令牌活动
NPC_FLAG_SYS_SKILLBOOK = 16 ----抽取技能书
NPC_FLAG_SYS_MATERIALBACK = 17 ----物资回收官,20090326,ver204,li
NPC_FLAG_SYS_NEWMAN = 18 ----新手指引
NPC_FLAG_SYS_STAR = 19-----星座幸运日
NPC_FLAG_SYS_UNIONWAR = 20 ----公会活动
NPC_FLAG_SYS_ASSOCIATION = 21---公会保卫活动
NPC_FLAG_SYS_HEALTH = 22----健康跑
NPC_FLAG_SYS_OLDMAN = 23----神秘老人
NPC_FLAG_SYS_LOSTBOOK = 24----失落之书
NPC_FLAG_SYS_REMAIN = 25----上古遗迹
NPC_FLAG_SYS_ZEUS = 26---宙斯献礼
NPC_FLAG_SYS_WARTXT   = 27--战场说明
NPC_FLAG_SYS_GIFT = 28--节日礼物
NPC_FLAG_SYS_SANLOAD = 29--圣衣铸造
NPC_FLAG_SYS_EMENT = 30--圣石工艺
NPC_FLAG_SYS_PET = 31---宠物
NPC_FLAG_SYS_HPMP = 32--回复HPMP
NPC_FLAG_SYS_FOOD = 33--美味食谱
NPC_FLAG_SYS_MOON = 34--月神祭祀
NPC_FLAG_SYS_MOONCAKE = 35--食谱换月饼
NPC_FLAG_SYS_PETT = 36--宠物洗点
NPC_FLAG_SYS_VOCATION = 37--职业套装
NPC_FLAG_SYS_HALLO = 38--万圣节活动
NPC_FLAG_SYS_THANK = 39--感恩节


--获取所有UI资源
local win = UIAPI:GetElement("FirstWin");
FirstWin_Text1= win:GetChild("FirstWin_Text1");
FirstWin_Text2= win:GetChild("FirstWin_Text2");
FirstWin_Text3= win:GetChild("FirstWin_Text3");

FirstWin_Button1 = win:GetChild("FirstWin_Button1");
FirstWin_Button2 = win:GetChild("FirstWin_Button2");
FirstWin_Button3 = win:GetChild("FirstWin_Button3");
FirstWin_Button4 = win:GetChild("FirstWin_Button4");
FirstWin_Button5 = win:GetChild("FirstWin_Button5");
FirstWin_Button6 = win:GetChild("FirstWin_Button6");
FirstWin_Button7 = win:GetChild("FirstWin_Button7");
FirstWin_Button8 = win:GetChild("FirstWin_Button8");
FirstWin_Button9 = win:GetChild("FirstWin_Button9");
FirstWin_Button10 = win:GetChild("FirstWin_Button10");
FirstWin_Button11 = win:GetChild("FirstWin_Button11");
FirstWin_Button12 = win:GetChild("FirstWin_Button12");

 FirstWin_ItemBtn1 = win:GetChild("FirstWin_ItemBtn1");
 FirstWin_ItemBtn2 = win:GetChild("FirstWin_ItemBtn2");
 FirstWin_ItemBtn3 = win:GetChild("FirstWin_ItemBtn3");
 FirstWin_ItemBtn4 = win:GetChild("FirstWin_ItemBtn4");

 FirstWin_CheckBox1 = win:GetChild("FirstWin_CheckBox1");
 FirstWin_CheckBox2 = win:GetChild("FirstWin_CheckBox2");
 FirstWin_CheckBox3 = win:GetChild("FirstWin_CheckBox3");
 FirstWin_CheckBox4 = win:GetChild("FirstWin_CheckBox4");

 FirstWin_ButtonA1 = win:GetChild("FirstWin_ButtonA1");
 FirstWin_ButtonA2 = win:GetChild("FirstWin_ButtonA2");
 FirstWin_ButtonA3 = win:GetChild("FirstWin_ButtonA3");

 InputText1 = win:GetChild("BijouEditBox");
 InputText2 = win:GetChild("MoneyEditBox");
 InputText3 = win:GetChild("NumEditBox");

 Input11 = win:GetChild("BijouEditMid");--外框架
 Input12 = win:GetChild("BijouEditLeft");
 Input13 = win:GetChild("BijouEditRight");

 Input21 = win:GetChild("MoneyEditMid");--外框架
 Input22 = win:GetChild("MoneyEditLeft");
 Input23 = win:GetChild("MoneyEditRight");

 Input31 = win:GetChild("NumEditMid");--外框架
 Input32 = win:GetChild("NumEditLeft");
 Input33 = win:GetChild("NumEditRight");

 strDesig = ""


--配置UI窗体信息生成自己想要的界面
function Set_NpcFun_Text(Type,Index,BtnID,SubID)
	if Type == NPC_FLAG_SYS_TRANMIT then    --------传送----------------------------------------------------------------------------------
		NpcFunTranmit_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_CREDIT_EXCHANGE then    --------兑换-----------------------------------------------------------------------
		NpcFunCredit_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_WAR then    --------战场活动----------------------------------------------------------------------
		NpcFunWar_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_BREAK then    --------装备分解-------------------------------------------------------------------
		NpcFunBreak_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_ALTAR then    --------------祭坛-----------------------------------------------------------------------------
		NpcFunAltar_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_GUILDQUEST then	--------公会任务-----------------------------------------------------------------------
		NpcFunGuildQuest_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_ACTIVITY then	--------活动------------------------
		NpcFunActivity_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_UNIONTIME then	--------公会福利-----------------------------------------------------------------------
		NpcFunUionTime_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_REPETITION then	--------副本----------------
		NpcFunRepetition_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_PAN then    --------潘神-------------------------------------------------------------------
		NpcFunPan_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_MESSENGER then	--------潘神的邀请函-----------------------------------------------------------------------
		NpcFunMessenger_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_DESIDENTIFY then	--------称谓鉴定-----------------------------------------------------------------------
		NpcFunDesidentify_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_DESAWARD then	--------称谓奖励-----------------------------------------------------------------------
		NpcFunDesaward_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_AWARD then    --------------供奉奖励-----------------------------------------------------------------------------
		NpcFunAward_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_SIGNACT then	--------令牌活动-----------------------------------------------------------------------
		NpcFunSignact_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_SKILLBOOK then    --------------抽取技能书-----------------------------------------------------------------------------
		NpcFunSkillbook_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_MATERIALBACK then		---------物资回收官-----------20090326,ver204,li
		NpcFunMaterialBack_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_NEWMAN then    --------新手-------------------------------------------------------------------
		NpcFunNewMan_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_STAR then	--------星座幸运日-----------------------------------------------------------------------
		NpcFunStar_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_UNIONWAR then    --------公会活动-------------------------------------------------------------------
		NpcFunUnionWar_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_ASSOCIATION then		---------公会保卫活动-----------
		NpcFunAssociation_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_HEALTH then		---------健康跑-----------
		NpcFunHealth_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_OLDMAN then	--------神秘老人-----------------------------------------------------------------------
		NpcFunOldMan_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_LOSTBOOK then	-------失落之书兑换奖励-----------------------------------------------------------------------
		NpcFunLostBook_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_REMAIN then	--------上古遗迹-----------------------------------------------------------------------
		NpcFunRemain_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_ZEUS then	--------宙斯献礼---------------------------------------------
		NpcFunZeus_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_WARTXT then    --------战场提示----------------------------------------------------------------------
		NpcFunWarTxt_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_GIFT then    --------节日礼物-------------------------------------------------------------------
		NpcFunGift_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_SANLOAD then    --------圣装打造-------------------------------------------------------------------
		NpcFunSanLoad_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_EMENT then		---------装备开孔-----------
		NpcFunEment_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_PET then    -------宠物管理员-------------------------------------------------------------------
		NpcFunPet_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_SOUND then    -------漫步者-------------------------------------------------------------------
		NpcFunSound_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_HPMP then    -------hpmp回复-------------------------------------------------------------------
		NpcFunHpmp_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_FOOD then --------美味食谱--------
		NpcFunFood_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_MOON then --------月神祭祀--------
		NpcFunMoon_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_MOONCAKE then    -------食谱兑换月饼-------------------------------------------------------------------
		NpcFunMooncake_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_PETT then    -------宠物洗点-------------------------------------------------------------------
		NpcFunPett_SetText(Type,Index,BtnID,SubID)
	elseif Type == NPC_FLAG_SYS_VOCATION then    -------职业套装-------------------------------------------------------------------
		NpcFunVocation_SetText(Type,Index,BtnID,SubID)

	elseif Type == NPC_FLAG_SYS_HALLO then    -------万圣节-------------------------------------------------------------------
		NpcFunHallo_SetText(Type,Index,BtnID,SubID)
	elseif Type == NPC_FLAG_SYS_THANK then    -------万圣节-------------------------------------------------------------------
		NpcFunThank_SetText(Type,Index,BtnID,SubID)
	end

end


--客户端消息提示框--------------------------------------------------------------------------------------------
function Set_MessageBox(Type,Index,PreSubID,SubID)
	Set_NpcFun_UI(Type,Index);

	if Type == NPC_FLAG_SYS_TRANMIT then    --------传送提示框----------------------------------------------------------------------------------
		NpcFunTranmit_SetMsg(Type,Index,PreSubID,SubID)

	elseif Type == NPC_FLAG_SYS_CREDIT_EXCHANGE then    --------兑换提示框-----------------------------------------------------------------------
		NpcFunCredit_SetMsg(Type,Index,PreSubID,SubID)

	elseif Type == NPC_FLAG_SYS_REPETITION  then  -------------------副本剧情提示框--------------------------------------------------------------
		 NpcFunRepetition_SetMsg(Type,Index,PreSubID,SubID)

	elseif Type == NPC_FLAG_SYS_NEWMAN then    -------------新手----------------------------------------------------------------------------
		NpcFunNewMan_SetMsg(Type,Index,PreSubID,SubID)

	elseif Type == NPC_FLAG_SYS_BREAK then    -------------装备分解客户端提示----------------------------------------------------------------------------
		NpcFunBreak_SetMsg(Type,Index,PreSubID,SubID)

        elseif Type == NPC_FLAG_SYS_EMENT then-------------圣装打孔------------------
		NpcFunEment_SetMsg(Type,Index,PreSubID,SubID)

	elseif Type == NPC_FLAG_SYS_PET  then  -------------------宠物客户端--------------------------------------------------------------
		NpcFunPet_SetMsg(Type,Index,PreSubID,SubID)

	elseif Type == NPC_FLAG_SYS_ALTAR then    --------------祭坛-----------------------------------------------------------------------------
		NpcFunAltar_SetMsg(Type,Index,PreSubID,SubID)

	elseif Type == NPC_FLAG_GUILDQUEST then  -------------------公会任务提示框--------------------------------------------------------------
		 NpcFunGuildQuest_SetMsg(Type,Index,PreSubID,SubID)
	end;
end
--显示不同类型的窗体界面
function Set_NpcFun_UI(Type,Index)
        print("Set_NpcFun_UI()");
        print(Type);
	SetOriginalPosition();
	NpcFun_Clear()

	strDesig = ""
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	win:Visible(true);


    	if Type == NPC_FLAG_SYS_STAR then
	        FirstWin_Text1:SetPosition(45,100);
	        FirstWin_Text2:SetPosition(45,120);
	        FirstWin_Text3:SetPosition(45,160);

    	elseif Type == NPC_FLAG_SYS_ALTAR then
                if Index == 1 then
	           FirstWin_Text1:SetPosition(45,100);
	           FirstWin_Text2:SetPosition(45,120);
	           FirstWin_Text3:SetPosition(45,140);

		   NpcFun_SetButtonPosition(45,160)
		end;
                if Index == 2 then
	           FirstWin_Text1:SetPosition(45,100);
	           FirstWin_Text2:SetPosition(45,140);
	           FirstWin_Text3:SetPosition(45,160);

		   NpcFun_SetButtonPosition(40,160)
                end;
                if Index == 3 then
	           FirstWin_Text1:SetPosition(45,100);
	           FirstWin_Text2:SetPosition(45,170);
	           FirstWin_Text3:SetPosition(45,190);

		   NpcFun_SetButtonPosition(25,210)
                end;
		if Index == 4 then
	           FirstWin_Text1:SetPosition(45,100);
	           FirstWin_Text2:SetPosition(45,120);
	           FirstWin_Text3:SetPosition(45,140);

   		   NpcFun_SetButtonPosition(45,160)
                end;
 		if Index == 5 then
	           FirstWin_Text1:SetPosition(45,100);
	           FirstWin_Text2:SetPosition(45,120);
	           FirstWin_Text3:SetPosition(45,140);

   		   NpcFun_SetButtonPosition(45,160)

	           FirstWin_ButtonA1:SetPosition(220,500);
	           FirstWin_ButtonA2:SetPosition(220,500);
                end;
    	elseif Type == NPC_FLAG_SYS_AWARD then
                if Index == 1 then
	           FirstWin_Text1:SetPosition(45,100);
	           FirstWin_Text2:SetPosition(45,120);
	           FirstWin_Text3:SetPosition(45,140);

		   NpcFun_SetButtonPosition(45,160)
                end;
                if Index == 2 then
	           FirstWin_Text1:SetPosition(45,100);
	           FirstWin_Text2:SetPosition(45,140);
	           FirstWin_Text3:SetPosition(45,160);

   		   NpcFun_SetButtonPosition(40,160)
                end;
                if Index == 3 then
	           FirstWin_Text1:SetPosition(45,100);
	           FirstWin_Text2:SetPosition(45,170);
	           FirstWin_Text3:SetPosition(45,190);

  	           NpcFun_SetButtonPosition(25,210);
                end;
		if Index == 4 then
	           FirstWin_Text1:SetPosition(45,100);
	           FirstWin_Text2:SetPosition(45,120);
	           FirstWin_Text3:SetPosition(45,140);

		   NpcFun_SetButtonPosition(45,160);
                end;
 		if Index == 5 then
	           FirstWin_Text1:SetPosition(45,100);
	           FirstWin_Text2:SetPosition(45,120);
	           FirstWin_Text3:SetPosition(45,140);

		   NpcFun_SetButtonPosition(45,160);

	           FirstWin_ButtonA1:SetPosition(220,500);
	           FirstWin_ButtonA2:SetPosition(220,500);

                end;

    	elseif Type == NPC_FLAG_SYS_BREAK then
                if Index== 3 then
                   zb = 0
                end;
    	elseif Type == NPC_FLAG_SYS_DESIDENTIFY then
	        FirstWin_Text1:SetPosition(45,100);
	        FirstWin_Text2:SetPosition(45,120);
	        FirstWin_Text3:SetPosition(45,160);

		NpcFun_SetButtonPosition(35,160);

    	elseif Type == NPC_FLAG_SYS_DESAWARD then
	        FirstWin_Text1:SetPosition(45,100);
	        FirstWin_Text2:SetPosition(45,120);
	        FirstWin_Text3:SetPosition(45,160);

  		NpcFun_SetButtonPosition(35,160);

    	elseif Type == NPC_FLAG_SYS_SIGNACT then
	        FirstWin_Text1:SetPosition(45,100);
	        FirstWin_Text2:SetPosition(45,120);
	        FirstWin_Text3:SetPosition(45,160);

    	end;
end

--UI都隐藏-------------------
function NpcFun_Clear()
	FirstWin_Text1:Visible(false);
	FirstWin_Text2:Visible(false);
	FirstWin_Text3:Visible(false);

	FirstWin_Button1:Visible(false);
	FirstWin_Button2:Visible(false);
	FirstWin_Button3:Visible(false);
	FirstWin_Button4:Visible(false);
	FirstWin_Button5:Visible(false);
	FirstWin_Button6:Visible(false);
	FirstWin_Button7:Visible(false);
	FirstWin_Button8:Visible(false);
	FirstWin_Button9:Visible(false);
	FirstWin_Button10:Visible(false);
	FirstWin_Button11:Visible(false);
	FirstWin_Button12:Visible(false);

	FirstWin_CheckBox1:Visible(false);
	FirstWin_CheckBox2:Visible(false);
	FirstWin_CheckBox3:Visible(false);
	FirstWin_CheckBox4:Visible(false);

        FirstWin_ItemBtn1:Visible(false);
	FirstWin_ItemBtn2:Visible(false);
        FirstWin_ItemBtn3:Visible(false);
	FirstWin_ItemBtn4:Visible(false);

	FirstWin_ButtonA1:Visible(false);
	FirstWin_ButtonA2:Visible(false);
	FirstWin_ButtonA3:Visible(false);

	InputText1:Visible(false);
	Input11:Visible(false);
	Input12:Visible(false);
	Input13:Visible(false);

	Input21:Visible(false);
	Input22:Visible(false);
	Input23:Visible(false);

	Input31:Visible(false);
	Input32:Visible(false);
	Input33:Visible(false);
end


--初始化UI的原始位置------------------------------------------------------------------------------------
function SetOriginalPosition()
	FirstWin_Text1:SetPosition(45,100);
	FirstWin_Text2:SetPosition(15,100);
	FirstWin_Text3:SetPosition(15,120);

	FirstWin_Button1:SetPosition(65,100);
        FirstWin_Button1:Enable(true);
	FirstWin_Button1:SetTextFormat(5);
	FirstWin_Button2:SetPosition(65,120);
        FirstWin_Button2:Enable(true);
	FirstWin_Button2:SetTextFormat(5);
	FirstWin_Button3:SetPosition(65,140);
        FirstWin_Button3:Enable(true);
	FirstWin_Button3:SetTextFormat(5);
	FirstWin_Button3:SetTextFormat(5);
	FirstWin_Button4:SetPosition(65,160);
        FirstWin_Button4:Enable(true);
	FirstWin_Button4:SetTextFormat(5);
	FirstWin_Button5:SetPosition(65,180);
        FirstWin_Button5:Enable(true);
	FirstWin_Button6:SetPosition(65,200);
        FirstWin_Button6:Enable(true);
	FirstWin_Button7:SetPosition(65,220);
        FirstWin_Button7:Enable(true);
	FirstWin_Button8:SetPosition(65,240);
        FirstWin_Button8:Enable(true);
	FirstWin_Button9:SetPosition(65,260);
        FirstWin_Button9:Enable(true);
	FirstWin_Button10:SetPosition(65,280);
        FirstWin_Button10:Enable(true);
	FirstWin_Button11:SetPosition(65,300);
        FirstWin_Button11:Enable(true);
	FirstWin_Button12:SetPosition(65,320);
        FirstWin_Button12:Enable(true);

	FirstWin_CheckBox1:SetPosition(45,220);
	FirstWin_CheckBox2:SetPosition(90,220);
	FirstWin_CheckBox3:SetPosition(135,220);
	FirstWin_CheckBox4:SetPosition(180,220);

        FirstWin_ItemBtn1:Visible(true);
	FirstWin_ItemBtn2:Visible(true);
        FirstWin_ItemBtn3:Visible(true);
	FirstWin_ItemBtn4:Visible(true);

	FirstWin_ButtonA1:SetPosition(175,500);
	FirstWin_ButtonA2:SetPosition(250,500);
	InputText1:SetText("");

end

function NpcFun_SetButtonPosition(firstX,firstY)
	FirstWin_Button1:SetPosition(firstX,firstY);
	FirstWin_Button2:SetPosition(firstX,firstY+25*1);
	FirstWin_Button3:SetPosition(firstX,firstY+25*2);
	FirstWin_Button4:SetPosition(firstX,firstY+25*3);
	FirstWin_Button5:SetPosition(firstX,firstY+25*4);
	FirstWin_Button6:SetPosition(firstX,firstY+25*5);
	FirstWin_Button7:SetPosition(firstX,firstY+25*6);
	FirstWin_Button8:SetPosition(firstX,firstY+25*7);
	FirstWin_Button9:SetPosition(firstX,firstY+25*8);
	FirstWin_Button10:SetPosition(firstX,firstY+25*9);
	FirstWin_Button11:SetPosition(firstX,firstY+25*10);
	FirstWin_Button12:SetPosition(firstX,firstY+25*11);

end
