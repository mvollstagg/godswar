local win = UIAPI:GetElement("FirstWin");
local lvv = nil
--圣石工艺
function NpcFunEment_SetUI(Type,Index)
	FirstWin_Text1:SetPosition(45,100);
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	strDesig = ""
	win:Visible(true);

end

function NpcFunEment_SetText(Type,Index,BtnID,SubID)
	if Index == 1 then
	   if SubID  == 2101 then
		FirstWin_Text1:SetText(NF_L0_ZBJY2102);
		FirstWin_Text1:Visible(true);
		FirstWin_Text1:SetPosition(45,100);
	   else
		FirstWin_Text1:SetText(NF_L0_ZBXQ1);
		FirstWin_Text1:Visible(true);
		FirstWin_Text1:SetPosition(45,100);
	   end
	elseif Index == 2 then --服务器消息提示
	   if SubID == 106 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ2);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	      FirstWin_Text2:SetText(NF_L0_ZBXQ3);
	      FirstWin_Text2:Visible(true);
	      FirstWin_Text2:SetPosition(45,305);
	      FirstWin_ItemBtn1:Visible(true);
	      FirstWin_ItemBtn1:SetPosition(60,170);
	      FirstWin_ItemBtn2:Visible(true);
	      FirstWin_ItemBtn2:SetPosition(60,240);
	   elseif SubID  == 206 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBXQ206);
	      Button:SetTextFormat(0);
	      Button:SetPosition(43,150);
	      Button:Visible(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif SubID  == 306 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBXQ306);
	      Button:SetTextFormat(0);
	      Button:SetPosition(43,220);
	      Button:Visible(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif SubID == 406 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ6);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	      FirstWin_Text2:SetText(NF_L0_ZBXQ7);
	      FirstWin_Text2:Visible(true);
	      FirstWin_Text2:SetPosition(45,310);
	      FirstWin_Text3:SetText(NF_L0_ZBXQ8);
	      FirstWin_Text3:Visible(true);
	      FirstWin_Text3:SetPosition(45,390);
	      FirstWin_ItemBtn1:Visible(true);
	      FirstWin_ItemBtn1:SetPosition(60,210);
	      FirstWin_ItemBtn2:Visible(true);
	      FirstWin_ItemBtn2:SetPosition(60,270);
	      FirstWin_ItemBtn3:Visible(true);
	      FirstWin_ItemBtn3:SetPosition(60,350);
	   elseif SubID  == 506 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBXQ506);
	      Button:SetTextFormat(0);
	      Button:SetPosition(43,190);
	      Button:Visible(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif SubID  == 606 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBXQ606);
	      Button:SetTextFormat(0);
	      Button:SetPosition(43,250);
	      Button:Visible(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif SubID == 706 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ9);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	      FirstWin_Text2:SetText(NF_L0_ZBXQ10);
	      FirstWin_Text2:Visible(true);
	      FirstWin_Text2:SetPosition(45,310);
	      FirstWin_Text3:SetText(NF_L0_ZBXQ11);
	      FirstWin_Text3:Visible(true);
	      FirstWin_Text3:SetPosition(45,390);
	      FirstWin_ItemBtn1:Visible(true);
	      FirstWin_ItemBtn1:SetPosition(60,190);
	      FirstWin_ItemBtn2:Visible(true);
	      FirstWin_ItemBtn2:SetPosition(60,260);
	      FirstWin_ItemBtn3:Visible(true);
	      FirstWin_ItemBtn3:SetPosition(60,350);
	   elseif SubID  == 806 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBXQ806);
	      Button:SetTextFormat(0);
	      Button:SetPosition(43,170);
	      Button:Visible(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   elseif SubID  == 906 then
	      print(BtnID,SubID);
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBXQ906);
	      Button:SetTextFormat(0);
	      Button:SetPosition(43,230);
	      Button:Visible(true);
	      Button:Enable(false);
	      UIAPI:SetChecked(false,Button);
	   end;
	end;
	if math.mod(SubID ,100) == 0 then
	   if SubID / 100 == 1 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ100);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 2 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ200);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 3 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ300);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 4 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ400);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 5 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ500);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 7 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ700);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 8 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ800);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 9 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ900);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 10 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1000);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 11 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1100);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 12 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1200);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 13 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1300);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 14 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1400);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 15 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1500);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 16 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1600);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 17 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1700);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 18 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1800);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 19 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1900);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 20 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ2000);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 21 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ2100);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif SubID / 100 == 22 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ2200);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   end;
	   FirstWin_ButtonA1:SetPosition(220,500);
	   FirstWin_ButtonA1:Visible(true);
	   FirstWin_ButtonA2:Visible(false);
	   NPCFUN:EndMessage(true);
	elseif math.mod(SubID ,100) == 1 then
	   if ( SubID - 1 ) / 100 == 1 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBXQ101);
	      Button:Visible(true);
	      Button:SetPosition(65,305);
	   elseif ( SubID - 1 ) / 100 == 2 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBXQ201);
	      Button:Visible(true);
	      Button:SetPosition(65,330);
	   elseif ( SubID - 1 ) / 100 == 3 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBXQ301);
	      Button:Visible(true);
	      Button:SetPosition(65,280);
	   elseif ( SubID - 1 ) / 100 == 4 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBXQ401);
	      Button:Visible(true);
	      Button:SetPosition(65,230);
	   elseif ( SubID - 1 ) / 100 == 5 then
	      local Button = win:GetChild("FirstWin_Button" .. BtnID);
	      Button:SetText(NF_L0_ZBXQ501);
	      Button:Visible(true);
	      Button:SetPosition(65,255);
	   end;
	elseif math.mod(SubID ,100) == 2 then
	   if ( SubID - 2 ) / 100 == 9040 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ904002);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif ( SubID - 2 ) / 100 == 9041 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ904102);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif ( SubID - 2 ) / 100 == 9042 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ904202);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   end;
	   FirstWin_ButtonA1:SetPosition(220,500);
	   FirstWin_ButtonA1:Visible(true);
	   FirstWin_ButtonA2:Visible(false);
	   NPCFUN:EndMessage(true);
	elseif math.mod(SubID ,100) == 3 then
	   if math.mod((SubID-3)/100 ,100) == 1 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ103..( ( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) / 100 )..NF_L0_ZBXQ10003);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 2 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ203..( ( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) / 100 )..NF_L0_ZBXQ10003);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 5 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ503..( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) );
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 6 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ603..( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) );
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 7 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ703..( ( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) / 100 )..NF_L0_ZBXQ10003);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 8 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ803..( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) );
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 9 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ903..( ( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) / 100 )..NF_L0_ZBXQ10003);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 10 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1003..( ( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) / 100 )..NF_L0_ZBXQ10003);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 11 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1103..( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) );
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 12 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1203..( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) );
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 19 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1903..( ( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) / 100 )..NF_L0_ZBXQ10003);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 20 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ2003..( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) );
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 13 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1303..( ( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) / 100 )..NF_L0_ZBXQ10003);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 14 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ1403..( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) );
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 3 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ303..( ( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) / 100 )..NF_L0_ZBXQ10003);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif math.mod((SubID-3)/100 ,100) == 4 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ403..( ( ( ( ( SubID - 3 ) / 100 ) - math.mod((SubID-3)/100 ,100) ) / 100 ) / 100 )..NF_L0_ZBXQ10003);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   end;
	   FirstWin_ButtonA1:SetPosition(220,500);
	   FirstWin_ButtonA1:Visible(true);
	   FirstWin_ButtonA2:Visible(false);
	   NPCFUN:EndMessage(true);
	elseif math.mod(SubID ,100) == 4 then
	   if ( SubID - 4 ) / 100 == 9030 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ903004);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif ( SubID - 4 ) / 100 == 9031 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ903104);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   end;
	   FirstWin_ButtonA1:SetPosition(220,500);
	   FirstWin_ButtonA1:Visible(true);
	   FirstWin_ButtonA2:Visible(false);
	   NPCFUN:EndMessage(true);
	elseif math.mod(SubID ,100) == 5 then
	   if ( SubID - 5 ) / 100 == 9030 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ903005);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   elseif ( SubID - 5 ) / 100 == 9031 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ903105);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	   end;
	   FirstWin_ButtonA1:SetPosition(220,500);
	   FirstWin_ButtonA1:Visible(true);
	   FirstWin_ButtonA2:Visible(false);
	   NPCFUN:EndMessage(true);
	end;
