local win = UIAPI:GetElement("FirstWin");
--健康跑
function NpcFunHealth_SetUI(Type,Index)
	FirstWin_Text1:SetPosition(45,100);
	FirstWin_Text1:Visible(false);
	FirstWin_Text2:Visible(false);
	FirstWin_Text3:Visible(false);
	FirstWin_ButtonA1:Visible(true);
	FirstWin_ButtonA2:Visible(true);
	strDesig = ""
	win:Visible(true);

end

function NpcFunHealth_SetText(Type,Index,BtnID,SubID)
		if Index == 1 then	------第1个页面-----
			if SubID == 1000 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1000);
                                Button:SetPosition(65,200)
				Button:Visible(true);
			elseif SubID == 1001 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1001);
                                Button:SetPosition(65,240)
				Button:Visible(true);
			elseif SubID == 1002 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1002);
                                Button:SetPosition(65,280)
				Button:Visible(true);
			elseif SubID == 1003 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1003);
                                Button:SetPosition(65,280)
				Button:Visible(true);
			elseif SubID == 1004 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1004);
                                Button:SetPosition(65,320)
				Button:Visible(true);
			elseif SubID == 1201 then
				FirstWin_Text1:SetText(NF_L0_H1113);
				FirstWin_Text1:Visible(true);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1201);
                                Button:SetPosition(65,250)
				Button:Visible(true);
			elseif SubID == 1202 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1201);
                                Button:SetPosition(65,300)
				Button:Visible(true);
			elseif SubID == 1218 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1218);
                                Button:SetPosition(65,300)
				Button:Visible(true);
			end;
		
		elseif Index == 2 then		-------消息提示文字----------
			if SubID == 1108 then
				FirstWin_Text1:SetText(NF_L0_H1108);
				FirstWin_Text1:Visible(true);
				FirstWin_Text1:SetPosition(40,200)
				NPCFUN:EndMessage(true);
			elseif SubID == 1604 then
				FirstWin_Text1:SetText(NF_L0_H1107);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1284 then
				FirstWin_Text1:SetText(NF_L0_H1284);
				FirstWin_Text1:Visible(true);
				FirstWin_Text1:SetPosition(40,80)
				NPCFUN:EndMessage(true);
			elseif SubID == 1601 then
				FirstWin_Text1:SetText(NF_L0_H1511);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1602 then
				FirstWin_Text1:SetText(NF_L0_H1101);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1251 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1252 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,300)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1253 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1254 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1255 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1256 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1257 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1258 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1259 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1260 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1261 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1262 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1263 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1264 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1265 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1266 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);
			elseif SubID == 1267 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1251);
                                Button:SetPosition(65,350)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
				FirstWin_Text1:SetText(NF_L0_H1282);
				FirstWin_Text1:SetPosition(65,180)
				FirstWin_Text1:Visible(true);
				FirstWin_Text3:SetText(NF_L0_H1283);
				FirstWin_Text3:SetPosition(120,180)
				FirstWin_Text3:Visible(true);
				InputText1:Visible(true);
		      		Input11:Visible(true); --设置为可见
		      		Input12:Visible(true); 
		      		Input13:Visible(true); 
		      		Input11:SetPosition(168,180);
		      		Input12:SetPosition(160,180);
		      		Input13:SetPosition(262,180);
                      		InputText1:SetPosition(164,181);		
			elseif SubID == 1281 then
				FirstWin_Text2:SetText(NF_L0_H1281);
				FirstWin_Text2:Visible(true);
				FirstWin_Text2:SetPosition(30,80)
			elseif SubID == 1005 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(NF_L0_H1005);
                                Button:SetPosition(60,300)
				Button:Visible(true);
			elseif SubID == 1006 then
				FirstWin_Text1:SetText(NF_L0_H1006);
				FirstWin_Text1:Visible(true);
			elseif SubID == 1101 then
				FirstWin_Text1:SetText(NF_L0_H1101);
				FirstWin_Text1:Visible(true);
				FirstWin_Text1:SetPosition(40,200)
				NPCFUN:EndMessage(true);

			elseif SubID == 1103 then
				FirstWin_Text1:SetText(NF_L0_H1103);
				FirstWin_Text1:Visible(true);
				FirstWin_Text1:SetPosition(40,150)
				NPCFUN:EndMessage(true);
			elseif SubID == 1104 then
				FirstWin_Text1:SetText(NF_L0_H1104);
				FirstWin_Text1:Visible(true);
				FirstWin_Text1:SetPosition(40,150)
				NPCFUN:EndMessage(true);
			elseif SubID == 1105 then
				FirstWin_Text1:SetText(NF_L0_H1105);
				FirstWin_Text1:Visible(true);
				FirstWin_Text1:SetPosition(40,150)
				NPCFUN:EndMessage(true);
			elseif SubID == 1106 then
				FirstWin_Text1:SetText(NF_L0_H1106);
				FirstWin_Text1:Visible(true);
				FirstWin_Text1:SetPosition(40,150)
				NPCFUN:EndMessage(true);
			elseif SubID == 1107 then
				FirstWin_Text1:SetText(NF_L0_H1107);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1114 then
				FirstWin_Text1:SetText(NF_L0_H1114);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1110 then
				FirstWin_Text1:SetText(NF_L0_H1110);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1219 then
				FirstWin_Text1:SetText(NF_L0_H1219);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1221 then
				FirstWin_Text1:SetText(NF_L0_H1107);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1222 then
				FirstWin_Text1:SetText(NF_L0_H1222);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1223 then
				FirstWin_Text1:SetText(NF_L0_H1223);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1111 then
				FirstWin_Text1:SetText(NF_L0_H1251);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);

			elseif SubID == 1113 then
				FirstWin_Text1:SetText(NF_L0_H1112);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1300 then
				FirstWin_Text1:SetText(NF_L0_H1300);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1400 then
				FirstWin_Text1:SetText(NF_L0_H1400);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			
			elseif SubID == 1507 then
				FirstWin_Text1:SetText(NF_L0_H1507);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1508 then
				FirstWin_Text1:SetText(NF_L0_H1508);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1509 then
				FirstWin_Text1:SetText(NF_L0_H1509);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1510 then
				FirstWin_Text1:SetText(NF_L0_H1510);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);

			elseif math.mod(SubID ,10) == 3 then
				FirstWin_Text1:SetText(NF_L0_H1111);
				FirstWin_Text1:Visible(true);
                                FirstWin_Text1:SetPosition(45,130);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(((SubID - 3)/10)..SM_311);
                                Button:SetPosition(5,170)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
			elseif math.mod(SubID ,10) == 4 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(((SubID - 4)/10)..SM_313);
                                Button:SetPosition(80,170)
				Button:Visible(true);
				NPCFUN:EndMessage(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
			elseif math.mod(SubID ,100) == 11 then
				FirstWin_Text2:SetText(NF_L0_H1117);
				FirstWin_Text2:Visible(true);
                                FirstWin_Text2:SetPosition(45,240);
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(((SubID - 11)/100)..SM_311);
                                Button:SetPosition(5,280)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
			elseif math.mod(SubID ,100) == 12 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(((SubID - 12)/100)..SM_313);
                                Button:SetPosition(80,280)
				Button:Visible(true);
				NPCFUN:EndMessage(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
			elseif math.mod(SubID ,10) == 5 then
				if ((SubID - 5)/10) == 9999 then
					local Button = win:GetChild("FirstWin_Button" .. BtnID);
					Button:SetText(NF_L0_H1119);
                                	Button:SetPosition(25,230)
					Button:Visible(true);
					Button:Enable(false);
                      			UIAPI:SetChecked(false,Button);
				else
					local Button = win:GetChild("FirstWin_Button" .. BtnID);
					Button:SetText(NF_L0_H1115 .. ((SubID - 5)/10)..SM_311);
                                	Button:SetPosition(-15,230)
					Button:Visible(true);
					Button:Enable(false);
                      			UIAPI:SetChecked(false,Button);
				end
				
			elseif math.mod(SubID ,10) == 6 then
				if ((SubID - 6)/10) == 999 then
					local Button = win:GetChild("FirstWin_Button" .. BtnID);
					Button:SetText(NF_L0_H1251);
                                	Button:SetPosition(70,270)
					Button:Visible(true);
					Button:Enable(false);
                      			UIAPI:SetChecked(false,Button);
				else
					local Button = win:GetChild("FirstWin_Button" .. BtnID);
					Button:SetText(((SubID - 6)/10)..SM_313);
                                	Button:SetPosition(110,230)
					Button:Visible(true);
					Button:Enable(false);
                      			UIAPI:SetChecked(false,Button);
				end
				
			elseif math.mod(SubID ,10000) == 50 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(((SubID - 50)/10000));
                                Button:SetPosition(-70,180)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
			elseif math.mod(SubID ,10000) == 60 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(((SubID - 60)/10000));
                                Button:SetPosition(-20,180)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
			elseif math.mod(SubID ,100) == 7 then
				if ((SubID - 7)/100) == 9999 then
					local Button = win:GetChild("FirstWin_Button" .. BtnID);
					Button:SetText(NF_L0_H1101);
                                	Button:SetPosition(25,270)
					Button:Visible(true);
					Button:Enable(false);
                      			UIAPI:SetChecked(false,Button);
				else
					local Button = win:GetChild("FirstWin_Button" .. BtnID);
					Button:SetText(NF_L0_H1116 .. ((SubID - 7)/100)..SM_311);
                                	Button:SetPosition(-5,270)
					Button:Visible(true);
					Button:Enable(false);
                      			UIAPI:SetChecked(false,Button);
				end
			elseif math.mod(SubID ,100) == 8 then
				if ((SubID - 8)/100) == 999 then
					local Button = win:GetChild("FirstWin_Button" .. BtnID);
					Button:SetText(NF_L0_H1251);
                                	Button:SetPosition(70,270)
					Button:Visible(true);
					Button:Enable(false);
                      			UIAPI:SetChecked(false,Button);
				else
					local Button = win:GetChild("FirstWin_Button" .. BtnID);
					Button:SetText(((SubID - 8)/100)..SM_313);
                                	Button:SetPosition(110,270)
					Button:Visible(true);
					Button:Enable(false);
                      			UIAPI:SetChecked(false,Button);
				end
			end;
		elseif Index == 3 then		-------消息提示文字----------
			if SubID == 1109 then
				FirstWin_Text1:SetText(NF_L0_H1109);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif math.mod(SubID ,10000) == 1 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(((SubID - 1)/10000)..SM_311);
                                Button:SetPosition(0,150)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
			elseif math.mod(SubID ,10000) == 2 then
				local Button = win:GetChild("FirstWin_Button" .. BtnID);
				Button:SetText(((SubID - 2)/10000)..SM_313);
                                Button:SetPosition(100,150)
				Button:Visible(true);
				Button:Enable(false);
                      		UIAPI:SetChecked(false,Button);
			elseif SubID == 1102 then
				FirstWin_Text1:SetText(NF_L0_H1102);
				FirstWin_Text1:Visible(true);
				FirstWin_Text1:SetPosition(40,150)
				NPCFUN:EndMessage(true);
			elseif SubID == 1112 then
				FirstWin_Text1:SetText(NF_L0_H1112);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1283 then
				FirstWin_Text1:SetText(NF_L0_H1107);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1110 then
				FirstWin_Text1:SetText(NF_L0_H1107);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1401 then
				FirstWin_Text1:SetText(NF_L0_H1401);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1402 then
				FirstWin_Text1:SetText(NF_L0_H1402);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1403 then
				FirstWin_Text1:SetText(NF_L0_H1403);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1404 then
				FirstWin_Text1:SetText(NF_L0_H1404);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1405 then
				FirstWin_Text1:SetText(NF_L0_H1405);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1406 then
				FirstWin_Text1:SetText(NF_L0_H1406);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1407 then
				FirstWin_Text1:SetText(NF_L0_H1407);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1408 then
				FirstWin_Text1:SetText(NF_L0_H1408);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1409 then
				FirstWin_Text1:SetText(NF_L0_H1409);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1410 then
				FirstWin_Text1:SetText(NF_L0_H1410);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1411 then
				FirstWin_Text1:SetText(NF_L0_H1411);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1412 then
				FirstWin_Text1:SetText(NF_L0_H1412);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1413 then
				FirstWin_Text1:SetText(NF_L0_H1413);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1414 then
				FirstWin_Text1:SetText(NF_L0_H1414);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1415 then
				FirstWin_Text1:SetText(NF_L0_H1415);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1416 then
				FirstWin_Text1:SetText(NF_L0_H1416);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1417 then
				FirstWin_Text1:SetText(NF_L0_H1417);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1280 then
				FirstWin_Text1:SetText(NF_L0_H1280);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1285 then
				FirstWin_Text1:SetText(NF_L0_H1284);
				FirstWin_Text1:Visible(true);
				FirstWin_Text1:SetPosition(40,80)
				NPCFUN:EndMessage(true);
			elseif SubID == 1600 then
				FirstWin_Text1:SetText(NF_L0_H1511);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1603 then
				FirstWin_Text1:SetText(NF_L0_H1107);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 2101 then
				FirstWin_Text1:SetText(NF_L0_H1511);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 3182 then
				FirstWin_Text1:SetText(NF_L0_H1112);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 3252 then
				FirstWin_Text1:SetText(NF_L0_H1312);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1172 then
				FirstWin_Text1:SetText(NF_L0_H1112);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
			elseif SubID == 1242 then
				FirstWin_Text1:SetText(NF_L0_H1312);
				FirstWin_Text1:Visible(true);
				NPCFUN:EndMessage(true);
		
			end;
		end;
end
