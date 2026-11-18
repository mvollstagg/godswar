local win = UIAPI:GetElement("FirstWin");
--供奉奖励
function NpcFunAward_SetUI(Type,Index)
	if Index == 1 then
		FirstWin_Text1:SetPosition(45,100);
		FirstWin_Text2:SetPosition(45,120);
		FirstWin_Text3:SetPosition(45,140);

        	FirstWin_Button1:SetPosition(65,160);
        	FirstWin_Button2:SetPosition(65,185);
        	FirstWin_Button3:SetPosition(65,210);
		FirstWin_Button4:SetPosition(65,235);
		FirstWin_Button5:SetPosition(65,260);
	 	FirstWin_Button6:SetPosition(65,285);
	 	FirstWin_Button7:SetPosition(65,310);
		FirstWin_Button8:SetPosition(65,335);
		FirstWin_Button9:SetPosition(65,360);
	 	FirstWin_Button10:SetPosition(65,385);
		FirstWin_Button11:SetPosition(65,410);
		FirstWin_Button12:SetPosition(65,435);

		FirstWin_ButtonA1:SetPosition(145,500);
		FirstWin_ButtonA2:SetPosition(220,500);
		FirstWin_ButtonA1:Visible(true);
		FirstWin_ButtonA2:Visible(true);
		win:Visible(true);
	end;
	if Index == 2 then
		FirstWin_Text1:SetPosition(45,100);
	        FirstWin_Text2:SetPosition(45,140);
	        FirstWin_Text3:SetPosition(45,160);

        	FirstWin_Button1:SetPosition(65,160);
               	FirstWin_Button2:SetPosition(65,185);
         	FirstWin_Button3:SetPosition(65,210);
	        FirstWin_Button4:SetPosition(65,235);
	        FirstWin_Button5:SetPosition(65,260);
	        FirstWin_Button6:SetPosition(65,285);
	        FirstWin_Button7:SetPosition(65,310);
	        FirstWin_Button8:SetPosition(65,335);
	        FirstWin_Button9:SetPosition(65,360);
	        FirstWin_Button10:SetPosition(65,385);
	        FirstWin_Button11:SetPosition(65,410);
	        FirstWin_Button12:SetPosition(65,435);

	        FirstWin_ButtonA1:SetPosition(145,500);
	        FirstWin_ButtonA2:SetPosition(220,500);
		FirstWin_ButtonA1:Visible(true);
		FirstWin_ButtonA2:Visible(true);
		win:Visible(true);
	end;
	if Index == 3 then
	        FirstWin_Text1:SetPosition(45,100);
	        FirstWin_Text2:SetPosition(45,170);
	        FirstWin_Text3:SetPosition(45,190);

        	FirstWin_Button1:SetPosition(65,210);
               	FirstWin_Button2:SetPosition(65,235);
         	FirstWin_Button3:SetPosition(65,260);
	        FirstWin_Button4:SetPosition(65,285);
	        FirstWin_Button5:SetPosition(65,310);
	        FirstWin_Button6:SetPosition(65,335);
	        FirstWin_Button7:SetPosition(65,360);
	        FirstWin_Button8:SetPosition(65,385);
	        FirstWin_Button9:SetPosition(65,410);
	        FirstWin_Button10:SetPosition(65,435);
	        FirstWin_Button11:SetPosition(65,410);
	        FirstWin_Button12:SetPosition(65,435);

	        FirstWin_ButtonA1:SetPosition(145,500);
	        FirstWin_ButtonA2:SetPosition(220,500);
		FirstWin_ButtonA1:Visible(true);
		FirstWin_ButtonA2:Visible(true);
		win:Visible(true);
	end;
	if Index == 4 then
	        FirstWin_Text1:SetPosition(45,100);
	        FirstWin_Text2:SetPosition(45,120);
	        FirstWin_Text3:SetPosition(45,140);

        	FirstWin_Button1:SetPosition(65,160);
               	FirstWin_Button2:SetPosition(65,185);
         	FirstWin_Button3:SetPosition(65,210);
	        FirstWin_Button4:SetPosition(65,235);
	        FirstWin_Button5:SetPosition(65,260);
	        FirstWin_Button6:SetPosition(65,285);
	        FirstWin_Button7:SetPosition(65,310);
	        FirstWin_Button8:SetPosition(65,335);
	        FirstWin_Button9:SetPosition(65,360);
	        FirstWin_Button10:SetPosition(65,385);
	        FirstWin_Button11:SetPosition(65,410);
	        FirstWin_Button12:SetPosition(65,435);

	        FirstWin_ButtonA1:SetPosition(145,500);
	        FirstWin_ButtonA2:SetPosition(220,500);
		FirstWin_ButtonA1:Visible(true);
		FirstWin_ButtonA2:Visible(true);
		win:Visible(true);
	end;
	if Index == 5 then
	        FirstWin_Text1:SetPosition(45,100);
	        FirstWin_Text2:SetPosition(45,120);
	        FirstWin_Text3:SetPosition(45,140);

        	FirstWin_Button1:SetPosition(65,160);
               	FirstWin_Button2:SetPosition(65,185);
         	FirstWin_Button3:SetPosition(65,210);
	        FirstWin_Button4:SetPosition(65,235);
	        FirstWin_Button5:SetPosition(65,260);
	        FirstWin_Button6:SetPosition(65,285);
	        FirstWin_Button7:SetPosition(65,310);
	        FirstWin_Button8:SetPosition(65,335);
	        FirstWin_Button9:SetPosition(65,360);
	        FirstWin_Button10:SetPosition(65,385);
	        FirstWin_Button11:SetPosition(65,410);
	        FirstWin_Button12:SetPosition(65,435);

	        FirstWin_ButtonA1:SetPosition(220,500);
	        FirstWin_ButtonA2:SetPosition(220,500);
		FirstWin_ButtonA1:Visible(true);
		FirstWin_ButtonA2:Visible(false);
		win:Visible(true);
	end;