end

function NpcFunEment_SetMsg(Type,Index,PreSubID,SubID)
	if Index == 1 then
	   if SubID == 201 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ4);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	      FirstWin_Text2:SetText(NF_L0_ZBXQ41);
	      FirstWin_Text2:Visible(true);
	      FirstWin_Text2:SetPosition(45,200);
	      FirstWin_ItemBtn1:Visible(true);
	      FirstWin_ItemBtn1:SetPosition(60,160);
	      InputText1:Visible(true);
	      InputText1:SetPosition(52,300);
	      Input11:Visible(true);
	      Input11:SetPosition(56,300);
	      Input12:Visible(true);
	      Input12:SetPosition(48,300);
	      Input13:Visible(true);
	      Input13:SetPosition(150,300);
	      NPCFUN:HaveMessageBox(true);
	   elseif SubID == 301 then
	      FirstWin_Text1:SetText(NF_L0_ZBXQ5);
	      FirstWin_Text1:Visible(true);
	      FirstWin_Text1:SetPosition(45,100);
	      FirstWin_Text2:SetText(NF_L0_ZBXQ12);
	      FirstWin_Text2:Visible(true);
	      FirstWin_Text2:SetPosition(45,230);
	      FirstWin_ItemBtn1:Visible(true);
	      FirstWin_ItemBtn1:SetPosition(60,290);
	      NPCFUN:HaveMessageBox(true);
	   end;
	elseif Index == 2 and PreSubID == 501 then
		lvv = PlayerAPI:GetNpcFunStoneLv(0);
	    if lvv ==1 then
			lvv=0;
			FirstWin_Text1:SetText(NF_L0_ZBXQ5000);
			FirstWin_Text1:Visible(true);
			FirstWin_Text1:SetPosition(45,100);
			NPCFUN:HaveMessageBox(true);
		end;
	end;
end
