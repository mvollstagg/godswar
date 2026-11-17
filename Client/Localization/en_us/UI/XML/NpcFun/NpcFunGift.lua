local win = UIAPI:GetElement("FirstWin");
--节日礼物
function NpcFunGift_SetUI(Type,Index)
	FirstWin_ButtonA1:SetPosition(145,500);
	FirstWin_ButtonA2:SetPosition(220,500);
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	win:Visible(true);

end

function NpcFunGift_SetText(Type,Index,BtnID,SubID)
		if Index == 1 then
                   if SubID == 1 then
                      FirstWin_Text1:SetText(NF_L0_GIFT1);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 2 then
                      local Button = win:GetChild("FirstWin_Button" .. BtnID);
                      Button:SetText(NF_L0_GIFT2);
                      Button:Visible(true);
                   elseif SubID == 3 then
                      local Button = win:GetChild("FirstWin_Button" .. BtnID);
                      Button:SetText(NF_L0_GIFT3);
                      Button:Visible(true);
                   elseif SubID == 4 then
                      local Button = win:GetChild("FirstWin_Button" .. BtnID);
                      Button:SetText(NF_L0_GIFT4);
                      Button:Visible(true);
                   elseif SubID == 5 then
	              local Button = win:GetChild("FirstWin_Button" .. BtnID);
	              Button:SetText(NF_L0_GIFT5);
	              Button:Visible(true);
                   end;
                elseif Index == 2 then
                   if SubID == 100 then
                      FirstWin_Text1:SetText(NF_L0_GIFT100);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID % 1000 ==1 then
                      local Button = win:GetChild("FirstWin_Button" .. BtnID);
                      Button:SetText(NF_L0_GIFT107 .. ((SubID-1)/1000));
		      Button:Visible(true);
		      Button:Enable(false);
		      UIAPI:SetChecked(false,Button);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID % 1000 ==2 then
                      local Button = win:GetChild("FirstWin_Button" .. BtnID);
                      Button:SetText(NF_L0_GIFT108 .. ((SubID-2)/1000));
		      Button:Visible(true);
		      Button:Enable(false);
		      UIAPI:SetChecked(false,Button);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 101 then
                      FirstWin_Text1:SetText(NF_L0_GIFT101);
	              FirstWin_Text1:SetPosition(105,140);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 111 then
                      FirstWin_Text1:SetText(NF_L0_GIFT111);
	              FirstWin_Text1:SetPosition(45,140);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 102 then
                      FirstWin_Text1:SetText(NF_L0_GIFT102);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 103 then
                      FirstWin_Text1:SetText(NF_L0_GIFT103);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 105 then
                      FirstWin_Text1:SetText(NF_L0_GIFT105);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 106 then
                      FirstWin_Text1:SetText(NF_L0_GIFT106);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 109 then
                      FirstWin_Text1:SetText(NF_L0_GIFT109);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 200 then
                      FirstWin_Text1:SetText(NF_L0_GIFT200);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 201 then
                      FirstWin_Text1:SetText(NF_L0_GIFT201);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 202 then
                      FirstWin_Text1:SetText(NF_L0_GIFT202);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 203 then
                      FirstWin_Text1:SetText(NF_L0_GIFT203);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 204 then
                      FirstWin_Text1:SetText(NF_L0_GIFT204);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 205 then
                      FirstWin_Text1:SetText(NF_L0_GIFT205);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 206 then
                      FirstWin_Text1:SetText(NF_L0_GIFT206);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 207 then
                      FirstWin_Text1:SetText(NF_L0_GIFT207);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 208 then
                      FirstWin_Text1:SetText(NF_L0_GIFT208);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 209 then
                      FirstWin_Text1:SetText(NF_L0_GIFT209);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 210 then
                      FirstWin_Text1:SetText(NF_L0_GIFT210);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 300 then
                      FirstWin_Text1:SetText(NF_L0_GIFT300);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 301 then
                      FirstWin_Text1:SetText(NF_L0_GIFT301);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 302 then
                      FirstWin_Text1:SetText(NF_L0_GIFT302);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 303 then
                      FirstWin_Text1:SetText(NF_L0_GIFT303);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 304 then
                      FirstWin_Text1:SetText(NF_L0_GIFT304);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 305 then
                      FirstWin_Text1:SetText(NF_L0_GIFT305);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 306 then
                      FirstWin_Text1:SetText(NF_L0_GIFT306);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 307 then
                      FirstWin_Text1:SetText(NF_L0_GIFT307);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 308 then
                      FirstWin_Text1:SetText(NF_L0_GIFT308);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 309 then
                      FirstWin_Text1:SetText(NF_L0_GIFT309);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 310 then
                      FirstWin_Text1:SetText(NF_L0_GIFT310);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
                   elseif SubID == 311 then
                      FirstWin_Text1:SetText(NF_L0_GIFT311);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(False);
	   	   elseif SubID == 501 then
	     	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	              Button:SetText(NF_L0_GIFT501);
	              Button:Visible(true);
                      Button:SetPosition(45,300);
	           elseif SubID == 502 then
	              FirstWin_Text1:SetText(NF_L0_GIFT502);
	              FirstWin_Text1:Visible(true);
	           end;
                elseif Index == 3 then
	           if SubID == 511 then
	              FirstWin_Text1:SetText(NF_L0_GIFT511);
	              FirstWin_Text1:Visible(true);
	              NPCFUN:EndMessage(true);
	              NPCFUN:NeedMessage(False);
	           elseif SubID == 512 then
	              FirstWin_Text1:SetText(NF_L0_GIFT512);
	              FirstWin_Text1:Visible(true);
	              NPCFUN:EndMessage(true);
	              NPCFUN:NeedMessage(False);
	           elseif SubID == 513 then
	              FirstWin_Text1:SetText(NF_L0_GIFT513);
	              FirstWin_Text1:Visible(true);
	              NPCFUN:EndMessage(true);
	              NPCFUN:NeedMessage(False);
	           end;
                end;
end