end

function NpcFunAward_SetText(Type,Index,BtnID,SubID)
		if Index == 1 then
                   if SubID % 100 == 1 then
                      if ((SubID - 1) / 100) % 100 == 3 then
                         FirstWin_Text1:SetText(NF_L0_CJ1.."|cffFF0000"..((((SubID - 1) / 100) - 3)/100).."|cffFFFFFF"..NF_L0_CJ29);
                         FirstWin_Text1:Visible(true);
                      elseif ((SubID - 1) / 100) % 100 == 1 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ2);
                         Button:Visible(true);
                      elseif ((SubID - 1) / 100) % 100 == 2 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ3);
                         Button:Visible(true);
                      end;
                   end;
                elseif Index == 2 then
                   if SubID % 100 == 1 then
                      if (SubID - 1) / 100 == 10 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ4);
                         Button:Visible(true);
                      elseif (SubID - 1) / 100 == 11 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ5);
                         Button:Visible(true);
                      elseif (SubID - 1) / 100 == 12 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ6);
                         Button:Visible(true);
                      elseif (SubID - 1) / 100 == 13 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ7);
                         Button:Visible(true);
                      elseif (SubID - 1) / 100 == 14 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ8);
                         Button:Visible(true);
                      elseif (SubID - 1) / 100 == 15 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ9);
                         Button:Visible(true);
                      elseif (SubID - 1) / 100 == 16 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ10);
                         Button:Visible(true);
                      elseif (SubID - 1) / 100 == 17 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ11);
                         Button:Visible(true);
                      elseif (SubID - 1) / 100 == 18 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ12);
                         Button:Visible(true);
                      elseif (SubID - 1) / 100 == 19 then
                         local Button = win:GetChild("FirstWin_Button" .. BtnID);
                         Button:SetText(NF_L0_CJ13);
                         Button:Visible(true);
                      end;
                   elseif SubID % 100 == 10 then
                      FirstWin_Text1:SetText(NF_L0_CJ14.."|cffFF0000"..((SubID -10) / 100).."|cffFFFFFF"..NF_L0_CJ15);
                      FirstWin_Text1:Visible(true);
                   elseif SubID % 100 == 11 then
                      FirstWin_Text1:SetText(NF_L0_CJ16.."|cffFF0000"..((SubID -11) / 100).."|cffFFFFFF"..NF_L0_CJ17);
                      FirstWin_Text1:Visible(true);
                   end;
                elseif Index == 3 then
                   if SubID % 100 == 2 then
                      FirstWin_ButtonA1:SetPosition(220,500);
                      FirstWin_ButtonA1:Visible(true);
		      FirstWin_ButtonA2:Visible(false);
                      if ((SubID - 2) / 100) % 100 == 10 then
                         FirstWin_Text1:SetText(NF_L0_CJ18.."|cffFF0000"..((((SubID - 2) / 100) - 10) / 100).."|cffFFFFFF"..NF_L0_CJ28);
                         FirstWin_Text1:Visible(true);
                      elseif ((SubID - 2) / 100) % 100 == 11 then
                         FirstWin_Text1:SetText(NF_L0_CJ19.."|cffFF0000"..((((SubID - 2) / 100) - 11) / 100).."|cffFFFFFF"..NF_L0_CJ28);
                         FirstWin_Text1:Visible(true);
                      elseif ((SubID - 2) / 100) % 100 == 12 then
                         FirstWin_Text1:SetText(NF_L0_CJ20.."|cffFF0000"..((((SubID - 2) / 100) - 12) / 100).."|cffFFFFFF"..NF_L0_CJ28);
                         FirstWin_Text1:Visible(true);
                      elseif ((SubID - 2) / 100) % 100 == 13 then
                         FirstWin_Text1:SetText(NF_L0_CJ21.."|cffFF0000"..((((SubID - 2) / 100) - 13) / 100).."|cffFFFFFF"..NF_L0_CJ28);
                         FirstWin_Text1:Visible(true);
                      elseif ((SubID - 2) / 100) % 100 == 14 then
                         FirstWin_Text1:SetText(NF_L0_CJ22.."|cffFF0000"..((((SubID - 2) / 100) - 14) / 100).."|cffFFFFFF"..NF_L0_CJ28);
                         FirstWin_Text1:Visible(true);
                      elseif ((SubID - 2) / 100) % 100 == 15 then
                         FirstWin_Text1:SetText(NF_L0_CJ23.."|cffFF0000"..((((SubID - 2) / 100) - 15) / 100).."|cffFFFFFF"..NF_L0_CJ28);
                         FirstWin_Text1:Visible(true);
                      elseif ((SubID - 2) / 100) % 100 == 16 then
                         FirstWin_Text1:SetText(NF_L0_CJ24.."|cffFF0000"..((((SubID - 2) / 100) - 16) / 100).."|cffFFFFFF"..NF_L0_CJ28);
                         FirstWin_Text1:Visible(true);
                      elseif ((SubID - 2) / 100) % 100 == 17 then
                         FirstWin_Text1:SetText(NF_L0_CJ25.."|cffFF0000"..((((SubID - 2) / 100) - 17) / 100).."|cffFFFFFF"..NF_L0_CJ28);
                         FirstWin_Text1:Visible(true);
                      elseif ((SubID - 2) / 100) % 100 == 18 then
                         FirstWin_Text1:SetText(NF_L0_CJ26.."|cffFF0000"..((((SubID - 2) / 100) - 18) / 100).."|cffFFFFFF"..NF_L0_CJ28);
                         FirstWin_Text1:Visible(true);
                      elseif ((SubID - 2) / 100) % 100 == 19 then
                         FirstWin_Text1:SetText(NF_L0_CJ27.."|cffFF0000"..((((SubID - 2) / 100) - 19) / 100).."|cffFFFFFF"..NF_L0_CJ28);
                         FirstWin_Text1:Visible(true);
                      end;
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(false);
                   end;
                end;
                if SubID % 100 == 0 then
                   FirstWin_ButtonA1:SetPosition(220,500);
                   FirstWin_ButtonA1:Visible(true);
		   FirstWin_ButtonA2:Visible(false);
                   if SubID / 100 == 100 then
                      FirstWin_Text1:SetText(NF_L0_CJ100);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(false);                   
                   elseif SubID / 100 == 101 then
                      FirstWin_Text1:SetText(NF_L0_CJ101);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(false);
                   elseif SubID / 100 == 102 then
                      FirstWin_Text1:SetText(NF_L0_CJ102);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(false);
                   elseif SubID / 100 == 103 then
                      FirstWin_Text1:SetText(NF_L0_CJ103);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(false);
                   elseif SubID / 100 == 104 then
                      FirstWin_Text1:SetText(NF_L0_CJ104);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(false);
                   elseif SubID / 100 == 105 then
                      FirstWin_Text1:SetText(NF_L0_CJ105);
                      FirstWin_Text1:Visible(true);
                      NPCFUN:EndMessage(true);
                      NPCFUN:NeedMessage(false);
                   end;
                end;
end
