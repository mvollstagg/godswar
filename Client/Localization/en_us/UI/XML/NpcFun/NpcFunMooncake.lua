local win = UIAPI:GetElement("FirstWin");
--活动
function NpcFunMooncake_SetUI(Type,Index)
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	win:Visible(true);

end

function NpcFunMooncake_SetText(Type,Index,BtnID,SubID)
	if Index== 1 then    ----第几个页面------
		FirstWin_Text1:SetText(NF_L0_M101);
		FirstWin_Text1:Visible(true);
		if SubID== 100      then
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetPosition(25,200);
			Button:SetText(NF_L0_M100);
			Button:Visible(true);

		end;
	elseif Index == 2 then	--服务器提示消息---------------------
		if SubID == 118 then
			FirstWin_Text1:SetText(NF_L0_M118);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 119 then
			FirstWin_Text1:SetText(NF_L0_S009);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 110 then
			FirstWin_Text1:SetText(NF_L0_M110);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 117 then
			FirstWin_Text1:SetText(NF_L0_M117);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);

		end;
	end;
end
