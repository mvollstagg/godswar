local win = UIAPI:GetElement("FirstWin");
--上古遗迹
function NpcFunRemain_SetUI(Type,Index)
	FirstWin_Text1:SetPosition(45,100);
	FirstWin_Text1:Visible(false);
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	strDesig = ""
	win:Visible(true);

end

function NpcFunRemain_SetText(Type,Index,BtnID,SubID)
		if Index== 1 then    ----第1个页面------
			if SubID == 101 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_RM101);
                                Button:SetPosition(65,285)
				Button:Visible(true);
			elseif SubID == 102 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_RM102);
                                Button:SetPosition(65,285)
				Button:Visible(true);
			elseif SubID == 103 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_RM103);
                                Button:SetPosition(65,285)
				Button:Visible(true);
			elseif SubID == 104 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_RM104);
                                Button:SetPosition(65,285)
				Button:Visible(true);
			elseif SubID == 105 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_RM105);
                                Button:SetPosition(65,285)
				Button:Visible(true);
			elseif SubID == 106 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_RM106);
                                Button:SetPosition(65,285)
				Button:Visible(true);
			elseif SubID == 107 then
				print(BtnID,SubID);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_RM107);
                                Button:SetPosition(65,285)
				Button:Visible(true);
                        elseif SubID ==501 then
				FirstWin_Text1:SetText(NF_L0_RM501);
				FirstWin_Text1:Visible(true);
                        end;
		elseif Index == 2 then	--第2个页面---------------------
			if SubID == 200 then
				FirstWin_Text1:SetText(NF_L0_RM200);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 201 then
				FirstWin_Text1:SetText(NF_L0_RM201);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 202 then
				FirstWin_Text1:SetText(NF_L0_RM202);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID%1000 == 203 then
				FirstWin_Text1:SetText(NF_L0_RM203..(((SubID-203)/1000)*500));
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID%1000 == 204 then
				FirstWin_Text1:SetText(NF_L0_RM204..(((SubID-204)/1000)*500));
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID%1000 == 214 then
				FirstWin_Text1:SetText(NF_L0_RM214..(((SubID-214)/1000)*500));
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID%1000 == 215 then
				FirstWin_Text1:SetText(NF_L0_RM215..(((SubID-215)/1000)*500));
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID%1000 == 216 then
				FirstWin_Text1:SetText(NF_L0_RM216..(((SubID-216)/1000)*500));
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID%1000 == 217 then
				FirstWin_Text1:SetText(NF_L0_RM217..(((SubID-217)/1000)*500));
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID%1000 == 218 then
				FirstWin_Text1:SetText(NF_L0_RM218..(((SubID-218)/1000)*500));
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID%1000 == 219 then
				FirstWin_Text1:SetText(NF_L0_RM219..(((SubID-219)/1000)*500));
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			end;
		end;
end
