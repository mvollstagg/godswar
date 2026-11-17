local win = UIAPI:GetElement("FirstWin");
--活动
function NpcFunVocation_SetUI(Type,Index)
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	win:Visible(true);

end

function NpcFunVocation_SetText(Type,Index,BtnID,SubID)
	if Index== 1 then    ----第几个页面------
		if SubID== 100      then
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetPosition(60,150);
			Button:SetText(NF_L0_V100);
			Button:Visible(true);
		elseif SubID== 101      then
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetPosition(60,180);
			Button:SetText(NF_L0_V101);
			Button:Visible(true);
		elseif SubID== 102      then
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetPosition(60,210);
			Button:SetText(NF_L0_V102);
			Button:Visible(true);
		elseif SubID== 103      then
			local Button = win:GetChild("FirstWin_Button" .. BtnID);
			Button:SetPosition(60,240);
			Button:SetText(NF_L0_V103);
			Button:Visible(true);
		elseif SubID == 121 then
			FirstWin_Text1:SetText(NF_L0_S009);
			FirstWin_Text1:Visible(true);

		end;
	elseif Index == 2 then	--服务器提示消息---------------------
		if SubID == 110 then
           		FirstWin_ItemBtn1:Visible(true);
            		FirstWin_ItemBtn1:SetPosition(60,140);
			FirstWin_Text1:SetText(NF_L0_V104);
			FirstWin_Text1:Visible(true);
		elseif SubID == 111 then
	        	FirstWin_ItemBtn2:Visible(true);
          		FirstWin_ItemBtn2:SetPosition(60,220);
			FirstWin_Text2:SetText(NF_L0_V105);
			FirstWin_Text2:Visible(true);
            		FirstWin_Text2:SetPosition(45,180);
		elseif SubID == 112 then
            		FirstWin_ItemBtn1:Visible(true);
            		FirstWin_ItemBtn1:SetPosition(60,150);
			FirstWin_Text1:SetText(NF_L0_V106);
			FirstWin_Text1:Visible(true);
		elseif SubID == 113 then
           		FirstWin_ItemBtn2:Visible(true);
            		FirstWin_ItemBtn2:SetPosition(60,240);
			FirstWin_Text2:SetText(NF_L0_V107);
			FirstWin_Text2:Visible(true);
            		FirstWin_Text2:SetPosition(45,200);
		elseif SubID == 114 then
            		FirstWin_ItemBtn3:Visible(true);
            		FirstWin_ItemBtn3:SetPosition(60,330);
			FirstWin_Text3:SetText(NF_L0_V108);
			FirstWin_Text3:Visible(true);
            		FirstWin_Text3:SetPosition(45,290);
		elseif SubID == 115 then
            		FirstWin_ItemBtn1:Visible(true);
            		FirstWin_ItemBtn1:SetPosition(60,150);
			FirstWin_Text1:SetText(NF_L0_V111);
			FirstWin_Text1:Visible(true);
		elseif SubID == 116 then
            		FirstWin_ItemBtn2:Visible(true);
            		FirstWin_ItemBtn2:SetPosition(60,240);
			FirstWin_Text2:SetText(NF_L0_V109);
			FirstWin_Text2:Visible(true);
            		FirstWin_Text2:SetPosition(45,200);
		elseif SubID == 117 then
            		FirstWin_ItemBtn3:Visible(true);
            		FirstWin_ItemBtn3:SetPosition(60,330);
			FirstWin_Text3:SetText(NF_L0_V108);
			FirstWin_Text3:Visible(true);
            		FirstWin_Text3:SetPosition(45,290);
		elseif SubID == 118 then
			FirstWin_Text1:SetText(NF_L0_V110);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 119 then
			FirstWin_Text3:SetText(NF_L0_V124);
			FirstWin_Text3:Visible(true);
            		FirstWin_Text3:SetPosition(45,260);
		end;
	elseif Index == 3 then	--服务器提示消息---------------------
		if SubID == 120 then
			FirstWin_Text1:SetText(NF_L0_V112);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 149 then
			FirstWin_Text1:SetText(NF_L0_V123);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 148 then
			FirstWin_Text1:SetText(NF_L0_V113);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 147 then
			FirstWin_Text1:SetText(NF_L0_V114);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 146 then
			FirstWin_Text1:SetText(NF_L0_V114);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 145 then
			FirstWin_Text1:SetText(NF_L0_V115);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 144 then
			FirstWin_Text1:SetText(NF_L0_V116);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 143 then
			FirstWin_Text1:SetText(NF_L0_V117);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 142 then
			FirstWin_Text1:SetText(NF_L0_V118);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 121 then
			FirstWin_Text1:SetText(NF_L0_V119);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 141 then
			FirstWin_Text1:SetText(NF_L0_V120);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 122 then
			FirstWin_Text1:SetText(NF_L0_V121);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 140 then
			FirstWin_Text1:SetText(NF_L0_V122);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		elseif SubID == 139 then
			FirstWin_Text1:SetText(NF_L0_V114);
			FirstWin_Text1:Visible(true);
			NPCFUN:EndMessage(true);
		end;
	end;
end
