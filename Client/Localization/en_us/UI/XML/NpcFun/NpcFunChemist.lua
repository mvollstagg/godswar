local win = UIAPI:GetElement("FirstWin");
--“©º¡ ¶
function NpcFunChemist_SetUI(Type,Index)
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	win:Visible(true);

end

function NpcFunChemist_SetText(Type,Index,BtnID,SubID)
	if Index == 1 then 
		if SubID == 100 then 
			FirstWin_Text1:SetText(NF_L0_CH100);
			FirstWin_Text1:Visible(true);
		elseif SubID == 101 then
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH101);
			Button:SetPosition(25,135)
			Button:Visible(true);
		elseif SubID == 102 then
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH102);
			Button:SetPosition(25,155)
			Button:Visible(true);
		elseif SubID == 103 then
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH103);
			Button:SetPosition(25,175)
			Button:Visible(true);
		elseif SubID == 104 then
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH104);
			Button:SetPosition(25,195)
			Button:Visible(true);
		end;
	elseif Index == 2 then
		if SubID == 200 then
			FirstWin_Text1:SetText(NF_L0_CH200);
			FirstWin_Text1:Visible(true);
		elseif SubID == 201 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH201);
			Button:SetPosition(25,135)
			Button:Visible(true);
		elseif SubID == 202 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH202);
			Button:SetPosition(25,155)
			Button:Visible(true);
		elseif SubID == 203 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH203);
			Button:SetPosition(25,175)
			Button:Visible(true);
		elseif SubID == 204 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH204);
			Button:SetPosition(25,195)
			Button:Visible(true);
		elseif SubID == 211 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH211);
			Button:SetPosition(25,135)
			Button:Visible(true);
		elseif SubID == 212 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH212);
			Button:SetPosition(25,155)
			Button:Visible(true);
		elseif SubID == 213 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH213);
			Button:SetPosition(25,175)
			Button:Visible(true);
		elseif SubID == 214 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH214);
			Button:SetPosition(25,195)
			Button:Visible(true);
		elseif SubID == 215 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH215);
			Button:SetPosition(25,215)
			Button:Visible(true);
		elseif SubID == 221 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH221);
			Button:SetPosition(25,135)
			Button:Visible(true);
		elseif SubID == 222 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH222);
			Button:SetPosition(25,155)
			Button:Visible(true);
		elseif SubID == 223 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH223);
			Button:SetPosition(25,175)
			Button:Visible(true);
		elseif SubID == 230 then
			FirstWin_Text1:SetText(NF_L0_CH230);
			FirstWin_Text1:Visible(true);
		elseif SubID == 231 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH231);
			Button:SetPosition(25,135)
			Button:Visible(true);
		elseif SubID == 232 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH232);
			Button:SetPosition(25,155)
			Button:Visible(true);
		elseif SubID == 233 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH233);
			Button:SetPosition(25,175)
			Button:Visible(true);
		elseif SubID == 234 then 
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_CH234);
			Button:SetPosition(25,195)
			Button:Visible(true);
		elseif SubID == 900 then
			FirstWin_Text1:SetText(NF_L0_CH900);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);		
		end;
	elseif Index == 3 then
		if SubID == 300 then
			FirstWin_Text1:SetText(NF_L0_CH300);
			FirstWin_Text1:Visible(true);
			FirstWin_ItemBtn1:Visible(true);
			FirstWin_ItemBtn1:SetPosition(25,135);
		elseif SubID == 301 then
			FirstWin_Text1:SetText(NF_L0_CH301);
			FirstWin_Text1:Visible(true);
			FirstWin_ItemBtn1:Visible(true);
			FirstWin_ItemBtn1:SetPosition(25,135);
		elseif SubID == 901 then
			FirstWin_Text1:SetText(NF_L0_CH900);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);		
		elseif SubID == 909 then
			FirstWin_Text1:SetText(NF_L0_CH909);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);		
		end;
	elseif SubID == 902 then
		FirstWin_Text1:SetText(NF_L0_CH902);
		FirstWin_Text1:Visible(true);
		NPCFUN:EndMessage(true);
	elseif SubID == 903 then
		FirstWin_Text1:SetText(NF_L0_CH903);
		FirstWin_Text1:Visible(true);
		NPCFUN:EndMessage(true);
	elseif SubID == 904 then
		FirstWin_Text1:SetText(NF_L0_CH904);
		FirstWin_Text1:Visible(true);
		NPCFUN:EndMessage(true);
	elseif SubID == 905 then
		FirstWin_Text1:SetText(NF_L0_CH905);
		FirstWin_Text1:Visible(true);
		NPCFUN:EndMessage(true);
	elseif SubID == 906 then
		FirstWin_Text1:SetText(NF_L0_CH906);
		FirstWin_Text1:Visible(true);
		NPCFUN:EndMessage(true);
	elseif SubID == 907 then
		FirstWin_Text1:SetText(NF_L0_CH907);
		FirstWin_Text1:Visible(true);
		NPCFUN:EndMessage(true);
	elseif SubID == 908 then
		FirstWin_Text1:SetText(NF_L0_CH908);
		FirstWin_Text1:Visible(true);
		NPCFUN:EndMessage(true);
	elseif SubID == 910 then
		FirstWin_Text1:SetText(NF_L0_CH910);
		FirstWin_Text1:Visible(true);
		NPCFUN:EndMessage(true);	
	end;
end 