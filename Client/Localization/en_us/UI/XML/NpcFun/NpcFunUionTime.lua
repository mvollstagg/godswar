local win = UIAPI:GetElement("FirstWin");
--生活技能
function NpcFunUionTime_SetUI(Type,Index)
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	win:Visible(true);

end

function NpcFunUionTime_SetText(Type,Index,BtnID,SubID)
		if Index== 1 then    ----第几个页面------
			if SubID== 1  then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION1);
				Button:SetPosition(65,240)
				Button:Visible(true);
			elseif SubID == 2  then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION2);
				Button:Visible(true);
				Button:SetPosition(65,260)
			elseif SubID == 3  then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION3);
				Button:Visible(true);
				Button:SetPosition(65,280)
			elseif SubID == 4  then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION4);
				Button:SetPosition(65,300)
				Button:Visible(true);
			elseif SubID == 5  then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION5);
				Button:Visible(true);
				Button:SetPosition(65,320)
			elseif SubID ==6 then
	      			FirstWin_Text1:SetText(NF_L0_UNION6);
	      			FirstWin_Text1:Visible(true);
			elseif SubID ==10 then
	      			FirstWin_Text1:SetText(NF_L0_UNION10);
	      			FirstWin_Text1:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
			end;
		elseif Index == 2 then	--服务器提示消息---------------------
			if SubID == 11  then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION11);
				Button:SetPosition(65,210);
				Button:Visible(true);
			elseif SubID == 12 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION12);
				Button:SetPosition(65,230);
				Button:Visible(true);
			elseif SubID == 13 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION13);
				Button:SetPosition(65,250);
				Button:Visible(true);
			elseif SubID == 14 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION14);
				Button:SetPosition(65,270);
				Button:Visible(true);
			elseif SubID == 15 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION15);
				Button:SetPosition(65,210);
				Button:Visible(true);
			elseif SubID == 16 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION16);
				Button:SetPosition(65,230);
				Button:Visible(true);
			elseif SubID == 17 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION17);
				Button:SetPosition(65,250);
				Button:Visible(true);
			elseif SubID == 18 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION18);
				Button:SetPosition(65,270);
				Button:Visible(true);
			elseif SubID % 500==1 then
	      			FirstWin_Text1:SetText(NF_L0_UNION19..(SubID-1)/500);
	      			FirstWin_Text1:Visible(true);
			elseif SubID % 500==2 then
	      			FirstWin_Text1:SetText(NF_L0_UNION20..(SubID-2)/500);
	      			FirstWin_Text1:Visible(true);
			elseif SubID % 500==3 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION21..(SubID-3)/500);
				Button:SetPosition(25,120);
				Button:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
				Button:Enable(false);
				UIAPI:SetChecked(false,Button);
			elseif SubID % 500==4 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION22..(SubID-4)/500);
				Button:SetPosition(25,140);
				Button:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
				Button:Enable(false);
				UIAPI:SetChecked(false,Button);
			elseif SubID % 500==6 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION320..(SubID-6)/500);
				Button:SetPosition(65,210);
				Button:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
				Button:Enable(false);
				UIAPI:SetChecked(false,Button);
			elseif SubID % 500==7 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION330..(SubID-7)/500);
				Button:SetPosition(65,230);
				Button:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
				Button:Enable(false);
				UIAPI:SetChecked(false,Button);
			elseif SubID % 500==8 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION320..(SubID-8)/500);
				Button:SetPosition(65,210);
				Button:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
				Button:Enable(false);
				UIAPI:SetChecked(false,Button);
			elseif SubID % 500==9 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION330..(SubID-9)/500);
				Button:SetPosition(65,230);
				Button:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
				Button:Enable(false);
				UIAPI:SetChecked(false,Button);
			elseif SubID % 500==23 then
	      			FirstWin_Text1:SetText(NF_L0_UNION400..(SubID-23)/500-1);
	      			FirstWin_Text1:Visible(true);
			elseif SubID % 500==24 then
	      			FirstWin_Text1:SetText(NF_L0_UNION410..(SubID-24)/500-1);
	      			FirstWin_Text1:Visible(true);
			elseif SubID ==240 then
	      			FirstWin_Text1:SetText(NF_L0_UNION240);
	      			FirstWin_Text1:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
			elseif SubID ==241 then
	      			FirstWin_Text1:SetText(NF_L0_UNION241);
	      			FirstWin_Text1:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
			elseif SubID ==300 then
	      			FirstWin_Text1:SetText(NF_L0_UNION300);
	      			FirstWin_Text1:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
			elseif SubID ==310 then
	      			FirstWin_Text1:SetText(NF_L0_UNION310);
	      			FirstWin_Text1:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
			elseif SubID ==245 then
	      			FirstWin_Text1:SetText(NF_L0_UNION245);
	      			FirstWin_Text1:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
			end;
		elseif Index == 3 then	--服务器提示消息---------------------
			if SubID == 200  then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION200);
				Button:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
			elseif SubID == 1000 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION1000);
				Button:Visible(true);
			elseif SubID % 500==5 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_UNION102..(SubID-5)/500);
				Button:Visible(true);
			elseif SubID ==310 then
	      			FirstWin_Text1:SetText(NF_L0_UNION310);
	      			FirstWin_Text1:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
			elseif SubID ==350 then
	      			FirstWin_Text1:SetText(NF_L0_UNION350);
	      			FirstWin_Text1:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
			elseif SubID ==351 then
	      			FirstWin_Text1:SetText(NF_L0_UNION351);
	      			FirstWin_Text1:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
			elseif SubID ==352 then
	      			FirstWin_Text1:SetText(NF_L0_UNION352);
	      			FirstWin_Text1:Visible(true);
	      			NPCFUN:EndMessage(true);
	      			NPCFUN:NeedMessage(False);
			end;
		end;
end