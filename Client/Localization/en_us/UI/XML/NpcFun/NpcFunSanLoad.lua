local win = UIAPI:GetElement("FirstWin");
--圣衣铸造
function NpcFunSanLoad_SetUI(Type,Index)
	FirstWin_ButtonA1:SetPosition(145,500);
	FirstWin_ButtonA2:SetPosition(220,500);
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	win:Visible(true);

end

function NpcFunSanLoad_SetText(Type,Index,BtnID,SubID)
	if Index == 1 then
	   if SubID  == 2101 then
		FirstWin_Text1:SetText(NF_L0_ZBJY2101);
		FirstWin_Text1:Visible(true);
		FirstWin_Text1:SetPosition(45,100);
	   else
		FirstWin_Text1:SetText(NF_L0_ZBJY1);
		FirstWin_Text1:Visible(true);
		FirstWin_Text1:SetPosition(45,100);
	   end
	elseif Index == 2 then --服务器消息提示
	   if math.mod(SubID ,100) == 4 then
		
		local Button = win:GetChild("FirstWin_Button" .. BtnID);
		Button:SetText(NF_L0_ZBJY404..((SubID-4)/100));
		Button:SetTextFormat(0);
		Button:SetPosition(43,350);
		Button:Visible(true);
		Button:Enable(false);
		UIAPI:SetChecked(false,Button);
	   elseif math.mod(SubID ,100) == 5 then
		
		local Button = win:GetChild("FirstWin_Button" .. BtnID);
		Button:SetText(NF_L0_ZBJY405..((SubID-5)/100));
		Button:SetTextFormat(0);
		Button:SetPosition(43,370);
		Button:Visible(true);
		Button:Enable(false);
		UIAPI:SetChecked(false,Button);
	   elseif SubID  == 106 then
		FirstWin_Text1:SetText(NF_L0_ZBJY2);
		FirstWin_Text1:Visible(true);
		FirstWin_Text1:SetPosition(45,100);
		FirstWin_Text2:SetText(NF_L0_ZBJY3);
		FirstWin_Text2:Visible(true);
		FirstWin_Text2:SetPosition(45,230);
		FirstWin_Text3:SetText(NF_L0_ZBJY4);
		FirstWin_Text3:Visible(true);
		FirstWin_Text3:SetPosition(45,305);
		FirstWin_ItemBtn1:Visible(true);
		FirstWin_ItemBtn1:SetPosition(52,180);
		InputText1:Visible(true);
		InputText1:SetPosition(60,270);
		Input11:Visible(true);
		Input11:SetPosition(60,270);
		Input12:Visible(true);
		Input12:SetPosition(52,270);
		Input13:Visible(true);
		Input13:SetPosition(154,270);
		
		local Button = win:GetChild("FirstWin_Button" .. BtnID);
		Button:SetText(NF_L0_ZBJY406);
		Button:SetTextFormat(0);
		Button:SetPosition(43,150);
		Button:Visible(true);
		Button:Enable(false);
		UIAPI:SetChecked(false,Button);
	   elseif SubID == 206 then
		FirstWin_Text1:SetText(NF_L0_ZBJY5);
		FirstWin_Text1:Visible(true);
		FirstWin_Text1:SetPosition(45,100);
		FirstWin_Text2:SetText(NF_L0_ZBJY6);
		FirstWin_Text2:Visible(true);
		FirstWin_Text2:SetPosition(45,320);
		FirstWin_Text3:SetText(NF_L0_ZBJY36);
		FirstWin_Text3:Visible(true);
		FirstWin_Text3:SetPosition(45,160);
		FirstWin_ItemBtn1:Visible(true);
		FirstWin_ItemBtn1:SetPosition(60,200);
		FirstWin_ItemBtn2:Visible(true);
		FirstWin_ItemBtn2:SetPosition(60,270);
		
		local Button = win:GetChild("FirstWin_Button" .. BtnID);
		Button:SetText(NF_L0_ZBJY37);
		Button:SetTextFormat(0);
		Button:SetPosition(43,250);
		Button:Visible(true);
		Button:Enable(false);
		UIAPI:SetChecked(false,Button);
	   elseif SubID== 306 then
		FirstWin_Text1:SetText(NF_L0_ZBJY7);
		FirstWin_Text1:Visible(true);
		FirstWin_Text1:SetPosition(45,100);
		FirstWin_Text2:SetText(NF_L0_ZBJY8);
		FirstWin_Text2:Visible(true);
		FirstWin_Text2:SetPosition(45,310);
		FirstWin_Text3:SetText(NF_L0_ZBJY9);
		FirstWin_Text3:Visible(true);
		FirstWin_Text3:SetPosition(45,450);
		FirstWin_ItemBtn1:Visible(true);
		FirstWin_ItemBtn1:SetPosition(60,190);
		FirstWin_ItemBtn2:Visible(true);
		FirstWin_ItemBtn2:SetPosition(60,260);
	   elseif SubID  == 406 then
		
		local Button = win:GetChild("FirstWin_Button" .. BtnID);
		Button:SetText(NF_L0_ZBJY506);
		Button:SetTextFormat(0);
		Button:SetPosition(43,170);
		Button:Visible(true);
		Button:Enable(false);
		UIAPI:SetChecked(false,Button);
	   elseif SubID  == 506 then
		
		local Button = win:GetChild("FirstWin_Button" .. BtnID);
		Button:SetText(NF_L0_ZBJY606);
		Button:SetTextFormat(0);
		Button:SetPosition(43,240);
		Button:Visible(true);
		Button:Enable(false);
		UIAPI:SetChecked(false,Button);
	   end;
	end;
	if math.mod(SubID ,100) == 0 then
	   if SubID / 100 == 1 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY100);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 2 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY200);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 3 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY300);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 4 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY400);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 5 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY500);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 6 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY600);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 7 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY700);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 8 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY800);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 9 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY900);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 10 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY1000);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 11 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY1100);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 12 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY1200);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 13 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY1300);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 14 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY1400);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 15 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY1500);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 16 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY1600);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 17 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY1700);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 18 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY1800);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 19 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY1900);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);  
	   elseif SubID / 100 == 20 then
	      FirstWin_Text1:SetText(NF_L0_ZBJY2000);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);    
	   end;
	   FirstWin_Button1:SetTextFormat(5);
	   FirstWin_Button2:SetTextFormat(5);
	   FirstWin_ButtonA1:SetPosition(220,500);
	   FirstWin_ButtonA1:Visible(true);
	   FirstWin_ButtonA2:Visible(false);
	   NPCFUN:EndMessage(true);
	elseif math.mod(SubID ,100) == 1 then
	   if ( SubID - 1 ) / 100 == 1 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBJY101);
	      Button:Visible(true);
	      Button:SetPosition(65,230);
	   elseif ( SubID - 1 ) / 100 == 2 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBJY201);
	      Button:Visible(true);
	      Button:SetPosition(65,255);
	   elseif ( SubID - 1 ) / 100 == 3 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBJY301);
	      Button:Visible(true);
	      Button:SetPosition(65,280);
	   end;
	elseif math.mod(SubID ,100) == 2 then
	   if ( SubID - 2 ) / 100 == 1 then
	      FirstWin_Text2:SetText(NF_L0_ZBJY102);
	      FirstWin_Text2:Visible(true);
	      FirstWin_Text2:SetPosition(45,150);
	   elseif ( SubID - 2 ) / 100 == 2 then
	      FirstWin_Text2:SetText(NF_L0_ZBJY202);
	      FirstWin_Text2:Visible(true);
	      FirstWin_Text2:SetPosition(45,150);
	   elseif ( SubID - 2 ) / 100 == 3 then
	      FirstWin_Text2:SetText(NF_L0_ZBJY302);
	      FirstWin_Text2:Visible(true);
	      FirstWin_Text2:SetPosition(45,150);                      
	   end;
	end;
end
