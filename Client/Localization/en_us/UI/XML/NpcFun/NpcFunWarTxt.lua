local win = UIAPI:GetElement("FirstWin");
--战场说明
function NpcFunWarTxt_SetUI(Type,Index)
	FirstWin_Text1:SetPosition(45,100);
	FirstWin_Text1:Visible(false);
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	strDesig = ""
	win:Visible(true);

end

function NpcFunWarTxt_SetText(Type,Index,BtnID,SubID)
		if Index == 1 then
			if SubID== 200 then
                                FirstWin_Text1:SetText(NF_L0_W200);
                                FirstWin_Text1:Visible(true);
                                FirstWin_Text1:SetPosition(45,100);
			elseif SubID== 201 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_W301);
				Button:Visible(true);
                                Button:SetPosition(65,145);
			elseif SubID== 202 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_W302);
				Button:Visible(true);
                                Button:SetPosition(65,175);
			elseif SubID== 203 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_W303);
				Button:Visible(true);
                                Button:SetPosition(65,205);
			elseif SubID== 204 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_W304);
				Button:Visible(true);
                                Button:SetPosition(65,235);
			end;
		elseif Index == 2 then --服务器消息提示
			if SubID ==211 then
				FirstWin_Text1:SetText(NF_L0_W211);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID ==212 then
				FirstWin_Text1:SetText(NF_L0_W212);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID==213 then
				FirstWin_Text1:SetText(NF_L0_W213);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID ==214 then
				FirstWin_Text1:SetText(NF_L0_W214);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID ==231 then
				FirstWin_Text1:SetText(NF_L0_W231);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID ==232 then
				FirstWin_Text1:SetText(NF_L0_W232);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID==233 then
				FirstWin_Text1:SetText(NF_L0_W233);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID ==234 then
				FirstWin_Text1:SetText(NF_L0_W234);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			end;
		end;
end
