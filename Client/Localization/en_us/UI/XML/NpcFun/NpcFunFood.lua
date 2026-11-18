local win = UIAPI:GetElement("FirstWin");
--副本
function NpcFunFood_SetUI(Type,Index)
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	win:Visible(true);

end
function NpcFunFood_SetText(Type,Index,BtnID,SubID)
	if Index == 1 then---第一个页面
		if SubID == 1 then
			FirstWin_Text1:SetText(NF_L0_FOD101);
			FirstWin_Text1:Visible(true);
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetText(NF_L0_FOD1);
			Button:SetPosition(65,270)
			Button:Visible(true);	
		end;
	elseif Index == 2 then    ----第二个页面
		if math.mod(SubID ,100) == 2 then
			FirstWin_Text1:SetText(NF_L0_FOD102..( ( ( ( SubID - 2 ) / 100 ) - ( ( SubID - 2 ) / 100 ) % 60 ) / 60 )..NF_L0_FOD202..( ( ( SubID - 2 ) / 100 ) % 60 )..NF_L0_FOD302);
			FirstWin_Text1:Visible(true);
			FirstWin_Text1:SetPosition(45,100);
			FirstWin_ButtonA1:SetPosition(220,500);
			FirstWin_ButtonA1:Visible(true);
			FirstWin_ButtonA2:Visible(false);
			NPCFUN:EndMessage(true);
		elseif SubID == 100 then
			FirstWin_Text1:SetText(NF_L0_FOD100);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 200 then
			FirstWin_Text1:SetText(NF_L0_FOD200);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 300 then
			FirstWin_Text1:SetText(NF_L0_FOD300);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 400 then
			FirstWin_Text1:SetText(NF_L0_FOD400);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		end;
	end;
end