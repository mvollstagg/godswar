local win = UIAPI:GetElement("FirstWin");
--活动
function NpcFunHorseFeeder_SetUI(Type,Index)
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	win:Visible(true);

end

function NpcFunHorseFeeder_SetText(Type,Index,BtnID,SubID)
	if Index== 1 then    ----第几个页面------
		FirstWin_Text1:SetText(LuaText["NF_HF_T1"]);
		FirstWin_Text1:Visible(true);
		--NPCFUN:EndMessage(true);
		if SubID >= 100 and SubID <= 102 then
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetPosition(25,200 + (SubID - 100) * 20);
			Button:SetText(LuaText["NF_HF_B" .. SubID]);
			Button:Visible(true);
		end;
	elseif Index == 2 then	--
		if SubID == 201 then
			FirstWin_Text1:SetText(LuaText["NF_HF_T" .. SubID]);
			FirstWin_Text1:Visible(true);
           		FirstWin_ItemBtn1:Visible(true);
            		FirstWin_ItemBtn1:SetPosition(50,95);
		elseif SubID == 200 then
			FirstWin_Text1:SetText(LuaText["NF_HF_T" .. SubID]);
			FirstWin_Text1:Visible(true);
			FirstWin_Text1:SetPosition(25,20);
           		FirstWin_ItemBtn1:Visible(true);
            		FirstWin_ItemBtn1:SetPosition(50,65);
		elseif SubID == 202 then
			FirstWin_Text1:SetText(LuaText["NF_HF_T" .. SubID]);
			FirstWin_Text1:Visible(true);
                        NPCFUN:EndMessage(true);
		end;
	elseif Index == 3 then	--
		if SubID >= 301 and SubID <= 317 then
			FirstWin_Text1:SetText(LuaText["NF_HF_T" .. SubID]);
			FirstWin_Text1:Visible(true);
                        NPCFUN:EndMessage(true);
		end;
	end;
end
